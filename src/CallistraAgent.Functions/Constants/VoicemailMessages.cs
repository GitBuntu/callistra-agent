namespace CallistraAgent.Functions.Constants;

/// <summary>
/// Voicemail messages and constants for person detection and callback messaging
/// </summary>
public static class VoicemailMessages
{
    /// <summary>
    /// Person detection prompt played when call connects.
    /// Asks for DTMF press to confirm human presence.
    /// If no response within PersonDetectionTimeoutSeconds, system hangs up.
    /// </summary>
    public const string PersonDetectionPrompt = "Hello, this is an automated call from your healthcare program. If you are a person and can hear this message, please press 1 now.";

    /// <summary>
    /// Voicemail callback message (no PHI).
    /// Reserved for future use - currently not used as system hangs up on person detection timeout.
    /// </summary>
    public const string CallbackMessage = "We were unable to reach you. Please call us back at your convenience to discuss your healthcare enrollment. Thank you.";

    /// <summary>
    /// Timeout for person detection response (seconds)
    /// </summary>
    public const int PersonDetectionTimeoutSeconds = 10;

    /// <summary>
    /// Timeout for question DTMF response (seconds)
    /// </summary>
    public const int QuestionTimeoutSeconds = 10;

    /// <summary>
    /// Maximum retries for invalid DTMF input
    /// </summary>
    public const int MaxRetries = 2;
}
