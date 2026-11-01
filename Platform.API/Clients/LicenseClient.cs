using Microsoft.Extensions.Logging;

using Platform.API.Http;
using Platform.API.Models;

using System.Text;

namespace Platform.API.Clients;

/// <summary>
/// Default implementation of <see cref="ILicenseClient"/>.
/// </summary>
internal sealed class LicenseClient(HttpClient httpClient, ILogger<LicenseClient> logger) : ILicenseClient
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<License>> GetLicensesAsync(
        int bibleId,
        string? developerId = null,
        bool? allAvailable = null,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        if (bibleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bibleId), bibleId, "Bible id must be greater than zero.");

        if (developerId is not null && string.IsNullOrWhiteSpace(developerId))
            throw new ArgumentException("Developer id cannot be empty or whitespace.", nameof(developerId));

        if (contentType is not null && string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type cannot be empty or whitespace.", nameof(contentType));

        var url = BuildLicensesUrl(bibleId, developerId, allAvailable, contentType);
        logger.LogDebug("Fetching licenses for Bible {BibleId}.", bibleId);

        var result = await ApiRequestHelper.GetJsonAsync<LicensesResponse>(httpClient, url, logger, cancellationToken)
            .ConfigureAwait(false);

        var licenses = result?.Data ?? [];
        logger.LogDebug("Fetched {Count} license(s) for Bible {BibleId}.", licenses.Count, bibleId);
        return licenses;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string BuildLicensesUrl(int bibleId, string? developerId, bool? allAvailable, string? contentType)
    {
        var sb = new StringBuilder("/v1/licenses?bible_id=");
        sb.Append(bibleId);

        if (developerId is not null)
        {
            sb.Append("&developer_id=");
            sb.Append(Uri.EscapeDataString(developerId));
        }

        if (allAvailable.HasValue)
        {
            sb.Append("&all_available=");
            sb.Append(allAvailable.Value ? "true" : "false");
        }

        if (contentType is not null)
        {
            sb.Append("&content_type=");
            sb.Append(Uri.EscapeDataString(contentType));
        }

        return sb.ToString();
    }
}
