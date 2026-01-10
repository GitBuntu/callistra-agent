using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallistraAgent.Functions.Constants;
using Microsoft.Extensions.Logging;

namespace CallistraAgent.Functions.Services;

/// <summary>
/// Service for managing healthcare questions and DTMF interactions
/// </summary>
public class QuestionService : IQuestionService
{
    private readonly ILogger<QuestionService> _logger;

    public QuestionService(ILogger<QuestionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task PlayPersonDetectionPromptAsync(CallConnection callConnection, CancellationToken cancellationToken = default)
    {
        if (callConnection == null)
            throw new ArgumentNullException(nameof(callConnection));

        _logger.LogInformation("Playing person detection prompt on call {CallConnectionId}",
            callConnection.CallConnectionId);

        var playSource = new TextSource(VoicemailMessages.PersonDetectionPrompt)
        {
            VoiceName = "en-US-JennyNeural" // Azure Cognitive Services Neural TTS voice
        };

        var recognizeOptions = new CallMediaRecognizeDtmfOptions(
            targetParticipant: new PhoneNumberIdentifier(""),
            maxTonesToCollect: 1)
        {
            InterruptPrompt = true,
            InitialSilenceTimeout = TimeSpan.FromSeconds(VoicemailMessages.PersonDetectionTimeoutSeconds),
            Prompt = playSource,
            InterToneTimeout = TimeSpan.FromSeconds(2)
        };

        try
        {
            await callConnection.GetCallMedia().StartRecognizingAsync(recognizeOptions, cancellationToken);
            _logger.LogInformation("Person detection prompt started successfully on call {CallConnectionId}",
                callConnection.CallConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play person detection prompt on call {CallConnectionId}",
                callConnection.CallConnectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task PlayHealthcareQuestionAsync(CallConnection callConnection, int questionNumber, CancellationToken cancellationToken = default)
    {
        if (callConnection == null)
            throw new ArgumentNullException(nameof(callConnection));

        if (questionNumber < 1 || questionNumber > HealthcareQuestions.TotalQuestions)
            throw new ArgumentOutOfRangeException(nameof(questionNumber),
                $"Question number must be between 1 and {HealthcareQuestions.TotalQuestions}");

        var questionText = HealthcareQuestions.GetQuestion(questionNumber);

        _logger.LogInformation("Playing question {QuestionNumber} on call {CallConnectionId}",
            questionNumber, callConnection.CallConnectionId);

        var playSource = new TextSource(questionText)
        {
            VoiceName = "en-US-JennyNeural"
        };

        var recognizeOptions = new CallMediaRecognizeDtmfOptions(
            targetParticipant: new PhoneNumberIdentifier(""),
            maxTonesToCollect: 1)
        {
            InterruptPrompt = true,
            InitialSilenceTimeout = TimeSpan.FromSeconds(VoicemailMessages.QuestionTimeoutSeconds),
            Prompt = playSource,
            InterToneTimeout = TimeSpan.FromSeconds(2)
        };

        try
        {
            await callConnection.GetCallMedia().StartRecognizingAsync(recognizeOptions, cancellationToken);
            _logger.LogInformation("Question {QuestionNumber} started successfully on call {CallConnectionId}",
                questionNumber, callConnection.CallConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play question {QuestionNumber} on call {CallConnectionId}",
                questionNumber, callConnection.CallConnectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HandleInvalidDtmfAsync(CallConnection callConnection, int questionNumber, int retryCount, CancellationToken cancellationToken = default)
    {
        if (callConnection == null)
            throw new ArgumentNullException(nameof(callConnection));

        _logger.LogWarning("Invalid DTMF input received for question {QuestionNumber} on call {CallConnectionId}, retry count: {RetryCount}",
            questionNumber, callConnection.CallConnectionId, retryCount);

        if (retryCount >= VoicemailMessages.MaxRetries)
        {
            _logger.LogWarning("Max retries ({MaxRetries}) reached for question {QuestionNumber} on call {CallConnectionId}, skipping question",
                VoicemailMessages.MaxRetries, questionNumber, callConnection.CallConnectionId);
            return false;
        }

        // Play error message and re-prompt
        var errorMessage = "Invalid input. Please press 1 for yes or 2 for no.";
        var playSource = new TextSource(errorMessage)
        {
            VoiceName = "en-US-JennyNeural"
        };

        try
        {
            var playOptions = new PlayToAllOptions(playSource);
            await callConnection.GetCallMedia().PlayToAllAsync(playOptions, cancellationToken);

            // Re-play the question after error message
            await PlayHealthcareQuestionAsync(callConnection, questionNumber, cancellationToken);

            return true; // Continue with retry
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle invalid DTMF for question {QuestionNumber} on call {CallConnectionId}",
                questionNumber, callConnection.CallConnectionId);
            return false; // Skip question on error
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HandleTimeoutAsync(CallConnection callConnection, int questionNumber, bool hasRetriedOnce, CancellationToken cancellationToken = default)
    {
        if (callConnection == null)
            throw new ArgumentNullException(nameof(callConnection));

        _logger.LogWarning("Timeout occurred for question {QuestionNumber} on call {CallConnectionId}, hasRetriedOnce: {HasRetriedOnce}",
            questionNumber, callConnection.CallConnectionId, hasRetriedOnce);

        if (hasRetriedOnce)
        {
            _logger.LogWarning("Question {QuestionNumber} already retried once on call {CallConnectionId}, skipping question",
                questionNumber, callConnection.CallConnectionId);
            return false; // Skip after one retry
        }

        // Play timeout message and re-prompt once
        var timeoutMessage = "We didn't receive your response. Let me repeat the question.";
        var playSource = new TextSource(timeoutMessage)
        {
            VoiceName = "en-US-JennyNeural"
        };

        try
        {
            var playOptions = new PlayToAllOptions(playSource);
            await callConnection.GetCallMedia().PlayToAllAsync(playOptions, cancellationToken);

            // Re-play the question after timeout message
            await PlayHealthcareQuestionAsync(callConnection, questionNumber, cancellationToken);

            return true; // Retry once
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle timeout for question {QuestionNumber} on call {CallConnectionId}",
                questionNumber, callConnection.CallConnectionId);
            return false; // Skip question on error
        }
    }

    /// <inheritdoc/>
    public async Task PlayVoicemailCallbackMessageAsync(CallConnection callConnection, CancellationToken cancellationToken = default)
    {
        if (callConnection == null)
            throw new ArgumentNullException(nameof(callConnection));

        _logger.LogInformation("Playing voicemail callback message on call {CallConnectionId}",
            callConnection.CallConnectionId);

        var playSource = new TextSource(VoicemailMessages.CallbackMessage)
        {
            VoiceName = "en-US-JennyNeural"
        };

        try
        {
            var playOptions = new PlayToAllOptions(playSource);
            await callConnection.GetCallMedia().PlayToAllAsync(playOptions, cancellationToken);
            _logger.LogInformation("Voicemail callback message played successfully on call {CallConnectionId}",
                callConnection.CallConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play voicemail callback message on call {CallConnectionId}",
                callConnection.CallConnectionId);
            throw;
        }
    }
}
