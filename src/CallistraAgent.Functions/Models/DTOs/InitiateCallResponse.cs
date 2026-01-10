namespace CallistraAgent.Functions.Models.DTOs;

/// <summary>
/// Response DTO for call initiation request
/// </summary>
public class InitiateCallResponse
{
    /// <summary>
    /// Unique identifier for the call session
    /// </summary>
    public int CallSessionId { get; set; }

    /// <summary>
    /// Member being called
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// Initial call status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When call initiation was requested (UTC)
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Webhook URL for call events
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;
}
