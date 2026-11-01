using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Searches the platform's verses, topics, and verse-art images.
/// </summary>
public interface ISearchClient
{
    /// <summary>
    /// Gets autocomplete or trending search query suggestions.
    /// </summary>
    /// <param name="languageRanges">An ordered, non-empty list of BCP 47 language ranges (e.g. <c>en</c>, <c>*</c>).</param>
    /// <param name="query">Partial query text to get autocomplete suggestions for.</param>
    /// <param name="trending">When <see langword="true"/>, returns trending searches instead of autocomplete suggestions.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The matching suggestions.</returns>
    Task<IReadOnlyList<SearchQuerySuggestion>> GetSearchQueriesAsync(
        IReadOnlyList<string> languageRanges,
        string? query = null,
        bool? trending = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches verses, topics, and images in a single combined, unpaginated result.
    /// </summary>
    /// <param name="query">The search text (1-100 characters).</param>
    /// <param name="bibleId">The Bible version id to search within.</param>
    /// <param name="languageRanges">An ordered, non-empty list of BCP 47 language ranges (e.g. <c>en</c>, <c>*</c>).</param>
    /// <param name="userIntent">An optional hint for how to interpret the query (<c>unknown</c>, <c>topical</c>, <c>text</c>, or <c>reference</c>).</param>
    /// <param name="fields">An optional subset of result types to include (<c>verses</c>, <c>topics</c>, <c>images</c>).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The combined search result.</returns>
    Task<UnifiedSearchResult> SearchUnifiedAsync(
        string query,
        int bibleId,
        IReadOnlyList<string> languageRanges,
        string? userIntent = null,
        IReadOnlyList<string>? fields = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches topics related to a query.
    /// </summary>
    /// <param name="query">The search text (1-100 characters).</param>
    /// <param name="languageRanges">An ordered, non-empty list of BCP 47 language ranges (e.g. <c>en</c>, <c>*</c>).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The matched topics.</returns>
    Task<TopicSearchResult> SearchTopicsAsync(
        string query,
        IReadOnlyList<string> languageRanges,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches verse references matching a query.
    /// </summary>
    /// <param name="query">The search text (1-100 characters).</param>
    /// <param name="bibleId">The Bible version id to search within.</param>
    /// <param name="userIntent">An optional hint for how to interpret the query (<c>unknown</c>, <c>topical</c>, <c>text</c>, or <c>reference</c>).</param>
    /// <param name="pageToken">An opaque token from a previous response's <c>next_page_token</c>.</param>
    /// <param name="pageSize">The maximum number of results to return (1-99).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A page of matched verse references.</returns>
    Task<VerseSearchResult> SearchVersesAsync(
        string query,
        int bibleId,
        string? userIntent = null,
        string? pageToken = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches verse-art images matching a query.
    /// </summary>
    /// <param name="query">The search text (1-100 characters).</param>
    /// <param name="bibleId">The Bible version id to search within.</param>
    /// <param name="pageToken">An opaque token from a previous response's <c>next_page_token</c>.</param>
    /// <param name="pageSize">The maximum number of results to return (1-99).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A page of matched images.</returns>
    Task<ImageSearchResult> SearchImagesAsync(
        string query,
        int bibleId,
        string? pageToken = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);
}
