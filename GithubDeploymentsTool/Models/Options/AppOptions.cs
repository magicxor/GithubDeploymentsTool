using GithubDeploymentsTool.Models.Options.CommandLine;

namespace GithubDeploymentsTool.Models.Options;

public sealed class AppOptions
{
    public ListDeploymentsOptions? List { get; set; }
    public CreateDeploymentOptions? Create { get; set; }
}
