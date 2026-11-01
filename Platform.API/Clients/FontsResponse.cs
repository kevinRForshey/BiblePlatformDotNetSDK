using System.Text.Json.Serialization;

using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>Response envelope for <c>GET /v1/fonts</c>.</summary>
internal sealed record FontsResponse
{
    [JsonPropertyName("data")]
    public List<Font> Data { get; init; } = [];
}
