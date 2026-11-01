using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

using Platform.API.Exceptions;

namespace Platform.API.Http;

/// <summary>
/// Shared HTTP response helpers used by all typed API clients.
/// Centralises error logging and JSON deserialization so individual clients
/// stay focused on their own request-building logic.
/// </summary>
internal static class ApiRequestHelper
{
    /// <summary>
    /// Throws <see cref="BibleApiException"/> when the response has a non-success
    /// status code, logging the failure details before throwing.
    /// </summary>
    internal static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string url,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogError(
                "API request to '{Url}' failed with HTTP {StatusCode} {ReasonPhrase}. Response body: {Body}",
                url, (int)response.StatusCode, response.ReasonPhrase, body);
            throw new BibleApiException(
                response.StatusCode,
                $"API request to '{url}' failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).",
                body);
        }
    }

    /// <summary>
    /// Issues a GET to <paramref name="relativeUrl"/>, checks for success, and
    /// deserializes the JSON body as <typeparamref name="T"/>.
    /// </summary>
    internal static async Task<T?> GetJsonAsync<T>(
        HttpClient httpClient,
        string relativeUrl,
        ILogger logger,
        CancellationToken cancellationToken)
        => await GetJsonAsync<T>(httpClient, relativeUrl, logger, cancellationToken, configureRequest: null)
            .ConfigureAwait(false);

    /// <summary>
    /// Issues a GET to <paramref name="relativeUrl"/>, checks for success, and
    /// deserializes the JSON body as <typeparamref name="T"/>. Use
    /// <paramref name="configureRequest"/> to set per-request headers (e.g.
    /// <c>Accept-Language</c>) that can't be set once on the shared, pooled <see cref="HttpClient"/>.
    /// </summary>
    internal static async Task<T?> GetJsonAsync<T>(
        HttpClient httpClient,
        string relativeUrl,
        ILogger logger,
        CancellationToken cancellationToken,
        Action<HttpRequestMessage>? configureRequest)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        configureRequest?.Invoke(request);

        using var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, relativeUrl, logger, cancellationToken).ConfigureAwait(false);

        return await response.Content
            .ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
