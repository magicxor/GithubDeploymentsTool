using System.Reflection;

namespace GithubDeploymentsTool.Extensions;

public static class TypeExtensions
{
    public static IReadOnlyCollection<string> ListProperties(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToList()
            .AsReadOnly();
    }
}
