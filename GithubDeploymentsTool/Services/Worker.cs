using System.Text.Json;
using GithubDeploymentsTool.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StrawberryShake;

namespace GithubDeploymentsTool.Services;

public sealed class Worker
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    private readonly IOptions<AppOptions> _githubDeploymentsToolOptions;
    private readonly ILogger<Worker> _logger;
    private readonly IGithubClient _githubClient;

    public Worker(IOptions<AppOptions> githubDeploymentsToolOptions,
        ILogger<Worker> logger,
        IGithubClient githubClient)
    {
        _githubDeploymentsToolOptions = githubDeploymentsToolOptions;
        _logger = logger;
        _githubClient = githubClient;
    }

    public async Task ListDeploymentsAsync(CancellationToken cancellationToken = default)
    {
        var args = _githubDeploymentsToolOptions.Value.List ?? throw new Exception($"{nameof(AppOptions)}.{nameof(AppOptions.List)} not set");

        var response = await _githubClient.ListRepositoryDeployments
            .ExecuteAsync(args.Owner, args.Repository, true, [args.Environment], cancellationToken);

        if (response.IsErrorResult())
        {
            _logger.LogError("Error creating deployment. Errors: {Errors}",
                JsonSerializer.Serialize(response.Errors, JsonSerializerOptions));
        }

        _logger.LogInformation("Repository deployments found. RepositoryId: {RepositoryId}, DeploymentsCount: {DeploymentsCount}", response.Data?.Repository?.Id, response.Data?.Repository?.Deployments.Edges?.Count);
        _logger.LogInformation("Deployments list: {Deployments}", JsonSerializer.Serialize(response.Data?.Repository?.Deployments.Edges, JsonSerializerOptions));
    }

    // https://docs.github.com/en/graphql/reference/mutations#createdeployment
    public async Task CreateDeploymentAsync(CancellationToken cancellationToken = default)
    {
        var args = _githubDeploymentsToolOptions.Value.Create ?? throw new Exception($"{nameof(AppOptions)}.{nameof(AppOptions.Create)} not set");

        _logger.LogInformation("Requesting repository commit information. Owner: {Owner}, Repository: {Repository}", args.Owner, args.Repository);

        var repoCommitResponse = await _githubClient.GetRepositoryCommit
            .ExecuteAsync(args.Owner, args.Repository, true, args.Ref, cancellationToken);

        if (repoCommitResponse.IsErrorResult())
        {
            _logger.LogError("Error getting repository commit information. Errors: {Errors}",
                JsonSerializer.Serialize(repoCommitResponse.Errors, JsonSerializerOptions));
            return;
        }

        var repositoryId = repoCommitResponse.Data?.Repository?.Id;
        var repositoryRefId = repoCommitResponse.Data?.Repository?.Ref?.Id;

        if (repositoryId == null)
            throw new Exception("Repository not found");
        if (repositoryRefId == null)
            throw new Exception("Repository ref not found");

        var deploymentResponse = await _githubClient.CreateDeployment.ExecuteAsync(new CreateDeploymentInput
        {
            Environment = args.Environment,
            Description = args.Description,
            Payload = args.Payload,
            Task = args.Task,
            RefId = repositoryRefId,
            RepositoryId = repositoryId,
        }, cancellationToken);

        if (deploymentResponse.IsErrorResult())
        {
            _logger.LogError("Error creating deployment. Errors: {Errors}", JsonSerializer.Serialize(deploymentResponse.Errors, JsonSerializerOptions));
            return;
        }
        else
        {
            _logger.LogInformation("Deployment created successfully. Id: {DeploymentId}, Environment: {Environment}, Description: {Description}, Task: {Task}",
                deploymentResponse.Data?.CreateDeployment?.Deployment?.Id,
                deploymentResponse.Data?.CreateDeployment?.Deployment?.Environment,
                deploymentResponse.Data?.CreateDeployment?.Deployment?.Description,
                deploymentResponse.Data?.CreateDeployment?.Deployment?.Task);
        }

        var deploymentId = deploymentResponse.Data?.CreateDeployment?.Deployment?.Id;
        var clientMutationId = deploymentResponse.Data?.CreateDeployment?.ClientMutationId;

        if (deploymentId == null)
            throw new Exception("Deployment not found");

        var deploymentStatusResponse = await _githubClient.CreateDeploymentStatus
            .ExecuteAsync(new CreateDeploymentStatusInput
            {
                Environment = args.Environment,
                Description = args.Description,
                State = DeploymentStatusState.Success,
                DeploymentId = deploymentId,
            }, cancellationToken);

        if (deploymentStatusResponse.IsErrorResult())
            _logger.LogError("Error creating deployment status. Errors: {Errors}", JsonSerializer.Serialize(deploymentStatusResponse.Errors, JsonSerializerOptions));
        else
            _logger.LogInformation("Deployment status created successfully. Id: {DeploymentStatusId}, State: {State}",
                deploymentStatusResponse.Data?.CreateDeploymentStatus?.DeploymentStatus?.Id,
                deploymentStatusResponse.Data?.CreateDeploymentStatus?.DeploymentStatus?.State);
    }
}
