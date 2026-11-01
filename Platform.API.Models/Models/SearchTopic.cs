using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A topic matched by a search.
/// </summary>
public sealed record SearchTopic
{
    /// <summary>Gets the topic's id.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Gets the topic's display text.</summary>
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    /// <summary>Gets related subtopics.</summary>
    [JsonPropertyName("subtopics")]
    public IReadOnlyList<string> Subtopics { get; init; } = [];
}
