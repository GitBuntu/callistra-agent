using CallistraAgent.Functions.Data;
using CallistraAgent.Functions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<Data.Repositories.IMemberRepository, Data.Repositories.MemberRepository>();
        services.AddScoped<Data.Repositories.ICallSessionRepository, Data.Repositories.CallSessionRepository>();

        // Register state management
        services.AddSingleton<CallSessionState>();

        Services = services.BuildServiceProvider();
    }

    public IServiceScope CreateScope() => Services.CreateScope();
}
