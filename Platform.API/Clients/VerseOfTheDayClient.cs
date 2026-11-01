using Microsoft.Extensions.Logging;

using Platform.API.Exceptions;
using Platform.API.Http;
using Platform.API.Models;

using System.Text;

namespace Platform.API.Clients;

/// <summary>
/// Default implementation of <see cref="IVerseOfTheDayClient"/>.
/// </summary>
internal sealed class VerseOfTheDayClient(HttpClient httpClient, ILogger<VerseOfTheDayClient> logger) : IVerseOfTheDayClient
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<VerseOfTheDay>> GetVerseOfTheDaysAsync(
        IReadOnlyList<string>? languageRanges = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("/v1/verse-of-the-days", languageRanges);
        logger.LogDebug("Fetching verse-of-the-day calendar.");

        var result = await ApiRequestHelper.GetJsonAsync<VerseOfTheDaysResponse>(httpClient, url, logger, cancellationToken)
            .ConfigureAwait(false);

        var days = result?.Data ?? [];
        logger.LogDebug("Fetched {Count} verse-of-the-day entr(y/ies) from API.", days.Count);
        return days;
    }

    /// <inheritdoc />
    public async Task<VerseOfTheDay> GetVerseOfTheDayAsync(
        int day,
        IReadOnlyList<string>? languageRanges = null,
        CancellationToken cancellationToken = default)
    {
        if (day is < 1 or > 366)
            throw new ArgumentOutOfRangeException(nameof(day), day, "Day must be between 1 and 366.");

        var url = BuildUrl($"/v1/verse-of-the-days/{day}", languageRanges);
        logger.LogDebug("Fetching verse-of-the-day entry for day {Day}.", day);

        var result = await ApiRequestHelper.GetJsonAsync<VerseOfTheDay>(httpClient, url, logger, cancellationToken)
            .ConfigureAwait(false);

        var entry = result ?? throw new BibleEmptyResponseException(
            $"The verse-of-the-day entry for day {day} was not found or returned an empty response.");

        logger.LogDebug("Fetched verse-of-the-day entry for day {Day}.", day);
        return entry;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string BuildUrl(string basePath, IReadOnlyList<string>? languageRanges)
    {
        if (languageRanges is null || languageRanges.Count == 0)
            return basePath;

        var sb = new StringBuilder(basePath).Append('?');
        for (var i = 0; i < languageRanges.Count; i++)
        {
            if (i > 0)
                sb.Append('&');

            sb.Append("language_ranges[]=").Append(Uri.EscapeDataString(languageRanges[i]));
        }

        return sb.ToString();
    }
}
