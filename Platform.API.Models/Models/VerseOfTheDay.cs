using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A single day's entry in the platform's verse-of-the-day calendar.
/// </summary>
public sealed record VerseOfTheDay
{
    /// <summary>Gets the day of year this entry applies to (1-366).</summary>
    [JsonPropertyName("day")]
    public int Day { get; init; }

    /// <summary>Gets the USFM passage id for this day (e.g. <c>JHN.3.16</c>).</summary>
    [JsonPropertyName("passage_id")]
    public string PassageId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the id of the matching verse art for this day, resolvable via <c>GET /v1/images/{image_id}</c>.
    /// </summary>
    /// <value>
    /// The image id, or <see langword="null"/> when no art matched the requested
    /// <c>language_ranges[]</c> (or none were supplied).
    /// </value>
    [JsonPropertyName("image_id")]
    public string? ImageId { get; init; }
}
