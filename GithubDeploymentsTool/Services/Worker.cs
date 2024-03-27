using System.Collections.ObjectModel;
using System.Text.Json;
using GithubDeploymentsTool.Models.Options;
using GithubDeploymentsTool.Models.Options.CommandLine;
using GithubDeploymentsTool.Models.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StrawberryShake;

namespace GithubDeploymentsTool.Services;

public sealed class Worker
{
    private const int Success = 0;
    private const int Error = 1;

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

    private async Task<ListDeploymentsApiResult> ListDeploymentsInnerAsync(ListDeploymentsOptions options, CancellationToken cancellationToken = default)
    {
        var response = await _githubClient.ListRepositoryDeployments
            .ExecuteAsync(options.Owner, options.Repository, true, [options.Environment], cancellationToken);

        if (response.IsErrorResult())
        {
            return new ListDeploymentsApiResult(
                response.IsSuccessResult(),
                ReadOnlyCollection<IListRepositoryDeployments_Repository_Deployments_Edges_Node>.Empty,
                response.Errors
            );
        }

        var nodes = response.Data?.Repository?.Deployments.Edges?
            .Select(edge => edge?.Node)
            .Where(node => node != null
                           && node.Environment == options.Environment
                           && $"{node.Ref?.Prefix}{node.Ref?.Name}" == options.Ref
                           && node.Task == options.Task
                           && node.LatestStatus?.State == DeploymentStatusState.Success)
            .Select(node => node!)
            .ToList()
            .AsReadOnly() ?? ReadOnlyCollection<IListRepositoryDeployments_Repository_Deployments_Edges_Node>.Empty;

        return new ListDeploymentsApiResult(
            response.IsSuccessResult(),
            nodes,
            ReadOnlyCollection<IClientError>.Empty
        );
    }

    public async Task<int> ListDeploymentsAsync(CancellationToken cancellationToken = default)
    {
        var options = _githubDeploymentsToolOptions.Value.List ?? throw new Exception($"{nameof(AppOptions)}.{nameof(AppOptions.List)} not set");

        var result = await ListDeploymentsInnerAsync(options, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Deployments: {Deployments}",
                JsonSerializer.Serialize(result.Nodes, JsonSerializerOptions));
            return Success;
        }
        else
        {
            _logger.LogError("Error listing deployments. Errors: {Errors}",
                JsonSerializer.Serialize(result.Errors, JsonSerializerOptions));
            return Error;
        }
    }

    private async Task<CreateDeploymentApiResult> CreateDeploymentInnerAsync(CreateDeploymentOptions options, CancellationToken cancellationToken = default)
    {
        // see https://docs.github.com/en/graphql/reference/mutations#createdeployment

        var repoCommitResponse = await _githubClient.GetRepositoryCommit
            .ExecuteAsync(options.Owner, options.Repository, true, options.Ref, cancellationToken);

        if (repoCommitResponse.IsErrorResult())
        {
            return new CreateDeploymentApiResult(
                repoCommitResponse.IsSuccessResult(),
                string.Empty,
                repoCommitResponse.Errors
            );
        }

        var repositoryId = repoCommitResponse.Data?.Repository?.Id;
        var repositoryRefId = repoCommitResponse.Data?.Repository?.Ref?.Id;

        if (repositoryId == null)
            throw new Exception("Repository.Id is null");
        if (repositoryRefId == null)
            throw new Exception("Repository.Ref.Id is null");

        var deploymentResponse = await _githubClient.CreateDeployment.ExecuteAsync(new CreateDeploymentInput
        {
            Environment = options.Environment,
            Description = options.Description,
            Payload = options.Payload,
            Task = options.Task,
            RefId = repositoryRefId,
            RepositoryId = repositoryId,
        }, cancellationToken);

        if (deploymentResponse.IsErrorResult())
        {
            return new CreateDeploymentApiResult(
                deploymentResponse.IsSuccessResult(),
                string.Empty,
                deploymentResponse.Errors
            );
        }

        var deploymentId = deploymentResponse.Data?.CreateDeployment?.Deployment?.Id;

        if (deploymentId == null)
            throw new Exception("Deployment.Id is null");

        var deploymentStatusResponse = await _githubClient.CreateDeploymentStatus
            .ExecuteAsync(new CreateDeploymentStatusInput
            {
                Environment = options.Environment,
                Description = options.Description,
                State = DeploymentStatusState.Success,
                DeploymentId = deploymentId,
            }, cancellationToken);

        if (deploymentStatusResponse.IsErrorResult())
        {
            return new CreateDeploymentApiResult(
                deploymentStatusResponse.IsSuccessResult(),
                string.Empty,
                deploymentStatusResponse.Errors
            );
        }

        return new CreateDeploymentApiResult(
            deploymentStatusResponse.IsSuccessResult(),
            deploymentId,
            ReadOnlyCollection<IClientError>.Empty
        );
    }

    public async Task<int> CreateDeploymentAsync(CancellationToken cancellationToken = default)
    {
        var options = _githubDeploymentsToolOptions.Value.Create ?? throw new Exception($"{nameof(AppOptions)}.{nameof(AppOptions.Create)} not set");

        var result = await CreateDeploymentInnerAsync(options, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Deployment created successfully. Id: {DeploymentId}", result.DeploymentId);
            return Success;
        }
        else
        {
            _logger.LogError("Error creating deployment. Errors: {Errors}",
                JsonSerializer.Serialize(result.Errors, JsonSerializerOptions));
            return Error;
        }
    }
}
