using Microsoft.Extensions.Logging;

using Platform.API.Exceptions;
using Platform.API.Http;
using Platform.API.Models;

using System.Text;

namespace Platform.API.Clients;

/// <summary>
/// Default implementation of <see cref="ISearchClient"/>.
/// </summary>
internal sealed class SearchClient(HttpClient httpClient, ILogger<SearchClient> logger) : ISearchClient
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchQuerySuggestion>> GetSearchQueriesAsync(
        IReadOnlyList<string> languageRanges,
        string? query = null,
        bool? trending = null,
        CancellationToken cancellationToken = default)
    {
        RequireLanguageRanges(languageRanges);

        var sb = new StringBuilder("/v1/search-queries");
        var hasQuery = false;
        AppendArray(sb, ref hasQuery, "language_ranges[]", languageRanges);
        AppendValue(sb, ref hasQuery, "query", query);

        if (trending.HasValue)
            AppendValue(sb, ref hasQuery, "trending", trending.Value ? "true" : "false");

        logger.LogDebug("Fetching search query suggestions (query={Query}).", query);

        var result = await ApiRequestHelper.GetJsonAsync<SearchQuerySuggestionsResponse>(httpClient, sb.ToString(), logger, cancellationToken)
            .ConfigureAwait(false);

        var suggestions = result?.Data ?? [];
        logger.LogDebug("Fetched {Count} search query suggestion(s) from API.", suggestions.Count);
        return suggestions;
    }

    /// <inheritdoc />
    public async Task<UnifiedSearchResult> SearchUnifiedAsync(
        string query,
        int bibleId,
        IReadOnlyList<string> languageRanges,
        string? userIntent = null,
        IReadOnlyList<string>? fields = null,
        CancellationToken cancellationToken = default)
    {
        RequireQuery(query);
        RequireBibleId(bibleId);
        RequireLanguageRanges(languageRanges);

        var sb = new StringBuilder("/v1/search-unified");
        var hasQuery = false;
        AppendValue(sb, ref hasQuery, "query", query);
        AppendValue(sb, ref hasQuery, "bible_id", bibleId.ToString());
        AppendArray(sb, ref hasQuery, "language_ranges[]", languageRanges);
        AppendValue(sb, ref hasQuery, "user_intent", userIntent);
        AppendArray(sb, ref hasQuery, "fields[]", fields);

        logger.LogDebug("Performing unified search for query '{Query}' in Bible version {BibleId}.", query, bibleId);

        var result = await ApiRequestHelper.GetJsonAsync<UnifiedSearchResult>(httpClient, sb.ToString(), logger, cancellationToken)
            .ConfigureAwait(false);

        return result ?? throw new BibleEmptyResponseException(
            $"The unified search for query '{query}' returned an empty response.");
    }

    /// <inheritdoc />
    public async Task<TopicSearchResult> SearchTopicsAsync(
        string query,
        IReadOnlyList<string> languageRanges,
        CancellationToken cancellationToken = default)
    {
        RequireQuery(query);
        RequireLanguageRanges(languageRanges);

        var sb = new StringBuilder("/v1/search-topics");
        var hasQuery = false;
        AppendValue(sb, ref hasQuery, "query", query);
        AppendArray(sb, ref hasQuery, "language_ranges[]", languageRanges);

        logger.LogDebug("Searching topics for query '{Query}'.", query);

        var result = await ApiRequestHelper.GetJsonAsync<TopicSearchResult>(httpClient, sb.ToString(), logger, cancellationToken)
            .ConfigureAwait(false);

        return result ?? throw new BibleEmptyResponseException(
            $"The topic search for query '{query}' returned an empty response.");
    }

    /// <inheritdoc />
    public async Task<VerseSearchResult> SearchVersesAsync(
        string query,
        int bibleId,
        string? userIntent = null,
        string? pageToken = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        RequireQuery(query);
        RequireBibleId(bibleId);

        if (pageSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than zero.");

        var sb = new StringBuilder("/v1/search-verses");
        var hasQuery = false;
        AppendValue(sb, ref hasQuery, "query", query);
        AppendValue(sb, ref hasQuery, "bible_id", bibleId.ToString());
        AppendValue(sb, ref hasQuery, "user_intent", userIntent);
        AppendValue(sb, ref hasQuery, "page_token", pageToken);

        if (pageSize.HasValue)
            AppendValue(sb, ref hasQuery, "page_size", pageSize.Value.ToString());

        logger.LogDebug("Searching verses for query '{Query}' in Bible version {BibleId}.", query, bibleId);

        var result = await ApiRequestHelper.GetJsonAsync<VerseSearchResult>(httpClient, sb.ToString(), logger, cancellationToken)
            .ConfigureAwait(false);

        return result ?? throw new BibleEmptyResponseException(
            $"The verse search for query '{query}' returned an empty response.");
    }

    /// <inheritdoc />
    public async Task<ImageSearchResult> SearchImagesAsync(
        string query,
        int bibleId,
        string? pageToken = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        RequireQuery(query);
        RequireBibleId(bibleId);

        if (pageSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than zero.");

        var sb = new StringBuilder("/v1/search-images");
        var hasQuery = false;
        AppendValue(sb, ref hasQuery, "query", query);
        AppendValue(sb, ref hasQuery, "bible_id", bibleId.ToString());
        AppendValue(sb, ref hasQuery, "page_token", pageToken);

        if (pageSize.HasValue)
            AppendValue(sb, ref hasQuery, "page_size", pageSize.Value.ToString());

        logger.LogDebug("Searching images for query '{Query}' in Bible version {BibleId}.", query, bibleId);

        var result = await ApiRequestHelper.GetJsonAsync<ImageSearchResult>(httpClient, sb.ToString(), logger, cancellationToken)
            .ConfigureAwait(false);

        return result ?? throw new BibleEmptyResponseException(
            $"The image search for query '{query}' returned an empty response.");
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void RequireQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query is required.", nameof(query));
    }

    private static void RequireBibleId(int bibleId)
    {
        if (bibleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bibleId), bibleId, "Bible id must be greater than zero.");
    }

    private static void RequireLanguageRanges(IReadOnlyList<string> languageRanges)
    {
        if (languageRanges is null || languageRanges.Count == 0)
            throw new ArgumentException("At least one language range is required.", nameof(languageRanges));
    }

    private static void AppendValue(StringBuilder sb, ref bool hasQuery, string key, string? value)
    {
        if (value is null)
            return;

        sb.Append(hasQuery ? '&' : '?').Append(key).Append('=').Append(Uri.EscapeDataString(value));
        hasQuery = true;
    }

    private static void AppendArray(StringBuilder sb, ref bool hasQuery, string key, IReadOnlyList<string>? values)
    {
        if (values is null)
            return;

        foreach (var value in values)
        {
            sb.Append(hasQuery ? '&' : '?').Append(key).Append('=').Append(Uri.EscapeDataString(value));
            hasQuery = true;
        }
    }
}
