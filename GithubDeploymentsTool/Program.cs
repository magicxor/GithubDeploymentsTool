using GithubDeploymentsTool.Enums;
using GithubDeploymentsTool.Models;
using GithubDeploymentsTool.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace GithubDeploymentsTool;

public static class Program
{
    private static readonly LoggingConfiguration LoggingConfiguration = new XmlLoggingConfiguration("nlog.config");

    public static void Main(string[] args)
    {
        // NLog: setup the logger first to catch all errors
        LogManager.Configuration = LoggingConfiguration;
        try
        {
            var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, config) =>
            {
                config
                    .AddEnvironmentVariables("GHDTOOL_")
                    .AddJsonFile("appsettings.json", optional: true);
            })
            .ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                loggingBuilder.AddNLog(LoggingConfiguration);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services
                    .AddOptions<GithubDeploymentsToolOptions>()
                    .Bind(hostContext.Configuration.GetSection(nameof(OptionSections.GithubDeploymentsToolOptions)))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services
                    .AddGithubClient()
                    .ConfigureHttpClient(httpClient => httpClient.BaseAddress = new Uri("https://api.github.com/graphql"));

                services.AddSingleton<TimeProvider>(_ => TimeProvider.System);
                services.AddHostedService<Worker>();
            })
            .Build();

            host.Run();
        }
        catch (Exception ex)
        {
            // NLog: catch setup errors
            LogManager.GetCurrentClassLogger().Error(ex, "Stopped program because of exception");
            throw;
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            LogManager.Shutdown();
        }
    }
}
