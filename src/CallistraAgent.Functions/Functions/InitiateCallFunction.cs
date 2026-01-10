using CallistraAgent.Functions.Configuration;
using CallistraAgent.Functions.Models.DTOs;
using CallistraAgent.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace CallistraAgent.Functions.Functions;

/// <summary>
/// Azure Function for initiating outbound calls to members
/// </summary>
public class InitiateCallFunction
{
    private readonly ICallService _callService;
    private readonly AzureCommunicationServicesOptions _acsOptions;
    private readonly ILogger<InitiateCallFunction> _logger;

    public InitiateCallFunction(
        ICallService callService,
        IOptions<AzureCommunicationServicesOptions> acsOptions,
        ILogger<InitiateCallFunction> logger)
    {
        _callService = callService;
        _acsOptions = acsOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Initiates an outbound call to a member
    /// POST /api/calls/initiate/{memberId}
    /// </summary>
    [Function("InitiateCall")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "calls/initiate/{memberId}")] HttpRequestData req,
        int memberId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("InitiateCall function triggered for member {MemberId}", memberId);

        try
        {
            // Validate memberId
            if (memberId <= 0)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new
                {
                    type = "https://callistra.io/errors/invalid-member",
                    title = "Invalid Member ID",
                    status = 400,
                    detail = "Member ID must be a positive integer"
                }, cancellationToken);
                return badRequestResponse;
            }

            // Initiate the call
            var callSession = await _callService.InitiateCallAsync(memberId, cancellationToken);

            // Return 202 Accepted response
            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new InitiateCallResponse
            {
                CallSessionId = callSession.Id,
                MemberId = callSession.MemberId,
                Status = callSession.Status.ToString(),
                StartTime = callSession.StartTime,
                CallbackUrl = $"{_acsOptions.CallbackBaseUrl}/api/calls/events"
            }, cancellationToken);

            return response;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            // Member not found
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new
            {
                type = "https://callistra.io/errors/member-not-found",
                title = "Member Not Found",
                status = 404,
                detail = ex.Message
            }, cancellationToken);
            return notFoundResponse;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not eligible"))
        {
            // Member not eligible (inactive status)
            var unprocessableResponse = req.CreateResponse(HttpStatusCode.UnprocessableEntity);
            await unprocessableResponse.WriteAsJsonAsync(new
            {
                type = "https://callistra.io/errors/member-not-eligible",
                title = "Member Not Eligible",
                status = 422,
                detail = ex.Message
            }, cancellationToken);
            return unprocessableResponse;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already has an active call"))
        {
            // Conflict - member already has active call
            var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
            await conflictResponse.WriteAsJsonAsync(new
            {
                type = "https://callistra.io/errors/call-in-progress",
                title = "Call Already In Progress",
                status = 409,
                detail = ex.Message
            }, cancellationToken);
            return conflictResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating call for member {MemberId}", memberId);

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                type = "https://callistra.io/errors/internal-error",
                title = "Internal Server Error",
                status = 500,
                detail = "An error occurred while initiating the call"
            }, cancellationToken);
            return errorResponse;
        }
    }
}
