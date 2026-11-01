using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A page of verse-art image search results.
/// </summary>
public sealed record ImageSearchResult
{
    /// <summary>Gets the matched images in this page.</summary>
    [JsonPropertyName("images")]
    public IReadOnlyList<SearchImage> Images { get; init; } = [];

    /// <summary>Gets the opaque token to supply as <c>page_token</c> on the next request.</summary>
    [JsonPropertyName("next_page_token")]
    public string? NextPageToken { get; init; }
}
