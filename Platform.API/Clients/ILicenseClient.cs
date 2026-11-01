using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Reads content licenses from the Bible Platform API.
/// </summary>
public interface ILicenseClient
{
    /// <summary>
    /// Lists licenses covering a Bible version.
    /// </summary>
    /// <param name="bibleId">The Bible version id to find licenses for.</param>
    /// <param name="developerId">
    /// An optional developer id to filter to licenses that developer has agreed to. The caller
    /// must belong to that developer to use this filter.
    /// </param>
    /// <param name="allAvailable">
    /// When <see langword="true"/> (combine with <paramref name="developerId"/>), returns all
    /// published licenses, including ones not yet agreed to.
    /// </param>
    /// <param name="contentType">An optional content type (e.g. <c>bible</c>) to limit results to.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The matching licenses.</returns>
    Task<IReadOnlyList<License>> GetLicensesAsync(
        int bibleId,
        string? developerId = null,
        bool? allAvailable = null,
        string? contentType = null,
        CancellationToken cancellationToken = default);
}
