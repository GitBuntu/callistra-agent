using CallistraAgent.Functions.Models;

namespace CallistraAgent.Functions.Data.Repositories;

/// <summary>
/// Repository interface for CallResponse entity operations
/// </summary>
public interface ICallResponseRepository
{
    /// <summary>
    /// Gets all responses for a call session
    /// </summary>
    Task<List<CallResponse>> GetByCallSessionIdAsync(int callSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new call response
    /// </summary>
    Task<CallResponse> CreateAsync(CallResponse callResponse, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific response by call session and question number
    /// </summary>
    Task<CallResponse?> GetByQuestionAsync(int callSessionId, int questionNumber, CancellationToken cancellationToken = default);
}
