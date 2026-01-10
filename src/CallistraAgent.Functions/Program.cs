using Azure.Communication.CallAutomation;
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

        // Register CallAutomationClient as singleton
        var acsConnectionString = context.Configuration[$"{AzureCommunicationServicesOptions.SectionName}:ConnectionString"];
        if (!string.IsNullOrEmpty(acsConnectionString))
        {
            services.AddSingleton(sp =>
            {
                return new CallAutomationClient(acsConnectionString);
            });
        }

        // Register repositories as scoped services
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ICallSessionRepository, CallSessionRepository>();
        services.AddScoped<ICallResponseRepository, CallResponseRepository>();

        // Register business services
        services.AddScoped<ICallService, CallService>();
    })
    .Build();

host.Run();
