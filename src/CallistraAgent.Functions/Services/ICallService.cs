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
}
