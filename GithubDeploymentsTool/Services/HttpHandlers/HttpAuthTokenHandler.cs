using System.Net.Http.Headers;
using GithubDeploymentsTool.Extensions;
using GithubDeploymentsTool.Models.Options;
using Microsoft.Extensions.Options;

namespace GithubDeploymentsTool.Services.HttpHandlers;

public class HttpAuthTokenHandler : DelegatingHandler
{
    private readonly AppOptions _appOptions;

    public HttpAuthTokenHandler(IOptions<AppOptions> appOptions)
    {
        ArgumentNullException.ThrowIfNull(appOptions);

        _appOptions = appOptions.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = _appOptions.GetToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
