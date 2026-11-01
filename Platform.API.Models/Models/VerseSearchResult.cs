using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A page of verse-reference search results.
/// </summary>
public sealed record VerseSearchResult
{
    /// <summary>Gets the matched verse references in this page.</summary>
    [JsonPropertyName("verses")]
    public IReadOnlyList<SearchVerse> Verses { get; init; } = [];

    /// <summary>Gets the detected or supplied search intent, when known.</summary>
    [JsonPropertyName("user_intent")]
    public string? UserIntent { get; init; }

    /// <summary>Gets spelling-correction suggestions for the query, if any.</summary>
    [JsonPropertyName("did_you_mean")]
    public IReadOnlyList<string> DidYouMean { get; init; } = [];

    /// <summary>Gets a suggested alternate query, if the original returned no results.</summary>
    [JsonPropertyName("search_instead_for")]
    public string? SearchInsteadFor { get; init; }

    /// <summary>Gets the opaque token to supply as <c>page_token</c> on the next request.</summary>
    [JsonPropertyName("next_page_token")]
    public string? NextPageToken { get; init; }
}
