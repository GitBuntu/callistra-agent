using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallistraAgent.Functions.Configuration;
using CallistraAgent.Functions.Constants;
using CallistraAgent.Functions.Data.Repositories;
using CallistraAgent.Functions.Models;
using CallistraAgent.Functions.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CallistraAgent.Functions.Tests.Unit.Services;

public class CallServiceTests
{
    private readonly Mock<ICallSessionRepository> _mockCallSessionRepository;
    private readonly Mock<IMemberRepository> _mockMemberRepository;
    private readonly Mock<ICallResponseRepository> _mockCallResponseRepository;
    private readonly Mock<CallAutomationClient> _mockCallAutomationClient;
    private readonly Mock<IQuestionService> _mockQuestionService;
    private readonly CallSessionState _callSessionState;
    private readonly Mock<ILogger<CallService>> _mockLogger;
    private readonly AzureCommunicationServicesOptions _acsOptions;
    private readonly CallService _callService;

    public CallServiceTests()
    {
        _mockCallSessionRepository = new Mock<ICallSessionRepository>();
        _mockMemberRepository = new Mock<IMemberRepository>();
        _mockCallResponseRepository = new Mock<ICallResponseRepository>();
        _mockCallAutomationClient = new Mock<CallAutomationClient>();
        _mockQuestionService = new Mock<IQuestionService>();
        _callSessionState = new CallSessionState();
        _mockLogger = new Mock<ILogger<CallService>>();

        _acsOptions = new AzureCommunicationServicesOptions
        {
            CallbackBaseUrl = "https://test.devtunnels.ms",
            PhoneNumber = "+15555555555",
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey"
        };

        var options = Options.Create(_acsOptions);

        _callService = new CallService(
            _mockCallSessionRepository.Object,
            _mockMemberRepository.Object,
            _mockCallResponseRepository.Object,
            _mockCallAutomationClient.Object,
            options,
            _mockQuestionService.Object,
            _callSessionState,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task InitiateCallAsync_MemberNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        int memberId = 999;
        _mockMemberRepository.Setup(r => r.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _callService.InitiateCallAsync(memberId)
        );

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task InitiateCallAsync_MemberNotActive_ThrowsInvalidOperationException()
    {
        // Arrange
        int memberId = 1;
        var member = new Member
        {
            Id = memberId,
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+12025551234",
            Program = "Diabetes Care",
            Status = "Pending"
        };

        _mockMemberRepository.Setup(r => r.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _callService.InitiateCallAsync(memberId)
        );

        exception.Message.Should().Contain("not eligible");
        exception.Message.Should().Contain("Pending");
    }

    [Fact]
    public async Task InitiateCallAsync_MemberHasActiveCall_ThrowsInvalidOperationException()
    {
        // Arrange
        int memberId = 1;
        var member = new Member
        {
            Id = memberId,
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+12025551234",
            Program = "Diabetes Care",
            Status = "Active"
        };

        var activeCall = new CallSession
        {
            Id = 10,
            MemberId = memberId,
            Status = CallStatus.Connected
        };

        _mockMemberRepository.Setup(r => r.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _mockCallSessionRepository.Setup(r => r.GetActiveCallForMemberAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeCall);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _callService.InitiateCallAsync(memberId)
        );

        exception.Message.Should().Contain("already has an active call");
    }

    [Fact]
    public async Task InitiateCallAsync_AcsServiceUnavailable_UpdatesCallSessionToFailed()
    {
        // Arrange
        int memberId = 1;
        var member = new Member
        {
            Id = memberId,
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+12025551234",
            Program = "Diabetes Care",
            Status = "Active"
        };

        var createdSession = new CallSession
        {
            Id = 1,
            MemberId = memberId,
            Status = CallStatus.Initiated,
            StartTime = DateTime.UtcNow
        };

        _mockMemberRepository.Setup(r => r.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _mockCallSessionRepository.Setup(r => r.GetActiveCallForMemberAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CallSession?)null);
        _mockCallSessionRepository.Setup(r => r.CreateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdSession);
        _mockCallAutomationClient.Setup(c => c.CreateCallAsync(It.IsAny<CreateCallOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("ACS service unavailable"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _callService.InitiateCallAsync(memberId)
        );

        exception.Message.Should().Contain("Failed to initiate call");

        // Verify session was updated to Failed status
        _mockCallSessionRepository.Verify(
            r => r.UpdateAsync(
                It.Is<CallSession>(cs => cs.Status == CallStatus.Failed && cs.EndTime != null),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    // Note: Full end-to-end call initiation with ACS tested manually with real phone calls
    // This test is omitted due to Azure SDK sealed types that cannot be mocked effectively

    [Fact]
    public async Task HandleCallConnectedAsync_CallSessionNotFound_LogsWarningAndReturns()
    {
        // Arrange
        string callConnectionId = "unknown123";
        _mockCallSessionRepository.Setup(r => r.GetByCallConnectionIdAsync(callConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CallSession?)null);

        // Act
        await _callService.HandleCallConnectedAsync(callConnectionId);

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );

        _mockCallSessionRepository.Verify(r => r.UpdateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleCallConnectedAsync_Success_UpdatesStatusToConnected()
    {
        // Arrange
        string callConnectionId = "acsCallConnection123";
        var callSession = new CallSession
        {
            Id = 1,
            MemberId = 1,
            CallConnectionId = callConnectionId,
            Status = CallStatus.Initiated,
            StartTime = DateTime.UtcNow
        };

        _mockCallSessionRepository.Setup(r => r.GetByCallConnectionIdAsync(callConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callSession);

        CallSession? updatedSession = null;
        _mockCallSessionRepository.Setup(r => r.UpdateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()))
            .Callback<CallSession, CancellationToken>((cs, ct) => updatedSession = cs)
            .Returns(Task.CompletedTask);

        // Act
        await _callService.HandleCallConnectedAsync(callConnectionId);

        // Assert
        updatedSession.Should().NotBeNull();
        updatedSession!.Status.Should().Be(CallStatus.Connected);

        _mockCallSessionRepository.Verify(r => r.UpdateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCallDisconnectedAsync_Success_UpdatesStatusAndEndTime()
    {
        // Arrange
        string callConnectionId = "acsCallConnection123";
        var callSession = new CallSession
        {
            Id = 1,
            MemberId = 1,
            CallConnectionId = callConnectionId,
            Status = CallStatus.Connected,
            StartTime = DateTime.UtcNow.AddMinutes(-5)
        };

        _mockCallSessionRepository.Setup(r => r.GetByCallConnectionIdAsync(callConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callSession);

        CallSession? updatedSession = null;
        _mockCallSessionRepository.Setup(r => r.UpdateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()))
            .Callback<CallSession, CancellationToken>((cs, ct) => updatedSession = cs)
            .Returns(Task.CompletedTask);

        // Act
        await _callService.HandleCallDisconnectedAsync(callConnectionId);

        // Assert
        updatedSession.Should().NotBeNull();
        updatedSession!.Status.Should().Be(CallStatus.Disconnected);
        updatedSession.EndTime.Should().NotBeNull();
        updatedSession.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        _mockCallSessionRepository.Verify(r => r.UpdateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCallFailedAsync_Success_UpdatesStatusToFailedAndSetsEndTime()
    {
        // Arrange
        string callConnectionId = "acsCallConnection123";
        string reason = "Network timeout";
        var callSession = new CallSession
        {
            Id = 1,
            MemberId = 1,
            CallConnectionId = callConnectionId,
            Status = CallStatus.Ringing,
            StartTime = DateTime.UtcNow.AddSeconds(-30)
        };

        _mockCallSessionRepository.Setup(r => r.GetByCallConnectionIdAsync(callConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callSession);

        CallSession? updatedSession = null;
        _mockCallSessionRepository.Setup(r => r.UpdateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()))
            .Callback<CallSession, CancellationToken>((cs, ct) => updatedSession = cs)
            .Returns(Task.CompletedTask);

        // Act
        await _callService.HandleCallFailedAsync(callConnectionId, reason);

        // Assert
        updatedSession.Should().NotBeNull();
        updatedSession!.Status.Should().Be(CallStatus.Failed);
        updatedSession.EndTime.Should().NotBeNull();
        updatedSession.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        _mockCallSessionRepository.Verify(r => r.UpdateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleNoAnswerAsync_Success_UpdatesStatusToNoAnswerAndSetsEndTime()
    {
        // Arrange
        string callConnectionId = "acsCallConnection123";
        var callSession = new CallSession
        {
            Id = 1,
            MemberId = 1,
            CallConnectionId = callConnectionId,
            Status = CallStatus.Ringing,
            StartTime = DateTime.UtcNow.AddSeconds(-60)
        };

        _mockCallSessionRepository.Setup(r => r.GetByCallConnectionIdAsync(callConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callSession);

        CallSession? updatedSession = null;
        _mockCallSessionRepository.Setup(r => r.UpdateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()))
            .Callback<CallSession, CancellationToken>((cs, ct) => updatedSession = cs)
            .Returns(Task.CompletedTask);

        // Act
        await _callService.HandleNoAnswerAsync(callConnectionId);

        // Assert
        updatedSession.Should().NotBeNull();
        updatedSession!.Status.Should().Be(CallStatus.NoAnswer);
        updatedSession.EndTime.Should().NotBeNull();
        updatedSession.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        _mockCallSessionRepository.Verify(r => r.UpdateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandlePersonDetectionTimeoutAsync_Success_UpdatesStatusToVoicemailMessage()
    {
        // Arrange
        string callConnectionId = "acsCallConnection123";
        var callSession = new CallSession
        {
            Id = 1,
            MemberId = 1,
            CallConnectionId = callConnectionId,
            Status = CallStatus.Connected,
            StartTime = DateTime.UtcNow.AddSeconds(-30)
        };

        _mockCallSessionRepository.Setup(r => r.GetByCallConnectionIdAsync(callConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callSession);

        CallSession? updatedSession = null;
        _mockCallSessionRepository.Setup(r => r.UpdateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()))
            .Callback<CallSession, CancellationToken>((cs, ct) => updatedSession = cs)
            .Returns(Task.CompletedTask);

        // Act
        await _callService.HandlePersonDetectionTimeoutAsync(callConnectionId);

        // Assert
        updatedSession.Should().NotBeNull();
        updatedSession!.Status.Should().Be(CallStatus.VoicemailMessage);
        _mockCallSessionRepository.Verify(r => r.UpdateAsync(It.IsAny<CallSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveCallResponseAsync_ValidDtmfResponse_SavesResponse()
    {
        // Arrange
        int callSessionId = 1;
        int questionNumber = 1;
        string dtmfResponse = "1";

        CallResponse? savedResponse = null;
        _mockCallResponseRepository.Setup(r => r.CreateAsync(It.IsAny<CallResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallResponse, CancellationToken>((cr, ct) => savedResponse = cr)
            .ReturnsAsync((CallResponse cr, CancellationToken ct) => cr);

        // Act
        await _callService.SaveCallResponseAsync(callSessionId, questionNumber, dtmfResponse);

        // Assert
        savedResponse.Should().NotBeNull();
        savedResponse!.CallSessionId.Should().Be(callSessionId);
        savedResponse.QuestionNumber.Should().Be(questionNumber);
        savedResponse.ResponseValue.Should().Be(1);
        savedResponse.QuestionText.Should().Be(HealthcareQuestions.Question1);
        _mockCallResponseRepository.Verify(r => r.CreateAsync(It.IsAny<CallResponse>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("0")]
    [InlineData("invalid")]
    [InlineData("")]
    public async Task SaveCallResponseAsync_InvalidDtmfResponse_ThrowsArgumentException(string invalidDtmf)
    {
        // Arrange
        int callSessionId = 1;
        int questionNumber = 1;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _callService.SaveCallResponseAsync(callSessionId, questionNumber, invalidDtmf)
        );

        exception.Message.Should().Contain("Invalid DTMF response");
        _mockCallResponseRepository.Verify(r => r.CreateAsync(It.IsAny<CallResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleRecognizeCompletedAsync_ValidDtmf_SavesResponseAndProgressesToNextQuestion()
    {
        // Arrange
        string callConnectionId = "acsCallConnection123";
        string dtmfTones = "2";
        var callSession = new CallSession
        {
            Id = 1,
            MemberId = 1,
            CallConnectionId = callConnectionId,
            Status = CallStatus.Connected
        };

        _mockCallSessionRepository.Setup(r => r.GetByCallConnectionIdAsync(callConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callSession);

        // Initialize call state
        _callSessionState.InitializeCallState(callConnectionId, callSession.Id);
        _callSessionState.ProgressToNextQuestion(callConnectionId); // Move to question 1

        // Act
        await _callService.HandleRecognizeCompletedAsync(callConnectionId, dtmfTones);

        // Assert
        _mockCallResponseRepository.Verify(r => r.CreateAsync(
            It.Is<CallResponse>(cr => cr.CallSessionId == callSession.Id && cr.ResponseValue == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
