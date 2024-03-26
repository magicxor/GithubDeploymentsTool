using System.Net.Http.Headers;
using GithubDeploymentsTool.Models;
using Microsoft.Extensions.Options;

namespace GithubDeploymentsTool.Services;

public class HttpAuthTokenHandler : DelegatingHandler
{
    private readonly AppOptions _appOptions;

    public HttpAuthTokenHandler(IOptions<AppOptions> appOptions)
    {
        _appOptions = appOptions.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _appOptions.List?.Token ?? _appOptions.Create?.Token ?? string.Empty;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
