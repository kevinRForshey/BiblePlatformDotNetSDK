using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// Metadata about a language supported by the platform.
/// </summary>
public sealed record Language
{
    /// <summary>Gets the canonical BCP 47 language id (e.g. <c>en</c>, <c>sr-Latn</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the ISO 639 canonical language subtag (e.g. <c>en</c>, <c>sr</c>).</summary>
    [JsonPropertyName("language")]
    public string LanguageSubtag { get; init; } = string.Empty;

    /// <summary>Gets the ISO 15924 script code, if the id includes a script subtag.</summary>
    [JsonPropertyName("script")]
    public string? Script { get; init; }

    /// <summary>Gets the English name of <see cref="Script"/>, if present.</summary>
    [JsonPropertyName("script_name")]
    public string? ScriptName { get; init; }

    /// <summary>Gets legacy/deprecated subtags mapped to this language during canonicalization.</summary>
    [JsonPropertyName("aliases")]
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>Gets a map of locale to display name for this language.</summary>
    [JsonPropertyName("display_names")]
    public IReadOnlyDictionary<string, string> DisplayNames { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the display name chosen based on the request's <c>Accept-Language</c> header.
    /// </summary>
    [JsonPropertyName("localized_name")]
    public string? LocalizedName { get; init; }

    /// <summary>Gets all known scripts for this language.</summary>
    [JsonPropertyName("scripts")]
    public IReadOnlyList<string> Scripts { get; init; } = [];

    /// <summary>Gets the variants associated with this language.</summary>
    [JsonPropertyName("variants")]
    public IReadOnlyList<string> Variants { get; init; } = [];

    /// <summary>Gets the countries where this language is spoken.</summary>
    [JsonPropertyName("countries")]
    public IReadOnlyList<string> Countries { get; init; } = [];

    /// <summary>Gets the text direction (e.g. <c>ltr</c>, <c>rtl</c>), if known.</summary>
    [JsonPropertyName("text_direction")]
    public string? TextDirection { get; init; }

    /// <summary>Gets the estimated writing population, if known.</summary>
    [JsonPropertyName("writing_population")]
    public int? WritingPopulation { get; init; }

    /// <summary>Gets the estimated speaking population, if known.</summary>
    [JsonPropertyName("speaking_population")]
    public int? SpeakingPopulation { get; init; }

    /// <summary>Gets the default Bible version id for this language, if known.</summary>
    [JsonPropertyName("default_bible_id")]
    public int? DefaultBibleId { get; init; }
}
