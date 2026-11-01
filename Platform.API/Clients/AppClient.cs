using Microsoft.Extensions.Logging;

using Platform.API.Exceptions;
using Platform.API.Http;
using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Default implementation of <see cref="IAppClient"/>.
/// </summary>
internal sealed class AppClient(HttpClient httpClient, ILogger<AppClient> logger) : IAppClient
{
    /// <inheritdoc />
    public async Task<App> GetAppAsync(string appId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appId))
            throw new ArgumentException("App id is required.", nameof(appId));

        logger.LogDebug("Fetching app {AppId}.", appId);

        var result = await ApiRequestHelper.GetJsonAsync<App>(httpClient, $"/v1/apps/{appId}", logger, cancellationToken)
            .ConfigureAwait(false);

        var app = result ?? throw new BibleEmptyResponseException(
            $"App {appId} was not found or returned an empty response.");

        logger.LogDebug("Fetched app {AppId} ({Name}).", appId, app.Name);
        return app;
    }
}
