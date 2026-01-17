using Azure.Communication.CallAutomation;
using Azure.Identity;
using CallistraAgent.Functions.Configuration;
using CallistraAgent.Functions.Data;
using CallistraAgent.Functions.Data.Repositories;
using CallistraAgent.Functions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Add Application Insights telemetry
        services.AddApplicationInsightsTelemetryWorkerService();

        // Configure Azure Communication Services options
        services.Configure<AzureCommunicationServicesOptions>(
            context.Configuration.GetSection(AzureCommunicationServicesOptions.SectionName));

        // Configure Database options
        services.Configure<DatabaseOptions>(
            context.Configuration.GetSection(DatabaseOptions.SectionName));

        // Add DbContext with connection pooling and retry policies
        var connectionString = context.Configuration.GetConnectionString("CallistraAgentDb");
        services.AddDbContext<CallistraAgentDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });
        });

        // Register CallAutomationClient as singleton with Managed Identity
        var acsConnectionString = context.Configuration[$"{AzureCommunicationServicesOptions.SectionName}:ConnectionString"];
        var acsEndpoint = context.Configuration[$"{AzureCommunicationServicesOptions.SectionName}:Endpoint"];

        if (!string.IsNullOrEmpty(acsConnectionString))
        {
            services.AddSingleton(sp =>
            {
                // Use Managed Identity for Cognitive Services integration
                if (!string.IsNullOrEmpty(acsEndpoint))
                {
                    return new CallAutomationClient(new Uri(acsEndpoint), new DefaultAzureCredential());
                }
                // Fallback to connection string (local dev without managed identity)
                return new CallAutomationClient(acsConnectionString);
            });
        }

        // Register repositories as scoped services
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ICallSessionRepository, CallSessionRepository>();
        services.AddScoped<ICallResponseRepository, CallResponseRepository>();

        // Register business services
        services.AddScoped<ICallService, CallService>();
        services.AddScoped<IQuestionService, QuestionService>();

        // Register CallSessionState as singleton (in-memory cache)
        services.AddSingleton<CallSessionState>();
    })
    .Build();

host.Run();
