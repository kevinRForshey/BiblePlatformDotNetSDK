using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// Verse-art image metadata matched by a search. Resolve <see cref="Id"/> via the image
/// client to get the full image, including its CDN URL.
/// </summary>
public sealed record SearchImage
{
    /// <summary>Gets the image's id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the passage reference the image illustrates.</summary>
    [JsonPropertyName("passage_id")]
    public string PassageId { get; init; } = string.Empty;
}
