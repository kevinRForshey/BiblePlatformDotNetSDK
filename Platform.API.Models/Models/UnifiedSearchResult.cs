using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A single combined search result across verses, topics, and images.
/// </summary>
public sealed record UnifiedSearchResult
{
    /// <summary>Gets the matched verse references.</summary>
    [JsonPropertyName("verses")]
    public IReadOnlyList<SearchVerse> Verses { get; init; } = [];

    /// <summary>Gets the matched topics.</summary>
    [JsonPropertyName("topics")]
    public IReadOnlyList<SearchTopic> Topics { get; init; } = [];

    /// <summary>Gets the matched verse-art images.</summary>
    [JsonPropertyName("images")]
    public IReadOnlyList<SearchImage> Images { get; init; } = [];

    /// <summary>Gets the detected or supplied search intent, when known.</summary>
    [JsonPropertyName("user_intent")]
    public string? UserIntent { get; init; }

    /// <summary>Gets spelling-correction suggestions for the query, if any.</summary>
    [JsonPropertyName("did_you_mean")]
    public IReadOnlyList<string> DidYouMean { get; init; } = [];

    /// <summary>Gets a suggested alternate query, if the original returned no results.</summary>
    [JsonPropertyName("search_instead_for")]
    public string? SearchInsteadFor { get; init; }
}
