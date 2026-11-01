using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A Bible version published by a specific <see cref="Organization"/>.
/// </summary>
public sealed record OrganizationBible
{
    /// <summary>Gets the Bible version's numeric id.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Gets the short abbreviation for the version (e.g. <c>NIV</c>, <c>BSB</c>).</summary>
    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; init; } = string.Empty;

    /// <summary>Gets the abbreviation localized to the version's language.</summary>
    [JsonPropertyName("localized_abbreviation")]
    public string LocalizedAbbreviation { get; init; } = string.Empty;

    /// <summary>Gets the full title of the Bible version.</summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the title localized to the version's language.</summary>
    [JsonPropertyName("localized_title")]
    public string LocalizedTitle { get; init; } = string.Empty;

    /// <summary>Gets the BCP-47 language tag (e.g. <c>en</c>, <c>es</c>).</summary>
    [JsonPropertyName("language_tag")]
    public string LanguageTag { get; init; } = string.Empty;

    /// <summary>Gets a free-text description of this version, if provided.</summary>
    [JsonPropertyName("info")]
    public string? Info { get; init; }

    /// <summary>Gets the publisher's website URL, if provided.</summary>
    [JsonPropertyName("publisher_url")]
    public string? PublisherUrl { get; init; }

    /// <summary>Gets the USFM book codes included in this version.</summary>
    [JsonPropertyName("books")]
    public IReadOnlyList<string> Books { get; init; } = [];

    /// <summary>Gets the deep link that opens this version in the YouVersion Bible App.</summary>
    [JsonPropertyName("youversion_deep_link")]
    public string? YouVersionDeepLink { get; init; }

    /// <summary>Gets the id of the organization that publishes this version.</summary>
    [JsonPropertyName("organization_id")]
    public string OrganizationId { get; init; } = string.Empty;
}
