namespace CallistraAgent.Functions.Models;

/// <summary>
/// Represents a single outbound call attempt to a member
/// </summary>
public class CallSession
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Reference to member being called
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// Azure Communication Services call connection identifier
    /// </summary>
    public string? CallConnectionId { get; set; }

    /// <summary>
    /// Current call status
    /// </summary>
    public CallStatus Status { get; set; } = CallStatus.Initiated;

    /// <summary>
    /// When call initiation was requested
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When call ended (null if still in progress)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last status update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to member
    /// </summary>
    public Member Member { get; set; } = null!;

    /// <summary>
    /// Navigation property to responses
    /// </summary>
    public ICollection<CallResponse> Responses { get; set; } = new List<CallResponse>();

    /// <summary>
    /// Gets the call duration in seconds (null if still in progress)
    /// </summary>
    public int? DurationSeconds => EndTime.HasValue
        ? (int)(EndTime.Value - StartTime).TotalSeconds
        : null;
}
