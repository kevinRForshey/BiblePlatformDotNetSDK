using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A font family available for rendering Bible text.
/// </summary>
public sealed record Font
{
    /// <summary>Gets the font's numeric id.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Gets the font's URL-safe slug.</summary>
    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    /// <summary>Gets the font family's display name.</summary>
    [JsonPropertyName("family")]
    public string Family { get; init; } = string.Empty;

    /// <summary>Gets the weight/style variants available for this font family.</summary>
    [JsonPropertyName("variants")]
    public IReadOnlyList<FontVariant> Variants { get; init; } = [];
}

/// <summary>
/// A single weight/style variant of a <see cref="Font"/> family.
/// </summary>
public sealed record FontVariant
{
    /// <summary>Gets the CSS font weight (e.g. <c>400</c>, <c>700</c>).</summary>
    [JsonPropertyName("weight")]
    public int Weight { get; init; }

    /// <summary>Gets the CSS font style (e.g. <c>normal</c>, <c>italic</c>).</summary>
    [JsonPropertyName("style")]
    public string Style { get; init; } = string.Empty;

    /// <summary>Gets the downloadable file sources for this variant.</summary>
    [JsonPropertyName("sources")]
    public IReadOnlyList<FontSource> Sources { get; init; } = [];
}

/// <summary>
/// A single downloadable file for a <see cref="FontVariant"/>.
/// </summary>
public sealed record FontSource
{
    /// <summary>Gets the font file format (e.g. <c>woff2</c>, <c>ttf</c>).</summary>
    [JsonPropertyName("format")]
    public string Format { get; init; } = string.Empty;

    /// <summary>Gets the URL from which the font file can be downloaded.</summary>
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}
