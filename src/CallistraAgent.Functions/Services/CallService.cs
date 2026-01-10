using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallistraAgent.Functions.Configuration;
using CallistraAgent.Functions.Data.Repositories;
using CallistraAgent.Functions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CallistraAgent.Functions.Services;

/// <summary>
/// Service implementation for call management operations
/// </summary>
public class CallService : ICallService
{
    private readonly ICallSessionRepository _callSessionRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly CallAutomationClient _callAutomationClient;
    private readonly AzureCommunicationServicesOptions _acsOptions;
    private readonly ILogger<CallService> _logger;

    public CallService(
        ICallSessionRepository callSessionRepository,
        IMemberRepository memberRepository,
        CallAutomationClient callAutomationClient,
        IOptions<AzureCommunicationServicesOptions> acsOptions,
        ILogger<CallService> logger)
    {
        _callSessionRepository = callSessionRepository;
        _memberRepository = memberRepository;
        _callAutomationClient = callAutomationClient;
        _acsOptions = acsOptions.Value;
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

        try
        {
            // Initiate call via Azure Communication Services
            var callbackUri = new Uri($"{_acsOptions.CallbackBaseUrl}/api/calls/events");
            var target = new PhoneNumberIdentifier(member.PhoneNumber);
            var caller = new PhoneNumberIdentifier(_acsOptions.PhoneNumber);

            var createCallOptions = new CreateCallOptions(
                new CallInvite(target, caller),
                callbackUri
            );

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

        callSession.Status = CallStatus.Disconnected;
        callSession.EndTime = DateTime.UtcNow;
        await _callSessionRepository.UpdateAsync(callSession, cancellationToken);

        _logger.LogInformation("Call session {CallSessionId} marked as Disconnected", callSession.Id);
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
}
