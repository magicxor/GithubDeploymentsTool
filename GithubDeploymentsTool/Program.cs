using System.Net.Http.Headers;
using CommandLine;
using GithubDeploymentsTool.Enums;
using GithubDeploymentsTool.Extensions;
using GithubDeploymentsTool.Models;
using GithubDeploymentsTool.Models.CommandLine;
using GithubDeploymentsTool.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace GithubDeploymentsTool;

public static class Program
{
    // see: https://docs.microsoft.com/en-us/windows/desktop/Debug/system-error-codes
    private const int Success = 0;
    private const int ErrorBadArguments = 160;

    private static readonly LoggingConfiguration LoggingConfiguration = new XmlLoggingConfiguration("nlog.config");

    private static IHost BuildHost(AppOptions appOptions)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddEnvironmentVariables("GHDTOOL_");

                if (appOptions.List != null)
                {
                    config.AddInMemoryCollection(typeof(ListDeploymentsOptions)
                        .ListProperties()
                        .Select(prop => new KeyValuePair<string, string?>(
                            $"{nameof(OptionSections.GithubDeploymentsTool)}:{nameof(AppOptions.List)}:{prop}",
                            appOptions.List[prop])));
                }
                
                if (appOptions.Create != null)
                {
                    config.AddInMemoryCollection(typeof(CreateDeploymentOptions)
                        .ListProperties()
                        .Select(prop => new KeyValuePair<string, string?>(
                            $"{nameof(OptionSections.GithubDeploymentsTool)}:{nameof(AppOptions.Create)}:{prop}",
                            appOptions.Create[prop])));
                }
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
                    .AddOptions<AppOptions>()
                    .Bind(hostContext.Configuration.GetSection(nameof(OptionSections.GithubDeploymentsTool)))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddScoped<HttpAcceptHeaderHandler>();
                services.AddScoped<HttpAuthTokenHandler>();
                services.AddScoped<HttpLoggingHandler>();

                services.AddHttpClient(GithubClient.ClientName, (_, httpClient) =>
                    {
                        httpClient.BaseAddress = new Uri("https://api.github.com/graphql");
                        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GithubDeploymentsTool", "1.0"));
                    })
                    .AddPolicyHandler(HttpPolicyProvider.GithubCombinedPolicy)
                    .AddHttpMessageHandler<HttpAcceptHeaderHandler>()
                    .AddHttpMessageHandler<HttpAuthTokenHandler>()
                    .AddHttpMessageHandler<HttpLoggingHandler>()
                    .AddDefaultLogger();

                services.AddGithubClient();

                services.AddSingleton<TimeProvider>(_ => TimeProvider.System);
                services.AddScoped<Worker>();
            })
            .Build();
    }

    private static Worker BuildWorker(AppOptions appOptions)
    {
        var host = BuildHost(appOptions);
        var worker = host.Services.GetRequiredService<Worker>();
        return worker;
    }

    private static async Task<int> OnParsedAsync(AppOptions appOptions)
    {
        var cancelTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, a) =>
        {
            cancelTokenSource.Cancel();
            a.Cancel = true;
        };

        var worker = BuildWorker(appOptions);

        if (appOptions.List != null)
            await worker.ListDeploymentsAsync(cancelTokenSource.Token);
        else if (appOptions.Create != null)
            await worker.CreateDeploymentAsync(cancelTokenSource.Token);

        return Success;
    }

    private static async Task<int> OnListVerbAsync(ListDeploymentsOptions arg)
    {
        return await OnParsedAsync(new AppOptions { List = arg });
    }

    private static async Task<int> OnCreateVerbAsync(CreateDeploymentOptions arg)
    {
        return await OnParsedAsync(new AppOptions { Create = arg });
    }

    private static Task<int> OnNotParsedAsync(IEnumerable<Error> errors)
    {
        return Task.FromResult(ErrorBadArguments);
    }

    public static async Task<int> Main(string[] args)
    {
        // NLog: setup the logger first to catch all errors
        LogManager.Configuration = LoggingConfiguration;
        try
        {
            using var parser = new Parser(settings =>
            {
                settings.CaseSensitive = true;
                settings.IgnoreUnknownArguments = false;
                settings.HelpWriter = Console.Out;
            });
            var exitCode = await parser
                .ParseArguments<ListDeploymentsOptions, CreateDeploymentOptions>(args)
                .MapResult<ListDeploymentsOptions, CreateDeploymentOptions, Task<int>>(
                    OnListVerbAsync, 
                    OnCreateVerbAsync, 
                    OnNotParsedAsync);
            return exitCode;
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
