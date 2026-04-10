using System.Net;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;

namespace PhotoDownloader.Infrastructure;

/// <summary>
/// Политики устойчивости для <see cref="HttpClient"/>.
/// </summary>
public static class HttpRetryPolicies
{
    /// <summary>
    /// Повтор при кратковременных сбоях HTTP (5xx, 408, сетевые ошибки).
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> TransientShortRetry { get; } = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            3,
            attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)));
}
