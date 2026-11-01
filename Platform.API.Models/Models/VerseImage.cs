using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A shareable image rendering a Bible verse or verse range.
/// </summary>
public sealed record VerseImage
{
    /// <summary>Gets the image's opaque id. Not guaranteed stable across responses.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDN URL for the full-size image. Append <c>?size=thumb</c>, <c>card</c>, or
    /// <c>full</c> to request a resized rendition.
    /// </summary>
    [JsonPropertyName("image_url")]
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>Gets the USFM passage id this image renders (e.g. <c>JHN.3.16</c>).</summary>
    [JsonPropertyName("passage_id")]
    public string PassageId { get; init; } = string.Empty;

    /// <summary>Gets the attribution text to display alongside this image (e.g. <c>© YouVersion</c>).</summary>
    [JsonPropertyName("attribution")]
    public string Attribution { get; init; } = string.Empty;

    /// <summary>Gets the BCP-47 language tag of the passage text rendered in this image.</summary>
    [JsonPropertyName("language_tag")]
    public string LanguageTag { get; init; } = string.Empty;

    /// <summary>Gets the image's native width in pixels.</summary>
    [JsonPropertyName("width")]
    public int Width { get; init; }

    /// <summary>Gets the image's native height in pixels.</summary>
    [JsonPropertyName("height")]
    public int Height { get; init; }

    /// <summary>Gets the image's aspect ratio (width/height), rounded to three decimal places.</summary>
    [JsonPropertyName("aspect_ratio")]
    public double AspectRatio { get; init; }
}
