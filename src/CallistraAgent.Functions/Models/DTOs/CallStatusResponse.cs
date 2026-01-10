namespace CallistraAgent.Functions.Models.DTOs;

/// <summary>
/// Response DTO for call status query
/// </summary>
public class CallStatusResponse
{
    /// <summary>
    /// Call session database ID
    /// </summary>
    public int CallSessionId { get; set; }

    /// <summary>
    /// Member being called
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// Member's full name
    /// </summary>
    public string MemberName { get; set; } = string.Empty;

    /// <summary>
    /// ACS call connection identifier
    /// </summary>
    public string? CallConnectionId { get; set; }

    /// <summary>
    /// Current call status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When call was initiated (UTC)
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When call ended (UTC, null if still in progress)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Call duration in seconds (null if still in progress)
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Responses captured during the call
    /// </summary>
    public List<CallResponseDto> Responses { get; set; } = new();
}

/// <summary>
/// DTO for a single call response
/// </summary>
public class CallResponseDto
{
    /// <summary>
    /// Question sequence number
    /// </summary>
    public int QuestionNumber { get; set; }

    /// <summary>
    /// Question text
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Response as text (Yes/No)
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// When response was captured
    /// </summary>
    public DateTime RespondedAt { get; set; }
}
