using StrawberryShake;

namespace GithubDeploymentsTool.Models.Results;

public record CreateDeploymentApiResult(
    bool IsSuccess,
    string DeploymentId,
    IReadOnlyCollection<IClientError> Errors
);
