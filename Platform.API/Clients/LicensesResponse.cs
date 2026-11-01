using System.Text.Json.Serialization;

using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>Response envelope for <c>GET /v1/licenses</c>.</summary>
internal sealed record LicensesResponse
{
    [JsonPropertyName("data")]
    public List<License> Data { get; init; } = [];
}
