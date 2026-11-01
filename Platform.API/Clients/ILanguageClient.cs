using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Reads language metadata from the Bible Platform API.
/// </summary>
public interface ILanguageClient
{
    /// <summary>
    /// Lists languages known to the platform.
    /// </summary>
    /// <param name="pageToken">An opaque token from a previous response's <c>next_page_token</c>, or <see langword="null"/> for the first page.</param>
    /// <param name="pageSize">The maximum number of languages to return per page, or <see langword="null"/> to use the API default.</param>
    /// <param name="country">An optional ISO 3166-1 country code to filter languages spoken in that country.</param>
    /// <param name="biblesAvailable">When <see langword="true"/>, restricts results to languages with at least one available Bible.</param>
    /// <param name="acceptLanguage">
    /// An optional BCP 47 language tag sent as the <c>Accept-Language</c> request header, used by the
    /// API to choose each language's <see cref="Language.LocalizedName"/>.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A page of languages.</returns>
    Task<PagedResult<Language>> GetLanguagesAsync(
        string? pageToken = null,
        int? pageSize = null,
        string? country = null,
        bool? biblesAvailable = null,
        string? acceptLanguage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single language by its canonical BCP 47 language id.
    /// </summary>
    /// <param name="languageId">The canonical language id (e.g. <c>en</c>, <c>sr-Latn</c>).</param>
    /// <param name="acceptLanguage">
    /// An optional BCP 47 language tag sent as the <c>Accept-Language</c> request header, used by the
    /// API to choose the <see cref="Language.LocalizedName"/>.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The requested language.</returns>
    Task<Language> GetLanguageAsync(
        string languageId,
        string? acceptLanguage = null,
        CancellationToken cancellationToken = default);
}
