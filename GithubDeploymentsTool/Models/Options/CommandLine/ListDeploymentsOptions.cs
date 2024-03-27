using System.ComponentModel.DataAnnotations;
using CommandLine;

namespace GithubDeploymentsTool.Models.Options.CommandLine;

// see https://docs.github.com/en/rest/deployments/deployments?apiVersion=2022-11-28#list-deployments
[Verb("list", HelpText = "List deployments")]
public sealed class ListDeploymentsOptions
{
    public string? this[string propertyName]
    {
        get
        {
            var thisType = typeof(ListDeploymentsOptions);
            var propInfo = thisType.GetProperty(propertyName);
            return propInfo?.GetValue(this, null)?.ToString();
        }
    }

    [Required]
    [Option('o', "owner", Required = true, HelpText = "The account owner of the repository. The name is not case sensitive.")]
    public required string Owner { get; set; }

    [Required]
    [Option('r', "repository", Required = true, HelpText = "The name of the repository without the .git extension. The name is not case sensitive.")]
    public required string Repository { get; set; }

    [Required]
    [Option('t', "token", Required = true,  HelpText = "Github token")]
    public required string Token { get; set; }

    [Required]
    [Option('e', "environment", Required = true, HelpText = "The name of the environment that was deployed to (e.g., staging or production).")]
    public required string Environment { get; set; }

    [Required]
    [Option('f', "ref", Required = true, HelpText = "The name of the ref. This can be a branch, tag, or SHA.")]
    public required string Ref { get; set; }

    [Required]
    [Option('k', "task", Required = true, HelpText = "The name of the task for the deployment (e.g., deploy or deploy:migrations).")]
    public required string Task { get; set; }
}
