namespace CallistraAgent.Functions.Models;

/// <summary>
/// Represents a member's answer to a specific question during a call
/// </summary>
public class CallResponse
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Reference to call session
    /// </summary>
    public int CallSessionId { get; set; }

    /// <summary>
    /// Question sequence number (1, 2, 3)
    /// </summary>
    public int QuestionNumber { get; set; }

    /// <summary>
    /// Full question text as spoken to member
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// DTMF response (1 = yes, 2 = no)
    /// </summary>
    public int ResponseValue { get; set; }

    /// <summary>
    /// When response was captured
    /// </summary>
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to call session
    /// </summary>
    public CallSession CallSession { get; set; } = null!;

    /// <summary>
    /// Gets the response as text
    /// </summary>
    public string ResponseText => ResponseValue == 1 ? "Yes" : "No";
}
