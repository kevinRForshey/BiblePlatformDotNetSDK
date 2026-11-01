using Microsoft.Extensions.Logging;

using Platform.API.Exceptions;
using Platform.API.Http;
using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Default implementation of <see cref="IFontClient"/>.
/// </summary>
internal sealed class FontClient(HttpClient httpClient, ILogger<FontClient> logger) : IFontClient
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Font>> GetFontsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching font families.");

        var result = await ApiRequestHelper.GetJsonAsync<FontsResponse>(httpClient, "/v1/fonts", logger, cancellationToken)
            .ConfigureAwait(false);

        var fonts = result?.Data ?? [];
        logger.LogDebug("Fetched {Count} font famil(y/ies) from API.", fonts.Count);
        return fonts;
    }

    /// <inheritdoc />
    public async Task<Font> GetFontAsync(int fontId, CancellationToken cancellationToken = default)
    {
        if (fontId <= 0)
            throw new ArgumentOutOfRangeException(nameof(fontId), fontId, "Font id must be greater than zero.");

        logger.LogDebug("Fetching font {FontId}.", fontId);

        var result = await ApiRequestHelper.GetJsonAsync<Font>(httpClient, $"/v1/fonts/{fontId}", logger, cancellationToken)
            .ConfigureAwait(false);

        var font = result ?? throw new BibleEmptyResponseException(
            $"Font {fontId} was not found or returned an empty response.");

        logger.LogDebug("Fetched font {FontId} ({Family}).", fontId, font.Family);
        return font;
    }

    /// <inheritdoc />
    public async Task<string> GetStylesheetAsync(int fontId, CancellationToken cancellationToken = default)
    {
        if (fontId <= 0)
            throw new ArgumentOutOfRangeException(nameof(fontId), fontId, "Font id must be greater than zero.");

        var url = $"/v1/fonts/{fontId}/stylesheet";
        logger.LogDebug("Fetching stylesheet for font {FontId}.", fontId);

        using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        await ApiRequestHelper.EnsureSuccessAsync(response, url, logger, cancellationToken).ConfigureAwait(false);

        var css = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Fetched stylesheet for font {FontId} ({Length} chars).", fontId, css.Length);
        return css;
    }
}
