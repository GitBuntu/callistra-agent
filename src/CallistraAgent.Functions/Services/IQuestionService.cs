using Azure.Communication.CallAutomation;

namespace CallistraAgent.Functions.Services;

/// <summary>
/// Service for managing healthcare questions and DTMF interactions
/// </summary>
public interface IQuestionService
{
    /// <summary>
    /// Plays the person detection prompt with DTMF recognition to detect voicemail
    /// </summary>
    /// <param name="callConnection">The call connection to play the prompt on</param>
    /// <param name="targetPhoneNumber">The target participant's phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PlayPersonDetectionPromptAsync(CallConnection callConnection, string targetPhoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays a healthcare question with DTMF recognition options (1=Yes, 2=No)
    /// </summary>
    /// <param name="callConnection">The call connection to play the question on</param>
    /// <param name="questionNumber">The question number (1-3)</param>
    /// <param name="targetPhoneNumber">The target participant's phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PlayHealthcareQuestionAsync(CallConnection callConnection, int questionNumber, string targetPhoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles invalid DTMF input with retry logic (max 2 retries)
    /// </summary>
    /// <param name="callConnection">The call connection</param>
    /// <param name="questionNumber">The question number being retried</param>
    /// <param name="retryCount">Current retry count</param>
    /// <param name="targetPhoneNumber">The target participant's phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if retry should occur, false if max retries exceeded</returns>
    Task<bool> HandleInvalidDtmfAsync(CallConnection callConnection, int questionNumber, int retryCount, string targetPhoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles DTMF timeout (no response within timeout period)
    /// </summary>
    /// <param name="callConnection">The call connection</param>
    /// <param name="questionNumber">The question number that timed out</param>
    /// <param name="hasRetriedOnce">Whether this question has already been retried once</param>
    /// <param name="targetPhoneNumber">The target participant's phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if retry should occur, false if should skip question</returns>
    Task<bool> HandleTimeoutAsync(CallConnection callConnection, int questionNumber, bool hasRetriedOnce, string targetPhoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays the completion message after all questions have been answered
    /// </summary>
    /// <param name="callConnection">The call connection to play the message on</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PlayCompletionMessageAsync(CallConnection callConnection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays the voicemail callback message (no PHI)
    /// </summary>
    /// <param name="callConnection">The call connection to play the message on</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PlayVoicemailCallbackMessageAsync(CallConnection callConnection, CancellationToken cancellationToken = default);
}
