using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// The permissions a signed-in user has granted to a calling app.
/// </summary>
public sealed record AppPermissions
{
    /// <summary>Gets the permissions the user has granted (e.g. <c>highlights</c>). Empty if none.</summary>
    [JsonPropertyName("permissions")]
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
