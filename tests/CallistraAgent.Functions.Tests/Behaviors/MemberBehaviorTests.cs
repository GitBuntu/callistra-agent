using CallistraAgent.Functions.Data;
using CallistraAgent.Functions.Models;
using CallistraAgent.Functions.Data.Repositories;
using CallistraAgent.Functions.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CallistraAgent.Functions.Tests.Behaviors;

/// <summary>
/// Behavior tests for member management
/// Covers: T046 (valid member), T047 (member not found)
/// </summary>
public class MemberBehaviorTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MemberBehaviorTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GivenValidMemberId_WhenRetrievingMember_ThenMemberIsReturned()
    {
        // Given - a member exists in the database
        using var scope = _factory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CallistraAgentDbContext>();
        var memberRepo = scope.ServiceProvider.GetRequiredService<IMemberRepository>();

        var member = new Member
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+15555551001",
            Program = "Medicare",
            Status = "Active"
        };
        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();

        // When - retrieving the member by ID
        var result = await memberRepo.GetByIdAsync(member.Id);

        // Then - the member details are correct
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.PhoneNumber.Should().Be("+15555551001");
        result.Program.Should().Be("Medicare");
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GivenInvalidMemberId_WhenRetrievingMember_ThenNullIsReturned()
    {
        // Given - a member ID that doesn't exist
        using var scope = _factory.CreateScope();
        var memberRepo = scope.ServiceProvider.GetRequiredService<IMemberRepository>();

        // When - attempting to retrieve with invalid ID
        var result = await memberRepo.GetByIdAsync(99999);

        // Then - null is returned
        result.Should().BeNull();
    }
}
