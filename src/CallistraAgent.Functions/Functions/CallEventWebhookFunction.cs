using Azure.Communication.CallAutomation;
using Azure.Messaging;
using CallistraAgent.Functions.Data.Repositories;
using CallistraAgent.Functions.Models;
using CallistraAgent.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace CallistraAgent.Functions.Functions;

/// <summary>
/// Azure Function for handling Azure Communication Services call event webhooks.
///
/// MVP SCOPE - Routes events for two core call scenarios:
/// 1. Human path: CallConnected → Person detection → DTMF responses → Questions → Completion
/// 2. Voicemail path: AMD detection → Beep wait → Leave message → Hangup
///
/// Timeout handling: Person detection timeout (10s) or question timeout (10s) → Disconnected status
/// </summary>
public class CallEventWebhookFunction
{
    private readonly ICallService _callService;
    private readonly ICallSessionRepository _callSessionRepository;
    private readonly CallSessionState _callSessionState;
    private readonly CallAutomationClient _callAutomationClient;
    private readonly ILogger<CallEventWebhookFunction> _logger;
    private readonly HashSet<string> _processedEventIds = new();

    public CallEventWebhookFunction(
        ICallService callService,
        ICallSessionRepository callSessionRepository,
        CallSessionState callSessionState,
        CallAutomationClient callAutomationClient,
        ILogger<CallEventWebhookFunction> logger)
    {
        _callService = callService;
        _callSessionRepository = callSessionRepository;
        _callSessionState = callSessionState;
        _callAutomationClient = callAutomationClient;
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

            case PlayCompleted playCompletedEvent:
                _logger.LogInformation("Routing PlayCompleted event for {CallConnectionId}", callConnectionId);
                await _callService.HandlePlayCompletedAsync(callConnectionId, cancellationToken);
                break;

            case RecognizeCompleted recognizeCompletedEvent:
                _logger.LogInformation("Routing RecognizeCompleted event for {CallConnectionId}", callConnectionId);
                await HandleRecognizeCompletedAsync(recognizeCompletedEvent, callConnectionId, cancellationToken);
                break;

            case RecognizeFailed recognizeFailedEvent:
                _logger.LogInformation("Routing RecognizeFailed event for {CallConnectionId}", callConnectionId);
                await HandleRecognizeFailedAsync(recognizeFailedEvent, callConnectionId, cancellationToken);
                break;

            default:
                _logger.LogInformation("Unhandled event type: {EventType}", callEvent.GetType().Name);
                break;
        }
    }

    private async Task HandleRecognizeCompletedAsync(RecognizeCompleted recognizeEvent, string callConnectionId, CancellationToken cancellationToken)
    {
        // Extract DTMF tones from recognize result
        if (recognizeEvent.RecognizeResult is DtmfResult dtmfResult)
        {
            // Convert DtmfTone enum values to their numeric string representations (e.g., DtmfTone.One -> "1")
            var tones = string.Join("", dtmfResult.Tones.Select(tone =>
            {
                if (tone.Equals(DtmfTone.Zero)) return "0";
                if (tone.Equals(DtmfTone.One)) return "1";
                if (tone.Equals(DtmfTone.Two)) return "2";
                if (tone.Equals(DtmfTone.Three)) return "3";
                if (tone.Equals(DtmfTone.Four)) return "4";
                if (tone.Equals(DtmfTone.Five)) return "5";
                if (tone.Equals(DtmfTone.Six)) return "6";
                if (tone.Equals(DtmfTone.Seven)) return "7";
                if (tone.Equals(DtmfTone.Eight)) return "8";
                if (tone.Equals(DtmfTone.Nine)) return "9";
                return tone.ToString();
            }));
            _logger.LogInformation("DTMF tones received: {Tones} for {CallConnectionId}", tones, callConnectionId);

            if (!string.IsNullOrEmpty(tones))
            {
                await _callService.HandleRecognizeCompletedAsync(callConnectionId, tones, cancellationToken);
            }
        }
        else
        {
            _logger.LogWarning("RecognizeCompleted event does not contain DTMF result for {CallConnectionId}", callConnectionId);
        }
    }

    private async Task HandleRecognizeFailedAsync(RecognizeFailed recognizeEvent, string callConnectionId, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Recognize failed for {CallConnectionId}, ResultInformation: {ResultInfo}",
            callConnectionId, recognizeEvent.ResultInformation?.Message);

        // Azure AMD Pattern: RecognizeFailed with timeout indicates voicemail (or unresponsive caller)
        if (recognizeEvent.ResultInformation?.SubCode == 8510) // InitialSilenceTimeout
        {
            _logger.LogInformation("DTMF timeout detected for {CallConnectionId}", callConnectionId);

            var callSession = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);
            if (callSession != null)
            {
                var state = _callSessionState.GetCallState(callConnectionId);
                if (state?.CurrentQuestionNumber == 0)
                {
                    // Person detection timeout - hang up
                    _logger.LogInformation("Person detection timeout for {CallConnectionId}, hanging up", callConnectionId);
                    await _callService.HandlePersonDetectionTimeoutAsync(callConnectionId, cancellationToken);
                }
                else
                {
                    // Healthcare question timeout - mark as Disconnected and hang up
                    _logger.LogInformation("Healthcare question timeout for {CallConnectionId}, marking Disconnected and hanging up", callConnectionId);

                    callSession.Status = CallStatus.Disconnected;
                    callSession.EndTime = DateTime.UtcNow;
                    await _callSessionRepository.UpdateAsync(callSession, cancellationToken);

                    _callSessionState.RemoveCallState(callConnectionId);

                    var callConnection = _callAutomationClient.GetCallConnection(callConnectionId);
                    await callConnection.HangUpAsync(forEveryone: true, cancellationToken);
                }
            }
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
            PlayCompleted playCompleted => playCompleted.CallConnectionId,
            RecognizeCompleted recognizeCompleted => recognizeCompleted.CallConnectionId,
            RecognizeFailed recognizeFailed => recognizeFailed.CallConnectionId,
            _ => null
        };
    }
}
