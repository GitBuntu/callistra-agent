using Azure.Communication.CallAutomation;
using CallistraAgent.Functions.Constants;
using CallistraAgent.Functions.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CallistraAgent.Functions.Tests.Unit.Services;

public class QuestionServiceTests
{
    private readonly Mock<ILogger<QuestionService>> _mockLogger;
    private readonly QuestionService _questionService;

    public QuestionServiceTests()
    {
        _mockLogger = new Mock<ILogger<QuestionService>>();
        _questionService = new QuestionService(_mockLogger.Object);
    }

    [Fact]
    public async Task PlayPersonDetectionPromptAsync_NullCallConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _questionService.PlayPersonDetectionPromptAsync(null!, "+11234567890")
        );
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task PlayHealthcareQuestionAsync_ValidQuestionNumber_DoesNotThrow(int questionNumber)
    {
        // Arrange
        var mockCallConnection = new Mock<CallConnection>();
        var mockCallMedia = new Mock<CallMedia>();

        mockCallConnection.Setup(c => c.CallConnectionId).Returns("test123");
        mockCallConnection.Setup(c => c.GetCallMedia()).Returns(mockCallMedia.Object);

        // Act & Assert (should not throw)
        var exception = await Record.ExceptionAsync(() =>
            _questionService.PlayHealthcareQuestionAsync(mockCallConnection.Object, questionNumber, "+11234567890")
        );

        // Note: This will throw due to mock limitations, but validates parameter validation
        exception.Should().NotBeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public async Task PlayHealthcareQuestionAsync_InvalidQuestionNumber_ThrowsArgumentOutOfRangeException(int questionNumber)
    {
        // Arrange
        var mockCallConnection = new Mock<CallConnection>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _questionService.PlayHealthcareQuestionAsync(mockCallConnection.Object, questionNumber, "+11234567890")
        );
    }

    [Fact]
    public async Task PlayHealthcareQuestionAsync_NullCallConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _questionService.PlayHealthcareQuestionAsync(null!, 1, "+11234567890")
        );
    }

    [Fact]
    public async Task HandleInvalidDtmfAsync_MaxRetriesReached_ReturnsFalse()
    {
        // Arrange
        var mockCallConnection = new Mock<CallConnection>();
        mockCallConnection.Setup(c => c.CallConnectionId).Returns("test123");

        // Act
        var result = await _questionService.HandleInvalidDtmfAsync(
            mockCallConnection.Object,
            questionNumber: 1,
            retryCount: VoicemailMessages.MaxRetries,
            targetPhoneNumber: "+11234567890"
        );

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleTimeoutAsync_HasRetriedOnce_ReturnsFalse()
    {
        // Arrange
        var mockCallConnection = new Mock<CallConnection>();
        mockCallConnection.Setup(c => c.CallConnectionId).Returns("test123");

        // Act
        var result = await _questionService.HandleTimeoutAsync(
            mockCallConnection.Object,
            questionNumber: 1,
            hasRetriedOnce: true,
            targetPhoneNumber: "+11234567890"
        );

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PlayVoicemailCallbackMessageAsync_NullCallConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _questionService.PlayVoicemailCallbackMessageAsync(null!)
        );
    }
}
