using Microsoft.Extensions.Logging;

namespace GithubDeploymentsTool.Services;

public class HttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger<HttpLoggingHandler> _logger;

    public HttpLoggingHandler(ILogger<HttpLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestContent = request.Content != null ? await request.Content?.ReadAsStringAsync(cancellationToken)! : string.Empty;
        _logger.LogInformation("{RequestName}:\r\n{Request}\r\n{RequestContent}", nameof(request), request, requestContent);

        var response = await base.SendAsync(request, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken)!;
        _logger.LogInformation("{ResponseName}:\r\n{Response}\r\n{ResponseContent}", nameof(response), response, responseContent);

        return response;
    }
}
