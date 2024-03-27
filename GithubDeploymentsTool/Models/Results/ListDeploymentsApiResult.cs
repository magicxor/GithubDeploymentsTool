using StrawberryShake;

namespace GithubDeploymentsTool.Models.Results;

public record ListDeploymentsApiResult(
    bool IsSuccess,
    IReadOnlyCollection<IListRepositoryDeployments_Repository_Deployments_Edges_Node> Nodes,
    IReadOnlyCollection<IClientError> Errors
);
