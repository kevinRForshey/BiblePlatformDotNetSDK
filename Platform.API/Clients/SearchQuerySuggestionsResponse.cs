using System.Text.Json.Serialization;

using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>Response envelope for <c>GET /v1/search-queries</c>.</summary>
internal sealed record SearchQuerySuggestionsResponse
{
    [JsonPropertyName("data")]
    public List<SearchQuerySuggestion> Data { get; init; } = [];
}
