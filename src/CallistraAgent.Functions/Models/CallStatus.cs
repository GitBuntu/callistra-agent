namespace CallistraAgent.Functions.Models;

/// <summary>
/// Status of a call session
/// </summary>
public enum CallStatus
{
    /// <summary>
    /// Call request sent to Azure Communication Services
    /// </summary>
    Initiated,

    /// <summary>
    /// Phone is ringing (CallConnecting event received)
    /// </summary>
    Ringing,

    /// <summary>
    /// Member answered (CallConnected event received)
    /// </summary>
    Connected,

    /// <summary>
    /// All questions answered successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Member hung up before completing all questions
    /// </summary>
    Disconnected,

    /// <summary>
    /// System error or call setup failure
    /// </summary>
    Failed,

    /// <summary>
    /// 30-second timeout expired without answer
    /// </summary>
    NoAnswer,

    /// <summary>
    /// Voicemail detected, generic callback message played
    /// </summary>
    VoicemailMessage
}
