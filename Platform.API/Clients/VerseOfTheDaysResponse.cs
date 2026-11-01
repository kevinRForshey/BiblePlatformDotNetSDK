using System.Text.Json.Serialization;

using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>Response envelope for <c>GET /v1/verse-of-the-days</c>.</summary>
internal sealed record VerseOfTheDaysResponse
{
    [JsonPropertyName("data")]
    public List<VerseOfTheDay> Data { get; init; } = [];
}
