using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// The result of a topic search.
/// </summary>
public sealed record TopicSearchResult
{
    /// <summary>Gets the matched topics.</summary>
    [JsonPropertyName("topics")]
    public IReadOnlyList<SearchTopic> Topics { get; init; } = [];

    /// <summary>Gets spelling-correction suggestions for the query, if any.</summary>
    [JsonPropertyName("did_you_mean")]
    public IReadOnlyList<string> DidYouMean { get; init; } = [];

    /// <summary>Gets a suggested alternate query, if the original returned no results.</summary>
    [JsonPropertyName("search_instead_for")]
    public string? SearchInsteadFor { get; init; }

    /// <summary>Gets the total number of matched topics.</summary>
    [JsonPropertyName("total_size")]
    public int TotalSize { get; init; }
}
