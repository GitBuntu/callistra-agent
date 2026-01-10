using CallistraAgent.Functions.Data;
using CallistraAgent.Functions.Models;
using CallistraAgent.Functions.Data.Repositories;
using CallistraAgent.Functions.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CallistraAgent.Functions.Tests.Behaviors;

/// <summary>
/// Behavior tests for call session management
/// Covers: T048 (active call detection), T049 (status updates), T050 (call completion)
/// </summary>
public class CallSessionBehaviorTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CallSessionBehaviorTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GivenMemberWithActiveCall_WhenCheckingForActiveCall_ThenReturnsTrue()
    {
        // Given - a member with an active call session
        using var scope = _factory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CallistraAgentDbContext>();
        var callSessionRepo = scope.ServiceProvider.GetRequiredService<ICallSessionRepository>();

        var member = new Member
        {
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "+15555551002",
            Program = "Medicaid",
            Status = "Active"
        };
        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();

        var activeSession = new CallSession
        {
            MemberId = member.Id,
            CallConnectionId = "active-call-123",
            Status = CallStatus.Connected,
            StartTime = DateTime.UtcNow
        };
        dbContext.CallSessions.Add(activeSession);
        await dbContext.SaveChangesAsync();

        // When - checking for active calls
        var activeCall = await callSessionRepo.GetActiveCallForMemberAsync(member.Id);

        // Then - the active session is found
        activeCall.Should().NotBeNull();
        activeCall!.Status.Should().Be(CallStatus.Connected);
        activeCall.CallConnectionId.Should().Be("active-call-123");
    }

    [Fact]
    public async Task GivenInitiatedCall_WhenUpdatingToConnected_ThenStatusChanges()
    {
        // Given - a call session in Initiated status
        using var scope = _factory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CallistraAgentDbContext>();
        var callSessionRepo = scope.ServiceProvider.GetRequiredService<ICallSessionRepository>();

        var member = new Member
        {
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+15555551003",
            Program = "Medicare",
            Status = "Active"
        };
        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();

        var session = new CallSession
        {
            MemberId = member.Id,
            CallConnectionId = "initiated-call-456",
            Status = CallStatus.Initiated,
            StartTime = DateTime.UtcNow
        };
        dbContext.CallSessions.Add(session);
        await dbContext.SaveChangesAsync();

        // When - updating status to Connected
        session.Status = CallStatus.Connected;
        await callSessionRepo.UpdateAsync(session);

        // Then - the status is persisted
        var updated = await callSessionRepo.GetByCallConnectionIdAsync("initiated-call-456");
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(CallStatus.Connected);
    }

    [Fact]
    public async Task GivenConnectedCall_WhenCompletingCall_ThenEndTimeIsSet()
    {
        // Given - a connected call session
        using var scope = _factory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CallistraAgentDbContext>();
        var callSessionRepo = scope.ServiceProvider.GetRequiredService<ICallSessionRepository>();

        var member = new Member
        {
            FirstName = "Test",
            LastName = "User2",
            PhoneNumber = "+15555551004",
            Program = "Medicare",
            Status = "Active"
        };
        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();

        var session = new CallSession
        {
            MemberId = member.Id,
            CallConnectionId = "connected-call-789",
            Status = CallStatus.Connected,
            StartTime = DateTime.UtcNow.AddMinutes(-5)
        };
        dbContext.CallSessions.Add(session);
        await dbContext.SaveChangesAsync();

        // When - completing the call
        session.Status = CallStatus.Completed;
        session.EndTime = DateTime.UtcNow;
        await callSessionRepo.UpdateAsync(session);

        // Then - end time is set and status is completed
        var completed = await callSessionRepo.GetByCallConnectionIdAsync("connected-call-789");
        completed.Should().NotBeNull();
        completed!.Status.Should().Be(CallStatus.Completed);
        completed.EndTime.Should().NotBeNull();
        completed.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
