namespace CallistraAgent.Functions.Models;

/// <summary>
/// Represents a healthcare program enrollee who can receive outreach calls
/// </summary>
public class Member
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Member's first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Member's last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// E.164 format phone number (e.g., +18005551234)
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Healthcare program name (e.g., "Diabetes Care", "Wellness")
    /// </summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>
    /// Enrollment status (Active, Pending, Inactive)
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to call sessions
    /// </summary>
    public ICollection<CallSession> CallSessions { get; set; } = new List<CallSession>();

    /// <summary>
    /// Gets the member's full name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";
}
