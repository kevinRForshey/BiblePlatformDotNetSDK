using Microsoft.Extensions.Logging;

using Platform.API.Exceptions;
using Platform.API.Http;
using Platform.API.Models;

using System.Text;

namespace Platform.API.Clients;

/// <summary>
/// Default implementation of <see cref="ILanguageClient"/>.
/// </summary>
internal sealed class LanguageClient(HttpClient httpClient, ILogger<LanguageClient> logger) : ILanguageClient
{
    /// <inheritdoc />
    public async Task<PagedResult<Language>> GetLanguagesAsync(
        string? pageToken = null,
        int? pageSize = null,
        string? country = null,
        bool? biblesAvailable = null,
        string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        if (pageToken is not null && string.IsNullOrWhiteSpace(pageToken))
            throw new ArgumentException("Page token cannot be empty or whitespace.", nameof(pageToken));

        if (pageSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than zero.");

        if (country is not null && string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty or whitespace.", nameof(country));

        var url = BuildLanguagesUrl(pageToken, pageSize, country, biblesAvailable);
        logger.LogDebug("Fetching languages (pageToken={PageToken}, country={Country}).", pageToken, country);

        var result = await ApiRequestHelper.GetJsonAsync<PagedResult<Language>>(
                httpClient, url, logger, cancellationToken, request => ApplyAcceptLanguage(request, acceptLanguage))
            .ConfigureAwait(false);

        var list = result ?? new PagedResult<Language>();
        logger.LogDebug("Fetched {Count} language(s) from API.", list.Data.Count);
        return list;
    }

    /// <inheritdoc />
    public async Task<Language> GetLanguageAsync(
        string languageId,
        string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(languageId))
            throw new ArgumentException("Language id is required.", nameof(languageId));

        logger.LogDebug("Fetching language {LanguageId}.", languageId);

        var result = await ApiRequestHelper.GetJsonAsync<Language>(
                httpClient, $"/v1/languages/{languageId}", logger, cancellationToken, request => ApplyAcceptLanguage(request, acceptLanguage))
            .ConfigureAwait(false);

        var language = result ?? throw new BibleEmptyResponseException(
            $"Language '{languageId}' was not found or returned an empty response.");

        logger.LogDebug("Fetched language {LanguageId}.", languageId);
        return language;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void ApplyAcceptLanguage(HttpRequestMessage request, string? acceptLanguage)
    {
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
            request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
    }

    private static string BuildLanguagesUrl(string? pageToken, int? pageSize, string? country, bool? biblesAvailable)
    {
        var sb = new StringBuilder("/v1/languages");
        var hasQuery = false;

        void AppendParam(string name, string value)
        {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append(name).Append('=').Append(Uri.EscapeDataString(value));
            hasQuery = true;
        }

        if (pageToken is not null)
            AppendParam("page_token", pageToken);

        if (pageSize.HasValue)
            AppendParam("page_size", pageSize.Value.ToString());

        if (country is not null)
            AppendParam("country", country);

        if (biblesAvailable.HasValue)
            AppendParam("bibles_available", biblesAvailable.Value ? "true" : "false");

        return sb.ToString();
    }
}
