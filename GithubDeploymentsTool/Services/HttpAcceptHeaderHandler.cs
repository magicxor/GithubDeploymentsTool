﻿using System.Net.Http.Headers;

namespace GithubDeploymentsTool.Services;

public class HttpAcceptHeaderHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // use preview schema
        // https://docs.github.com/en/graphql/overview/schema-previews#deployments-preview

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.flash-preview+json"));

        return await base.SendAsync(request, cancellationToken);
    }
}
