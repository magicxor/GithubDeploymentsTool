﻿using GithubDeploymentsTool.Models.Options;

namespace GithubDeploymentsTool.Extensions;

public static class AppOptionsExtensions
{
    public static string GetToken(this AppOptions appOptions)
    {
        ArgumentNullException.ThrowIfNull(appOptions);

        return appOptions.List?.Token ?? appOptions.Create?.Token ?? string.Empty;
    }
}
