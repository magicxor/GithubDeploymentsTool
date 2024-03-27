using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using CommandLine;
using GithubDeploymentsTool.Enums;
using GithubDeploymentsTool.Extensions;
using GithubDeploymentsTool.Models.Options;
using GithubDeploymentsTool.Models.Options.CommandLine;
using GithubDeploymentsTool.Services;
using GithubDeploymentsTool.Services.HttpHandlers;
using GithubDeploymentsTool.Utils;
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
    // see: https://docs.microsoft.com/en-us/windows/desktop/Debug/system-error-codes
    private const int ErrorBadArguments = 160;

    private static readonly LoggingConfiguration LoggingConfiguration = new XmlLoggingConfiguration("nlog.config");

    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "This is a constant value")]
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

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Host is disposed by the caller")]
    [SuppressMessage("Blocker Bug", "S2930:\"IDisposables\" should be disposed", Justification = "Host is disposed by the caller")]
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
            return await worker.ListDeploymentsAsync(cancelTokenSource.Token);
        else if (appOptions.Create != null)
            return await worker.CreateDeploymentAsync(cancelTokenSource.Token);
        else
            throw new ArgumentException("No verb selected", nameof(appOptions));
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

    [SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both", Justification = "This is an entry point")]
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
