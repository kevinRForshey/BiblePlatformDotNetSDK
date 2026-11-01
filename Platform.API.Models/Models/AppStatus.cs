using Platform.API.Models.Json;

using System.Text.Json.Serialization;

namespace Platform.API.Models;

/// <summary>
/// The lifecycle status of an app registered with the platform.
/// </summary>
[JsonConverter(typeof(AppStatusJsonConverter))]
public enum AppStatus
{
    /// <summary>The status could not be determined from the API response.</summary>
    Unknown,

    /// <summary>The app is in active development and not yet publicly available.</summary>
    Development,

    /// <summary>The app is live and publicly available.</summary>
    Live,

    /// <summary>The app has been archived and is no longer active.</summary>
    Archived
}
