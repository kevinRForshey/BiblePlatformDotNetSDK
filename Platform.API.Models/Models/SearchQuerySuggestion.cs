using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A single search query suggestion (autocomplete or trending).
/// </summary>
public sealed record SearchQuerySuggestion
{
    /// <summary>Gets the suggested search text.</summary>
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    /// <summary>Gets the source of the suggestion (e.g. <c>trending</c>).</summary>
    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;
}
