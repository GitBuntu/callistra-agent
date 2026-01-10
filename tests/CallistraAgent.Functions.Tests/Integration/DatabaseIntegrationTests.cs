using CallistraAgent.Functions.Data;
using CallistraAgent.Functions.Models;
using CallistraAgent.Functions.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CallistraAgent.Functions.Tests.Integration;

public class DatabaseIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DatabaseIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CallResponse_ForeignKeyConstraints_WorkCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CallistraAgentDbContext>();

        var member = new Member
        {
            FirstName = "Test",
            LastName = "Member",
            PhoneNumber = "+15555551236",
            Program = "Test Program",
            Status = "Active"
        };
        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();

        var callSession = new CallSession
        {
            MemberId = member.Id,
            CallConnectionId = "test-connection-789",
            Status = CallStatus.Connected
        };
        dbContext.CallSessions.Add(callSession);
        await dbContext.SaveChangesAsync();

        // Act & Assert - Create CallResponse with valid foreign key
        var callResponse = new CallResponse
        {
            CallSessionId = callSession.Id,
            QuestionNumber = 1,
            QuestionText = "Test question?",
            ResponseValue = 1,
            RespondedAt = DateTime.UtcNow
        };

        dbContext.CallResponses.Add(callResponse);
        var saveAction = async () => await dbContext.SaveChangesAsync();

        await saveAction.Should().NotThrowAsync();

        // Assert - Verify response was saved
        var savedResponse = await dbContext.CallResponses
            .Include(cr => cr.CallSession)
            .FirstAsync(cr => cr.Id == callResponse.Id);

        savedResponse.CallSession.Should().NotBeNull();
        savedResponse.CallSession.Id.Should().Be(callSession.Id);
    }

    [Fact(Skip = "In-memory database doesn't enforce foreign key constraints")]
    public async Task CallResponse_InvalidCallSessionId_ThrowsException()
    {
        // This test is skipped because Entity Framework's in-memory database
        // does not enforce foreign key constraints like a real database would.
        // To properly test this, we would need to use SQLite or SQL Server.
        await Task.CompletedTask;
    }

    [Fact(Skip = "In-memory database doesn't enforce unique constraints")]
    public async Task CallResponse_UniqueConstraint_PreventsDuplicateQuestionResponses()
    {
        // This test is skipped because Entity Framework's in-memory database
        // does not enforce unique constraints like a real database would.
        // To properly test this, we would need to use SQLite or SQL Server.
        await Task.CompletedTask;
    }

    [Fact]
    public async Task CallSession_Navigation_LoadsResponsesCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CallistraAgentDbContext>();

        var member = new Member
        {
            FirstName = "Test",
            LastName = "Member",
            PhoneNumber = "+15555551238",
            Program = "Test Program",
            Status = "Active"
        };
        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();

        var callSession = new CallSession
        {
            MemberId = member.Id,
            CallConnectionId = "test-navigation",
            Status = CallStatus.Connected
        };
        dbContext.CallSessions.Add(callSession);
        await dbContext.SaveChangesAsync();

        // Add multiple responses
        var responses = new[]
        {
            new CallResponse { CallSessionId = callSession.Id, QuestionNumber = 1, QuestionText = "Q1?", ResponseValue = 1 },
            new CallResponse { CallSessionId = callSession.Id, QuestionNumber = 2, QuestionText = "Q2?", ResponseValue = 2 },
            new CallResponse { CallSessionId = callSession.Id, QuestionNumber = 3, QuestionText = "Q3?", ResponseValue = 1 }
        };
        dbContext.CallResponses.AddRange(responses);
        await dbContext.SaveChangesAsync();

        // Act
        var loadedSession = await dbContext.CallSessions
            .Include(cs => cs.Responses)
            .FirstAsync(cs => cs.Id == callSession.Id);

        // Assert
        loadedSession.Responses.Should().HaveCount(3);
        loadedSession.Responses.Select(r => r.QuestionNumber).Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }
}
