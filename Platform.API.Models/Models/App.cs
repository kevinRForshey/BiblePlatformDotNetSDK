using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// Public metadata about an app registered with the platform.
/// </summary>
public sealed record App
{
    /// <summary>Gets the app's unique identifier.</summary>
    [JsonPropertyName("app_id")]
    public string AppId { get; init; } = string.Empty;

    /// <summary>Gets the app's display name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the app's description, if provided.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Gets the app's website URL, if provided.</summary>
    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; init; }

    /// <summary>Gets the app's lifecycle status.</summary>
    [JsonPropertyName("status")]
    public AppStatus Status { get; init; }
}
