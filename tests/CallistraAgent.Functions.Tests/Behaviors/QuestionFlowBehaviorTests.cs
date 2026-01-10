using CallistraAgent.Functions.Services;
using CallistraAgent.Functions.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CallistraAgent.Functions.Tests.Behaviors;

/// <summary>
/// Behavior tests for question flow state management
/// Covers: T072 (person detection), T073 (first question), T074 (question progression)
/// </summary>
public class QuestionFlowBehaviorTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public QuestionFlowBehaviorTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void GivenNewCall_WhenInitializing_ThenStartsAtPersonDetection()
    {
        // Given - a new call with connection ID
        var callSessionState = _factory.Services.GetRequiredService<CallSessionState>();
        var connectionId = "new-call-123";

        // When - initializing call state (starts at person detection = question 0)
        callSessionState.InitializeCallState(connectionId, callSessionId: 1);

        // Then - current question is 0 (person detection)
        var state = callSessionState.GetCallState(connectionId);
        state.Should().NotBeNull();
        state!.CurrentQuestionNumber.Should().Be(0);
    }

    [Fact]
    public void GivenPersonDetected_WhenProgressingToFirstQuestion_ThenMovesToQuestion1()
    {
        // Given - person detection is complete (at question 0)
        var callSessionState = _factory.Services.GetRequiredService<CallSessionState>();
        var connectionId = "person-detected-456";

        callSessionState.InitializeCallState(connectionId, callSessionId: 2);

        // When - moving to first healthcare question
        var nextQuestion = callSessionState.ProgressToNextQuestion(connectionId);

        // Then - now at question 1 (first healthcare question)
        nextQuestion.Should().Be(1);
        var state = callSessionState.GetCallState(connectionId);
        state!.CurrentQuestionNumber.Should().Be(1);
    }

    [Fact]
    public void GivenThreeQuestions_WhenProgressingThroughAll_ThenReachesQuestion4()
    {
        // Given - call flow started at question 0 (person detection)
        var callSessionState = _factory.Services.GetRequiredService<CallSessionState>();
        var connectionId = "full-flow-789";

        callSessionState.InitializeCallState(connectionId, callSessionId: 3);

        // When - progressing through all questions: 0→1→2→3→4
        callSessionState.ProgressToNextQuestion(connectionId); // 0→1 (Q1)
        callSessionState.GetCallState(connectionId)!.CurrentQuestionNumber.Should().Be(1);

        callSessionState.ProgressToNextQuestion(connectionId); // 1→2 (Q2)
        callSessionState.GetCallState(connectionId)!.CurrentQuestionNumber.Should().Be(2);

        callSessionState.ProgressToNextQuestion(connectionId); // 2→3 (Q3)
        callSessionState.GetCallState(connectionId)!.CurrentQuestionNumber.Should().Be(3);

        callSessionState.ProgressToNextQuestion(connectionId); // 3→4 (Complete)

        // Then - at question 4 means all 3 questions completed
        callSessionState.GetCallState(connectionId)!.CurrentQuestionNumber.Should().Be(4);

        // Cleanup
        callSessionState.RemoveCallState(connectionId);
    }

    [Fact]
    public void GivenCompletedFlow_WhenRemoving_ThenStateIsGone()
    {
        // Given - an initialized call state
        var callSessionState = _factory.Services.GetRequiredService<CallSessionState>();
        var connectionId = "cleanup-test-999";

        callSessionState.InitializeCallState(connectionId, callSessionId: 4);

        // When - removing the call state
        var removed = callSessionState.RemoveCallState(connectionId);

        // Then - state no longer exists
        removed.Should().BeTrue();
        callSessionState.GetCallState(connectionId).Should().BeNull();
    }
}
