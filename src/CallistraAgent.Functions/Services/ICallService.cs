using CallistraAgent.Functions.Models;

namespace CallistraAgent.Functions.Services;

/// <summary>
/// Service interface for call management operations
/// </summary>
public interface ICallService
{
    /// <summary>
    /// Initiates an outbound call to a member
    /// </summary>
    Task<CallSession> InitiateCallAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles CallConnected event from Azure Communication Services
    /// </summary>
    Task HandleCallConnectedAsync(string callConnectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles CallDisconnected event from Azure Communication Services
    /// </summary>
    Task HandleCallDisconnectedAsync(string callConnectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles call failure scenarios
    /// </summary>
    Task HandleCallFailedAsync(string callConnectionId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles NoAnswer timeout
    /// </summary>
    Task HandleNoAnswerAsync(string callConnectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes call state and plays person detection prompt when call connects
    /// </summary>
    Task InitializeCallFlowAsync(string callConnectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles person detection timeout per Azure AMD pattern.
    /// Timeout = voicemail detected → Leaves callback message.
    /// </summary>
    Task HandlePersonDetectionTimeoutAsync(string callConnectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles successful DTMF response and progresses to next question
    /// </summary>
    Task HandleDtmfResponseAsync(string callConnectionId, string dtmfTones, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles PlayCompleted event to trigger next action in call flow
    /// </summary>
    Task HandlePlayCompletedAsync(string callConnectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles RecognizeCompleted event with DTMF tones and saves response
    /// </summary>
    Task HandleRecognizeCompletedAsync(string callConnectionId, string dtmfTones, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a call response to the database
    /// </summary>
    Task SaveCallResponseAsync(int callSessionId, int questionNumber, string dtmfResponse, CancellationToken cancellationToken = default);
}
