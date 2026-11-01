using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A content license covering one or more Bible versions.
/// </summary>
public sealed record License
{
    /// <summary>Gets the license's numeric id.</summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Gets the license's display name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the license version number.</summary>
    [JsonPropertyName("version")]
    public int Version { get; init; }

    /// <summary>Gets the id of the organization that issued this license.</summary>
    [JsonPropertyName("organization_id")]
    public string OrganizationId { get; init; } = string.Empty;

    /// <summary>Gets the full license text as HTML.</summary>
    [JsonPropertyName("html")]
    public string Html { get; init; } = string.Empty;

    /// <summary>Gets the Bible version ids covered by this license.</summary>
    [JsonPropertyName("bible_ids")]
    public IReadOnlyList<int> BibleIds { get; init; } = [];

    /// <summary>Gets a map of content type (e.g. <c>bible</c>) to the number of items of that type this license offers.</summary>
    [JsonPropertyName("content_types")]
    public IReadOnlyDictionary<string, int> ContentTypes { get; init; } = new Dictionary<string, int>();

    /// <summary>Gets the URI of the full license document.</summary>
    [JsonPropertyName("uri")]
    public string Uri { get; init; } = string.Empty;

    /// <summary>Gets the date and time the caller (or <c>developer_id</c>) agreed to this license, if agreed.</summary>
    [JsonPropertyName("agreed_dt")]
    public DateTimeOffset? AgreedDt { get; init; }

    /// <summary>Gets the id of the user who agreed to this license, if agreed.</summary>
    [JsonPropertyName("yvp_user_id")]
    public string? YvpUserId { get; init; }
}
