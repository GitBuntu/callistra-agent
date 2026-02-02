using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallistraAgent.Functions.Configuration;
using CallistraAgent.Functions.Data.Repositories;
using CallistraAgent.Functions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CallistraAgent.Functions.Services;

/// <summary>
/// Service implementation for call management operations.
///
/// MVP SCOPE - This implementation supports TWO core scenarios only:
/// 1. Human Answers: CallConnected → Person detection prompt → Press 1 (RecognizeCompleted) → 3 questions → Completed or Disconnected
/// 2. Voicemail Answers: CallConnected → Person detection prompt → Timeout (RecognizeFailed) → Leave message → VoicemailMessage
///
/// Implements Azure's documented AMD pattern: Use Recognize API after CallConnected to detect human vs voicemail.
/// Out of scope: Retry logic, partial response recovery, call transfers, inbound calls, dynamic questions
/// </summary>
public class CallService : ICallService
{
    private readonly ICallSessionRepository _callSessionRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ICallResponseRepository _callResponseRepository;
    private readonly CallAutomationClient _callAutomationClient;
    private readonly AzureCommunicationServicesOptions _acsOptions;
    private readonly IQuestionService _questionService;
    private readonly CallSessionState _callSessionState;
    private readonly ILogger<CallService> _logger;

    public CallService(
        ICallSessionRepository callSessionRepository,
        IMemberRepository memberRepository,
        ICallResponseRepository callResponseRepository,
        CallAutomationClient callAutomationClient,
        IOptions<AzureCommunicationServicesOptions> acsOptions,
        IQuestionService questionService,
        CallSessionState callSessionState,
        ILogger<CallService> logger)
    {
        _callSessionRepository = callSessionRepository;
        _memberRepository = memberRepository;
        _callResponseRepository = callResponseRepository;
        _callAutomationClient = callAutomationClient;
        _acsOptions = acsOptions.Value;
        _questionService = questionService;
        _callSessionState = callSessionState;
        _logger = logger;
    }

    public async Task<CallSession> InitiateCallAsync(int memberId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating call for member {MemberId}", memberId);

        // Validate member exists
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken);
        if (member == null)
        {
            _logger.LogWarning("Member {MemberId} not found", memberId);
            throw new InvalidOperationException($"Member with ID {memberId} not found");
        }

        // Check if member is active
        if (member.Status != "Active")
        {
            _logger.LogWarning("Member {MemberId} is not active (Status: {Status})", memberId, member.Status);
            throw new InvalidOperationException($"Member {memberId} is not eligible for calls (Status: {member.Status})");
        }

        // Check for existing active call
        var activeCall = await _callSessionRepository.GetActiveCallForMemberAsync(memberId, cancellationToken);
        if (activeCall != null)
        {
            _logger.LogWarning("Member {MemberId} already has an active call (Session ID: {CallSessionId})", memberId, activeCall.Id);
            throw new InvalidOperationException($"Member {memberId} already has an active call session (ID: {activeCall.Id})");
        }

        // Create call session record
        var callSession = new CallSession
        {
            MemberId = memberId,
            Status = CallStatus.Initiated,
            StartTime = DateTime.UtcNow
        };

        callSession = await _callSessionRepository.CreateAsync(callSession, cancellationToken);
        _logger.LogInformation("Created call session {CallSessionId} for member {MemberId}", callSession.Id, memberId);

        // DEBUG: Log configuration values
        _logger.LogInformation("DEBUG: ACS Config - PhoneNumber: {Phone}, CallbackUrl: {Callback}, CognitiveEndpoint: '{Endpoint}'",
            _acsOptions.PhoneNumber, _acsOptions.CallbackBaseUrl, _acsOptions.CognitiveServicesEndpoint ?? "NULL");

        try
        {
            // Initiate call via Azure Communication Services
            // AMD is implemented via person detection prompt AFTER CallConnected (per Azure docs)
            var callbackUri = new Uri($"{_acsOptions.CallbackBaseUrl}/api/calls/events");
            var target = new PhoneNumberIdentifier(member.PhoneNumber);
            var caller = new PhoneNumberIdentifier(_acsOptions.PhoneNumber);

            var createCallOptions = new CreateCallOptions(
                new CallInvite(target, caller),
                callbackUri
            );

            // Enable Azure Cognitive Services for TTS/STT if configured
            if (!string.IsNullOrEmpty(_acsOptions.CognitiveServicesEndpoint))
            {
                createCallOptions.CallIntelligenceOptions = new CallIntelligenceOptions()
                {
                    CognitiveServicesEndpoint = new Uri(_acsOptions.CognitiveServicesEndpoint)
                };
                _logger.LogInformation("Cognitive Services enabled for TTS: {Endpoint}", _acsOptions.CognitiveServicesEndpoint);
            }
            else
            {
                _logger.LogWarning("Cognitive Services endpoint not configured. Text-to-speech will not work. Add 'AzureCommunicationServices__CognitiveServicesEndpoint' to configuration.");
            }

            var createCallResult = await _callAutomationClient.CreateCallAsync(createCallOptions, cancellationToken);

            // Update call session with connection ID
            callSession.CallConnectionId = createCallResult.Value.CallConnectionProperties.CallConnectionId;
            await _callSessionRepository.UpdateAsync(callSession, cancellationToken);

            _logger.LogInformation("Call initiated successfully. CallConnectionId: {CallConnectionId}", callSession.CallConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate call for member {MemberId}", memberId);

            // Update call session to Failed status
            callSession.Status = CallStatus.Failed;
            callSession.EndTime = DateTime.UtcNow;
            await _callSessionRepository.UpdateAsync(callSession, cancellationToken);

            throw new InvalidOperationException($"Failed to initiate call: {ex.Message}", ex);
        }

        return callSession;
    }

    public async Task HandleCallConnectedAsync(string callConnectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling CallConnected event for {CallConnectionId}", callConnectionId);

        var callSession = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);
        if (callSession == null)
        {
            _logger.LogWarning("Call session not found for CallConnectionId: {CallConnectionId}", callConnectionId);
            return;
        }

        callSession.Status = CallStatus.Connected;
        await _callSessionRepository.UpdateAsync(callSession, cancellationToken);

        _logger.LogInformation("Call session {CallSessionId} marked as Connected", callSession.Id);

        // Initialize call flow (play person detection prompt)
        await InitializeCallFlowAsync(callConnectionId, cancellationToken);
    }

    public async Task HandleCallDisconnectedAsync(string callConnectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling CallDisconnected event for {CallConnectionId}", callConnectionId);

        var callSession = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);
        if (callSession == null)
        {
            _logger.LogWarning("Call session not found for CallConnectionId: {CallConnectionId}", callConnectionId);
            return;
        }

        // Only mark as Disconnected if not already in a terminal state (Completed, VoicemailMessage, etc.)
        if (callSession.Status != CallStatus.Completed && callSession.Status != CallStatus.VoicemailMessage)
        {
            callSession.Status = CallStatus.Disconnected;
            callSession.EndTime = DateTime.UtcNow;
            await _callSessionRepository.UpdateAsync(callSession, cancellationToken);

            _logger.LogInformation("Call session {CallSessionId} marked as Disconnected", callSession.Id);
        }
        else
        {
            _logger.LogInformation("Call session {CallSessionId} already has terminal status {Status}, preserving it",
                callSession.Id, callSession.Status);
        }
    }

    public async Task HandleCallFailedAsync(string callConnectionId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling CallFailed event for {CallConnectionId}. Reason: {Reason}", callConnectionId, reason);

        var callSession = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);
        if (callSession == null)
        {
            _logger.LogWarning("Call session not found for CallConnectionId: {CallConnectionId}", callConnectionId);
            return;
        }

        callSession.Status = CallStatus.Failed;
        callSession.EndTime = DateTime.UtcNow;
        await _callSessionRepository.UpdateAsync(callSession, cancellationToken);

        _logger.LogInformation("Call session {CallSessionId} marked as Failed", callSession.Id);
    }

    public async Task HandleNoAnswerAsync(string callConnectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling NoAnswer timeout for {CallConnectionId}", callConnectionId);

        var callSession = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);
        if (callSession == null)
        {
            _logger.LogWarning("Call session not found for CallConnectionId: {CallConnectionId}", callConnectionId);
            return;
        }

        callSession.Status = CallStatus.NoAnswer;
        callSession.EndTime = DateTime.UtcNow;
        await _callSessionRepository.UpdateAsync(callSession, cancellationToken);

        _logger.LogInformation("Call session {CallSessionId} marked as NoAnswer", callSession.Id);
    }

    public async Task InitializeCallFlowAsync(string callConnectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing call flow for {CallConnectionId}", callConnectionId);

        var callSession = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);
        if (callSession == null)
        {
            _logger.LogWarning("Call session not found for CallConnectionId: {CallConnectionId}", callConnectionId);
            return;
        }

        // Get member to retrieve phone number
        var member = await _memberRepository.GetByIdAsync(callSession.MemberId, cancellationToken);
        if (member == null)
        {
            _logger.LogWarning("Member {MemberId} not found for call session {CallSessionId}", callSession.MemberId, callSession.Id);
            return;
        }

        // Implement Azure's documented AMD approach:
        // 1. After CallConnected, play person detection prompt with DTMF recognition
        // 2. If DTMF received (RecognizeCompleted) = Human detected → Continue with questions
        // 3. If timeout (RecognizeFailed) = Voicemail detected → Leave message

        // Initialize state starting at question 0 (person detection)
        _callSessionState.InitializeCallState(callConnectionId, callSession.Id);
        _logger.LogInformation("Initialized call state for session {CallSessionId}, starting AMD person detection", callSession.Id);

        // Play person detection prompt (Azure AMD pattern)
        var callConnection = _callAutomationClient.GetCallConnection(callConnectionId);
        await _questionService.PlayPersonDetectionPromptAsync(callConnection, member.PhoneNumber, cancellationToken);
    }

    /// <summary>
    /// Handles person detection timeout per Azure's AMD pattern.
    /// Timeout = voicemail detected (Scenario 2) → Leave callback message.
    /// </summary>
    public async Task HandlePersonDetectionTimeoutAsync(string callConnectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Person detection timeout (voicemail detected per Azure AMD) for {CallConnectionId}", callConnectionId);

        var callSession = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);
        if (callSession == null)
        {
            _logger.LogWarning("Call session not found for CallConnectionId: {CallConnectionId}", callConnectionId);
            return;
        }

        // Mark as VoicemailMessage (Scenario 2: Voicemail detected)
        callSession.Status = CallStatus.VoicemailMessage;
        await _callSessionRepository.UpdateAsync(callSession, cancellationToken);

        _logger.LogInformation("Call session {CallSessionId} marked as VoicemailMessage, leaving callback message", callSession.Id);

        // Leave voicemail callback message (Scenario 2)
        var callConnection = _callAutomationClient.GetCallConnection(callConnectionId);
        await _questionService.PlayVoicemailCallbackMessageAsync(callConnection, cancellationToken);

        // PlayCompleted event will trigger hangup
    }

    public async Task HandleDtmfResponseAsync(string callConnectionId, string dtmfTones, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling DTMF response '{DtmfTones}' for {CallConnectionId}", dtmfTones, callConnectionId);

        var state = _callSessionState.GetCallState(callConnectionId);
        if (state == null)
        {
            _logger.LogWarning("Call state not found for {CallConnectionId}", callConnectionId);
            return;
        }

        // Progress to next question
        var nextQuestion = _callSessionState.ProgressToNextQuestion(callConnectionId);
        _logger.LogInformation("Progressed to question {QuestionNumber} for {CallConnectionId}", nextQuestion, callConnectionId);

        // If we've completed all 3 questions, mark call as Completed and play completion message
        if (nextQuestion > 3)
        {
            var callSession = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);
            if (callSession != null)
            {
                callSession.Status = CallStatus.Completed;
                callSession.EndTime = DateTime.UtcNow;
                await _callSessionRepository.UpdateAsync(callSession, cancellationToken);
                _logger.LogInformation("Call session {CallSessionId} marked as Completed", callSession.Id);

                // Play completion message before hanging up
                var callConnection = _callAutomationClient.GetCallConnection(callConnectionId);
                await _questionService.PlayCompletionMessageAsync(callConnection, cancellationToken);
                _logger.LogInformation("Playing completion message for {CallConnectionId}, will hang up after message completes", callConnectionId);
            }

            // Don't clean up state here - will be cleaned up in HandlePlayCompletedAsync
            return;
        }

        // Get member phone number for next question
        var session = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Call session not found for CallConnectionId: {CallConnectionId}", callConnectionId);
            return;
        }

        var member = await _memberRepository.GetByIdAsync(session.MemberId, cancellationToken);
        if (member == null)
        {
            _logger.LogWarning("Member {MemberId} not found", session.MemberId);
            return;
        }

        // Play next question
        var callConnection2 = _callAutomationClient.GetCallConnection(callConnectionId);
        await _questionService.PlayHealthcareQuestionAsync(callConnection2, nextQuestion, member.PhoneNumber, cancellationToken);
    }

    public async Task HandlePlayCompletedAsync(string callConnectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling PlayCompleted event for {CallConnectionId}", callConnectionId);

        var callSession = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);
        if (callSession == null)
        {
            _logger.LogWarning("Call session not found for CallConnectionId: {CallConnectionId}", callConnectionId);
            return;
        }

        // If status is VoicemailMessage or Completed, hang up the call
        if (callSession.Status == CallStatus.VoicemailMessage || callSession.Status == CallStatus.Completed)
        {
            _logger.LogInformation("{Status} message completed, hanging up call {CallConnectionId}",
                callSession.Status, callConnectionId);
            var callConnection = _callAutomationClient.GetCallConnection(callConnectionId);
            await callConnection.HangUpAsync(forEveryone: true, cancellationToken);

            // Only update EndTime for VoicemailMessage (Completed already has EndTime set)
            if (callSession.Status == CallStatus.VoicemailMessage)
            {
                callSession.EndTime = DateTime.UtcNow;
                await _callSessionRepository.UpdateAsync(callSession, cancellationToken);
            }

            _callSessionState.RemoveCallState(callConnectionId);
        }
    }

    public async Task HandleRecognizeCompletedAsync(string callConnectionId, string dtmfTones, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling RecognizeCompleted for {CallConnectionId} with DTMF: {DtmfTones}", callConnectionId, dtmfTones);

        var callSession = await _callSessionRepository.GetByCallConnectionIdAsync(callConnectionId, cancellationToken);
        if (callSession == null)
        {
            _logger.LogWarning("Call session not found for CallConnectionId: {CallConnectionId}", callConnectionId);
            return;
        }

        var state = _callSessionState.GetCallState(callConnectionId);
        if (state == null)
        {
            _logger.LogWarning("Call state not found for {CallConnectionId}", callConnectionId);
            return;
        }

        // Save the response only for healthcare questions (1-3), not for person detection (0)
        if (state.CurrentQuestionNumber >= 1 && state.CurrentQuestionNumber <= 3)
        {
            await SaveCallResponseAsync(callSession.Id, state.CurrentQuestionNumber, dtmfTones, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Skipping response save for question {QuestionNumber} (person detection)", state.CurrentQuestionNumber);
        }

        // Progress to next question
        await HandleDtmfResponseAsync(callConnectionId, dtmfTones, cancellationToken);
    }

    public async Task SaveCallResponseAsync(int callSessionId, int questionNumber, string dtmfResponse, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving response for CallSession {CallSessionId}, Question {QuestionNumber}: {Response}",
            callSessionId, questionNumber, dtmfResponse);

        var questionText = questionNumber switch
        {
            1 => Constants.HealthcareQuestions.Question1,
            2 => Constants.HealthcareQuestions.Question2,
            3 => Constants.HealthcareQuestions.Question3,
            _ => "Unknown question"
        };

        // Convert DTMF string to integer (1 = yes, 2 = no)
        if (!int.TryParse(dtmfResponse, out var responseValue) || (responseValue != 1 && responseValue != 2))
        {
            _logger.LogWarning("Invalid DTMF response value: {Response}. Expected 1 or 2.", dtmfResponse);
            throw new ArgumentException($"Invalid DTMF response: {dtmfResponse}. Expected 1 or 2.", nameof(dtmfResponse));
        }

        var response = new CallResponse
        {
            CallSessionId = callSessionId,
            QuestionNumber = questionNumber,
            QuestionText = questionText,
            ResponseValue = responseValue,
            RespondedAt = DateTime.UtcNow
        };

        await _callResponseRepository.CreateAsync(response, cancellationToken);
        _logger.LogInformation("Response saved successfully for CallSession {CallSessionId}, Question {QuestionNumber}",
            callSessionId, questionNumber);
    }
}
