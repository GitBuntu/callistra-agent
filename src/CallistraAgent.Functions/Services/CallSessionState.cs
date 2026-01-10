using System.Collections.Concurrent;

namespace CallistraAgent.Functions.Services;

/// <summary>
/// In-memory cache for tracking call session state during active calls
/// </summary>
public class CallSessionState
{
    private readonly ConcurrentDictionary<string, CallStateData> _callStates = new();

    /// <summary>
    /// Initializes call state for a new call (starts at question 0 = person detection)
    /// </summary>
    /// <param name="callConnectionId">The call connection ID</param>
    /// <param name="callSessionId">The database CallSession ID</param>
    public void InitializeCallState(string callConnectionId, int callSessionId)
    {
        if (string.IsNullOrWhiteSpace(callConnectionId))
            throw new ArgumentException("Call connection ID cannot be null or empty", nameof(callConnectionId));

        var state = new CallStateData
        {
            CallSessionId = callSessionId,
            CurrentQuestionNumber = 0, // 0 = person detection, 1-3 = healthcare questions
            RetryCount = 0,
            HasRetriedTimeout = false,
            CreatedAt = DateTime.UtcNow
        };

        _callStates.AddOrUpdate(callConnectionId, state, (key, existing) => state);
    }

    /// <summary>
    /// Gets the current state for a call
    /// </summary>
    /// <param name="callConnectionId">The call connection ID</param>
    /// <returns>Call state data, or null if not found</returns>
    public CallStateData? GetCallState(string callConnectionId)
    {
        if (string.IsNullOrWhiteSpace(callConnectionId))
            return null;

        return _callStates.TryGetValue(callConnectionId, out var state) ? state : null;
    }

    /// <summary>
    /// Progresses to the next question (1 → 2 → 3)
    /// </summary>
    /// <param name="callConnectionId">The call connection ID</param>
    /// <returns>The new current question number, or -1 if call state not found</returns>
    public int ProgressToNextQuestion(string callConnectionId)
    {
        if (string.IsNullOrWhiteSpace(callConnectionId))
            return -1;

        if (_callStates.TryGetValue(callConnectionId, out var state))
        {
            state.CurrentQuestionNumber++;
            state.RetryCount = 0; // Reset retry count for new question
            state.HasRetriedTimeout = false; // Reset timeout retry flag
            state.UpdatedAt = DateTime.UtcNow;
            return state.CurrentQuestionNumber;
        }

        return -1;
    }

    /// <summary>
    /// Increments the retry count for the current question
    /// </summary>
    /// <param name="callConnectionId">The call connection ID</param>
    /// <returns>The new retry count, or -1 if call state not found</returns>
    public int IncrementRetryCount(string callConnectionId)
    {
        if (string.IsNullOrWhiteSpace(callConnectionId))
            return -1;

        if (_callStates.TryGetValue(callConnectionId, out var state))
        {
            state.RetryCount++;
            state.UpdatedAt = DateTime.UtcNow;
            return state.RetryCount;
        }

        return -1;
    }

    /// <summary>
    /// Marks that a timeout retry has occurred for the current question
    /// </summary>
    /// <param name="callConnectionId">The call connection ID</param>
    public void MarkTimeoutRetried(string callConnectionId)
    {
        if (string.IsNullOrWhiteSpace(callConnectionId))
            return;

        if (_callStates.TryGetValue(callConnectionId, out var state))
        {
            state.HasRetriedTimeout = true;
            state.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Removes call state (typically on call completion or disconnect)
    /// </summary>
    /// <param name="callConnectionId">The call connection ID</param>
    /// <returns>True if removed, false if not found</returns>
    public bool RemoveCallState(string callConnectionId)
    {
        if (string.IsNullOrWhiteSpace(callConnectionId))
            return false;

        return _callStates.TryRemove(callConnectionId, out _);
    }

    /// <summary>
    /// Cleans up stale call states older than the specified age
    /// </summary>
    /// <param name="maxAge">Maximum age for call states (default: 1 hour)</param>
    /// <returns>Number of states removed</returns>
    public int CleanupStaleStates(TimeSpan? maxAge = null)
    {
        var cutoff = DateTime.UtcNow - (maxAge ?? TimeSpan.FromHours(1));
        var staleKeys = _callStates
            .Where(kvp => kvp.Value.CreatedAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        var removedCount = 0;
        foreach (var key in staleKeys)
        {
            if (_callStates.TryRemove(key, out _))
                removedCount++;
        }

        return removedCount;
    }
}

/// <summary>
/// Data structure for tracking call state
/// </summary>
public class CallStateData
{
    /// <summary>
    /// Database CallSession ID
    /// </summary>
    public int CallSessionId { get; set; }

    /// <summary>
    /// Current question number (0 = person detection, 1-3 = healthcare questions)
    /// </summary>
    public int CurrentQuestionNumber { get; set; }

    /// <summary>
    /// Retry count for current question (invalid DTMF)
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Whether timeout retry has occurred for current question
    /// </summary>
    public bool HasRetriedTimeout { get; set; }

    /// <summary>
    /// When the state was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the state was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
