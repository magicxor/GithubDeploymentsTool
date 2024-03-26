﻿using System.Net;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Wrap;

namespace GithubDeploymentsTool.Services;

public static class HttpPolicyProvider
{
    private const int MaxRetryAfterCount = 2;
    private static readonly TimeSpan DefaultRetryAfterTimeout = TimeSpan.FromSeconds(1);
    private static readonly IAsyncPolicy<HttpResponseMessage> RetryAfterPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests && r.Headers.RetryAfter?.Delta != null)
        .WaitAndRetryAsync(
            MaxRetryAfterCount,
            (_, result, _) => result.Result.Headers.RetryAfter is { Delta: not null }
                ? result.Result.Headers.RetryAfter.Delta.Value
                : DefaultRetryAfterTimeout,
            (_, _, _, _) => Task.CompletedTask);

    private static readonly IEnumerable<TimeSpan> GithubDelay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(0.3), retryCount: 3);
    private static readonly IAsyncPolicy<HttpResponseMessage> GithubRetryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(GithubDelay);
    public static readonly AsyncPolicyWrap<HttpResponseMessage> GithubCombinedPolicy = Policy.WrapAsync(RetryAfterPolicy, GithubRetryPolicy);
}
