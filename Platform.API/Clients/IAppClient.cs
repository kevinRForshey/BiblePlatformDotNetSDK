using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Provides metadata lookup for apps registered with the platform.
/// </summary>
public interface IAppClient
{
    /// <summary>
    /// Returns public metadata for a single app.
    /// </summary>
    /// <param name="appId">The app's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The <see cref="App"/> metadata.</returns>
    Task<App> GetAppAsync(string appId, CancellationToken cancellationToken = default);
}
