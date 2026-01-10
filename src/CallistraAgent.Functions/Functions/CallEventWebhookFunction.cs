using Azure.Communication.CallAutomation;
using Azure.Messaging;
using CallistraAgent.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace CallistraAgent.Functions.Functions;

/// <summary>
/// Azure Function for handling Azure Communication Services call event webhooks
/// </summary>
public class CallEventWebhookFunction
{
    private readonly ICallService _callService;
    private readonly ILogger<CallEventWebhookFunction> _logger;
    private readonly HashSet<string> _processedEventIds = new();

    public CallEventWebhookFunction(
        ICallService callService,
        ILogger<CallEventWebhookFunction> logger)
    {
        _callService = callService;
        _logger = logger;
    }

    /// <summary>
    /// Handles Azure Communication Services call events
    /// POST /api/calls/events
    /// </summary>
    [Function("CallEventWebhook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "calls/events")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("CallEventWebhook function triggered");

        try
        {
            // Read the CloudEvent from request body
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new
                {
                    type = "https://callistra.io/errors/invalid-request",
                    title = "Invalid Request",
                    status = 400,
                    detail = "Request body is empty"
                }, cancellationToken);
                return badRequestResponse;
            }

            // Parse CloudEvent
            var cloudEvent = CloudEvent.Parse(BinaryData.FromString(requestBody));

            _logger.LogInformation("Received CloudEvent: Type={EventType}, Id={EventId}", cloudEvent.Type, cloudEvent.Id);

            // Check for duplicate events (idempotency)
            if (_processedEventIds.Contains(cloudEvent.Id))
            {
                _logger.LogInformation("Event {EventId} already processed, skipping", cloudEvent.Id);
                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                await okResponse.WriteAsJsonAsync(new { eventId = cloudEvent.Id, processed = false, reason = "duplicate" }, cancellationToken);
                return okResponse;
            }

            // Parse the call automation event
            var callEvent = CallAutomationEventParser.Parse(cloudEvent);

            // Extract call connection ID from the event
            string? callConnectionId = GetCallConnectionId(callEvent);

            if (string.IsNullOrEmpty(callConnectionId))
            {
                _logger.LogWarning("CallConnectionId not found in event {EventId}", cloudEvent.Id);
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new
                {
                    type = "https://callistra.io/errors/invalid-event",
                    title = "Invalid Event",
                    status = 400,
                    detail = "CallConnectionId not found in event"
                }, cancellationToken);
                return badRequestResponse;
            }

            // Route event to appropriate handler
            await RouteEventAsync(callEvent, callConnectionId, cancellationToken);

            // Mark event as processed
            _processedEventIds.Add(cloudEvent.Id);

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                eventId = cloudEvent.Id,
                processed = true
            }, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing call event");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                type = "https://callistra.io/errors/internal-error",
                title = "Internal Server Error",
                status = 500,
                detail = "An error occurred while processing the call event"
            }, cancellationToken);
            return errorResponse;
        }
    }

    private async Task RouteEventAsync(CallAutomationEventBase callEvent, string callConnectionId, CancellationToken cancellationToken)
    {
        switch (callEvent)
        {
            case CallConnected:
                _logger.LogInformation("Routing CallConnected event for {CallConnectionId}", callConnectionId);
                await _callService.HandleCallConnectedAsync(callConnectionId, cancellationToken);
                break;

            case CallDisconnected disconnectedEvent:
                _logger.LogInformation("Routing CallDisconnected event for {CallConnectionId}", callConnectionId);
                await _callService.HandleCallDisconnectedAsync(callConnectionId, cancellationToken);
                break;

            default:
                _logger.LogInformation("Unhandled event type: {EventType}", callEvent.GetType().Name);
                break;
        }
    }

    private string? GetCallConnectionId(CallAutomationEventBase callEvent)
    {
        // Extract CallConnectionId from the event
        // Different event types may have CallConnectionId in different properties
        return callEvent switch
        {
            CallConnected connected => connected.CallConnectionId,
            CallDisconnected disconnected => disconnected.CallConnectionId,
            _ => null
        };
    }
}
