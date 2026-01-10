using CallistraAgent.Functions.Data.Repositories;
using CallistraAgent.Functions.Models.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CallistraAgent.Functions.Functions;

/// <summary>
/// Azure Function for querying call session status
/// </summary>
public class CallStatusFunction
{
    private readonly ICallSessionRepository _callSessionRepository;
    private readonly ILogger<CallStatusFunction> _logger;

    public CallStatusFunction(
        ICallSessionRepository callSessionRepository,
        ILogger<CallStatusFunction> logger)
    {
        _callSessionRepository = callSessionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets call status by connection ID
    /// GET /api/calls/status/{callConnectionId}
    /// </summary>
    [Function("GetCallStatus")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "calls/status/{callConnectionId}")] HttpRequestData req,
        string callConnectionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetCallStatus function triggered for {CallConnectionId}", callConnectionId);

        try
        {
            // Validate callConnectionId
            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new
                {
                    type = "https://callistra.io/errors/invalid-parameter",
                    title = "Invalid Parameter",
                    status = 400,
                    detail = "CallConnectionId cannot be empty"
                }, cancellationToken);
                return badRequestResponse;
            }

            // Retrieve call session
            var callSession = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);

            if (callSession == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new
                {
                    type = "https://callistra.io/errors/call-not-found",
                    title = "Call Not Found",
                    status = 404,
                    detail = $"No call session found with connection ID: {callConnectionId}"
                }, cancellationToken);
                return notFoundResponse;
            }

            // Build response DTO
            var statusResponse = new CallStatusResponse
            {
                CallSessionId = callSession.Id,
                MemberId = callSession.MemberId,
                MemberName = callSession.Member?.FullName ?? string.Empty,
                CallConnectionId = callSession.CallConnectionId,
                Status = callSession.Status.ToString(),
                StartTime = callSession.StartTime,
                EndTime = callSession.EndTime,
                DurationSeconds = callSession.DurationSeconds,
                Responses = callSession.Responses.Select(r => new CallResponseDto
                {
                    QuestionNumber = r.QuestionNumber,
                    QuestionText = r.QuestionText,
                    Response = r.ResponseText,
                    RespondedAt = r.RespondedAt
                }).ToList()
            };

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(statusResponse, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving call status for {CallConnectionId}", callConnectionId);

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                type = "https://callistra.io/errors/internal-error",
                title = "Internal Server Error",
                status = 500,
                detail = "An error occurred while retrieving the call status"
            }, cancellationToken);
            return errorResponse;
        }
    }
}
