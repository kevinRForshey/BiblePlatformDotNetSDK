using Microsoft.Extensions.Logging;

using Platform.API.Exceptions;
using Platform.API.Http;
using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Default implementation of <see cref="IPermissionClient"/>.
/// </summary>
internal sealed class PermissionClient(HttpClient httpClient, ILogger<PermissionClient> logger) : IPermissionClient
{
    /// <inheritdoc />
    public async Task<AppPermissions> GetPermissionsAsync(string appId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appId))
            throw new ArgumentException("App id is required.", nameof(appId));

        logger.LogDebug("Fetching permissions granted to app {AppId}.", appId);

        var result = await ApiRequestHelper.GetJsonAsync<AppPermissions>(
                httpClient, $"/v1/apps/{appId}/permissions", logger, cancellationToken)
            .ConfigureAwait(false);

        var permissions = result ?? throw new BibleEmptyResponseException(
            $"Permissions for app '{appId}' were not found or returned an empty response.");

        logger.LogDebug("Fetched {Count} permission(s) for app {AppId}.", permissions.Permissions.Count, appId);
        return permissions;
    }
}
