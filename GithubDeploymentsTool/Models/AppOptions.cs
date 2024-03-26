using GithubDeploymentsTool.Models.CommandLine;

namespace GithubDeploymentsTool.Models;

public sealed class AppOptions
{
    public ListDeploymentsOptions? List { get; set; }
    public CreateDeploymentOptions? Create { get; set; }
}
