using System.ComponentModel.DataAnnotations;
using CommandLine;

namespace GithubDeploymentsTool.Models.CommandLine;

// see https://docs.github.com/en/rest/deployments/deployments?apiVersion=2022-11-28#create-a-deployment
[Verb("create", HelpText = "Create a deployment")]
public sealed class CreateDeploymentOptions
{
    public string? this[string propertyName]
    {
        get
        {
            var thisType = typeof(CreateDeploymentOptions);
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

    [Required]
    [Option('p', "payload", Required = true, HelpText = "JSON payload with extra information about the deployment.")]
    public required string Payload { get; set; }

    [Required]
    [Option('d', "description", Required = true, HelpText = "Short description of the deployment.")]
    public required string Description { get; set; }

    [Required]
    [Option('n', "production_environment", Required = true, HelpText = "Specifies if the given environment is one that end-users directly interact with. Default: true when environment is production and false otherwise.")]
    public required bool ProductionEnvironment { get; set; }
}
