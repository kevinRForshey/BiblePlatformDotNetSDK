using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// A publisher or ministry organization on the platform.
/// </summary>
public sealed record Organization
{
    /// <summary>Gets the organization's unique id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the organization's display name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the organization's description, if provided.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Gets the organization's contact email, if provided.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>Gets the organization's contact phone number, if provided.</summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    /// <summary>Gets the organization's primary language tag, if provided.</summary>
    [JsonPropertyName("primary_language")]
    public string? PrimaryLanguage { get; init; }

    /// <summary>Gets the organization's website URL, if provided.</summary>
    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; init; }

    /// <summary>Gets the organization's physical address, if provided.</summary>
    [JsonPropertyName("address")]
    public OrganizationAddress? Address { get; init; }
}

/// <summary>
/// A physical address associated with an <see cref="Organization"/>.
/// </summary>
public sealed record OrganizationAddress
{
    /// <summary>Gets the full formatted address.</summary>
    [JsonPropertyName("formatted_address")]
    public string? FormattedAddress { get; init; }

    /// <summary>Gets the formatted locality (city/town).</summary>
    [JsonPropertyName("formatted_locality")]
    public string? FormattedLocality { get; init; }

    /// <summary>Gets the Google Places id for this address, if known.</summary>
    [JsonPropertyName("place_id")]
    public string? PlaceId { get; init; }

    /// <summary>Gets the address's latitude, if known.</summary>
    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    /// <summary>Gets the address's longitude, if known.</summary>
    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }

    /// <summary>Gets the state/province component of this address, if known.</summary>
    [JsonPropertyName("administrative_area_level_1")]
    public AddressComponent? AdministrativeAreaLevel1 { get; init; }

    /// <summary>Gets the city/town component of this address, if known.</summary>
    [JsonPropertyName("locality")]
    public AddressComponent? Locality { get; init; }

    /// <summary>Gets the country component of this address, if known.</summary>
    [JsonPropertyName("country")]
    public AddressComponent? Country { get; init; }
}

/// <summary>
/// A single short/long-named component of an <see cref="OrganizationAddress"/>.
/// </summary>
public sealed record AddressComponent
{
    /// <summary>Gets the abbreviated form (e.g. <c>TX</c>, <c>US</c>).</summary>
    [JsonPropertyName("short_name")]
    public string? ShortName { get; init; }

    /// <summary>Gets the full form (e.g. <c>Texas</c>, <c>United States</c>).</summary>
    [JsonPropertyName("long_name")]
    public string? LongName { get; init; }
}
