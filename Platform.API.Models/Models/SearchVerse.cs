using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A verse reference matched by a search. Carries no passage text or version data by design.
/// </summary>
public sealed record SearchVerse
{
    /// <summary>Gets the matched passage reference in USFM form (e.g. <c>JHN.3.16</c>).</summary>
    [JsonPropertyName("reference")]
    public string Reference { get; init; } = string.Empty;
}
