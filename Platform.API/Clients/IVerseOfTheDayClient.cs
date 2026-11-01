using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Reads the platform's verse-of-the-day calendar.
/// </summary>
public interface IVerseOfTheDayClient
{
    /// <summary>
    /// Gets the full year's verse-of-the-day calendar.
    /// </summary>
    /// <param name="languageRanges">
    /// An optional ordered list of BCP 47 language ranges (e.g. <c>en</c>, <c>*</c>) used to
    /// select each day's verse art. The returned passage text is unaffected.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>One entry per day of the year.</returns>
    Task<IReadOnlyList<VerseOfTheDay>> GetVerseOfTheDaysAsync(
        IReadOnlyList<string>? languageRanges = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single day's verse-of-the-day entry.
    /// </summary>
    /// <param name="day">The day of year (1-366).</param>
    /// <param name="languageRanges">
    /// An optional ordered list of BCP 47 language ranges (e.g. <c>en</c>, <c>*</c>) used to
    /// select that day's verse art. The returned passage text is unaffected.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The requested day's verse-of-the-day entry.</returns>
    Task<VerseOfTheDay> GetVerseOfTheDayAsync(
        int day,
        IReadOnlyList<string>? languageRanges = null,
        CancellationToken cancellationToken = default);
}
