using CallistraAgent.Functions.Models;

namespace CallistraAgent.Functions.Data.Repositories;

/// <summary>
/// Repository interface for CallSession entity operations
/// </summary>
public interface ICallSessionRepository
{
    /// <summary>
    /// Gets a call session by ID
    /// </summary>
    Task<CallSession?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a call session by call connection ID
    /// </summary>
    Task<CallSession?> GetByCallConnectionIdAsync(string callConnectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all call sessions for a member
    /// </summary>
    Task<List<CallSession>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active call session for a member (if any)
    /// </summary>
    Task<CallSession?> GetActiveCallForMemberAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new call session
    /// </summary>
    Task<CallSession> CreateAsync(CallSession callSession, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing call session
    /// </summary>
    Task UpdateAsync(CallSession callSession, CancellationToken cancellationToken = default);
}
