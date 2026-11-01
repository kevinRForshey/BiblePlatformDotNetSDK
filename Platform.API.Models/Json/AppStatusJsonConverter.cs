using System.Text.Json;
using System.Text.Json.Serialization;

namespace Platform.API.Models.Json;

/// <summary>
/// Maps the wire's lower-case <c>status</c> values to <see cref="AppStatus"/>. Unrecognized
/// values deserialize to <see cref="AppStatus.Unknown"/> instead of throwing, so a future
/// status addition on the API side doesn't break deserialization of the rest of the app.
/// </summary>
internal sealed class AppStatusJsonConverter : JsonConverter<AppStatus>
{
    public override AppStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString() switch
        {
            "development" => AppStatus.Development,
            "live" => AppStatus.Live,
            "archived" => AppStatus.Archived,
            _ => AppStatus.Unknown
        };

    public override void Write(Utf8JsonWriter writer, AppStatus value, JsonSerializerOptions options)
        => writer.WriteStringValue(value switch
        {
            AppStatus.Development => "development",
            AppStatus.Live => "live",
            AppStatus.Archived => "archived",
            _ => "unknown"
        });
}
