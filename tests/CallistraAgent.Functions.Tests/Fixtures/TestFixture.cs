using Azure.Communication.CallAutomation;
using CallistraAgent.Functions.Data;
using CallistraAgent.Functions.Data.Repositories;
using CallistraAgent.Functions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace CallistraAgent.Functions.Tests.Fixtures;

/// <summary>
/// Test fixture providing in-memory database and services for behavior tests
/// </summary>
public class TestWebApplicationFactory
{
    public IServiceProvider Services { get; }

    public TestWebApplicationFactory()
    {
        var services = new ServiceCollection();

        // Add in-memory database with unique name per test instance
        var databaseName = $"TestDb_{Guid.NewGuid()}";
        services.AddDbContext<CallistraAgentDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        // Register repositories
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ICallSessionRepository, CallSessionRepository>();
        services.AddScoped<ICallResponseRepository, CallResponseRepository>();

        // Register services
        services.AddScoped<CallService>();
        services.AddScoped<IQuestionService, QuestionService>();

        // Register state management
        services.AddSingleton<CallSessionState>();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add mocked Azure Communication Services
        var mockCallAutomationClient = new Mock<CallAutomationClient>();
        services.AddSingleton(mockCallAutomationClient.Object);

        Services = services.BuildServiceProvider();
    }

    public IServiceScope CreateScope() => Services.CreateScope();
}
