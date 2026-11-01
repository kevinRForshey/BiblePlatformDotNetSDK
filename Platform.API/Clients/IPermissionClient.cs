using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Reads the permissions a signed-in user has granted to an app.
/// </summary>
/// <remarks>
/// Requires an authenticated caller: register OAuth support (<c>AddBibleOAuth</c>) after
/// registering the API clients (<c>AddBibleApiClients</c>) so requests carry a bearer token.
/// </remarks>
public interface IPermissionClient
{
    /// <summary>
    /// Gets the permissions the signed-in user has granted to the given app.
    /// </summary>
    /// <param name="appId">The calling app's id.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The granted permissions.</returns>
    Task<AppPermissions> GetPermissionsAsync(string appId, CancellationToken cancellationToken = default);
}
