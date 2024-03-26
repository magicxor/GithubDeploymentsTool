using GithubDeploymentsTool.Models;
using Microsoft.Extensions.Options;

namespace GithubDeploymentsTool.Services;

public sealed class Worker
{
    private readonly IOptions<AppOptions> _githubDeploymentsToolOptions;
    private readonly IGithubClient _githubClient;

    public Worker(IOptions<AppOptions> githubDeploymentsToolOptions,
        IGithubClient githubClient)
    {
        _githubDeploymentsToolOptions = githubDeploymentsToolOptions;
        _githubClient = githubClient;
    }

    public async Task ListDeploymentsAsync(CancellationToken cancellationToken = default)
    {
        var args = _githubDeploymentsToolOptions.Value.List ?? throw new Exception("List options not set");
        var response = await _githubClient.Repository
            .ExecuteAsync(args.Owner, args.Repository, true, [args.Environment], cancellationToken);
        Console.WriteLine("Deployments found: " + response.Data?.Repository?.Deployments.Edges?.Count);
    }

    public async Task CreateDeploymentAsync(CancellationToken cancellationToken = default)
    {
        var args = _githubDeploymentsToolOptions.Value.Create ?? throw new Exception("Create options not set");
        throw new NotImplementedException();
    }
}
