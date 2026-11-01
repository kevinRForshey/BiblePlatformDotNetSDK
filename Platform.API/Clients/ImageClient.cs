using Microsoft.Extensions.Logging;

using Platform.API.Exceptions;
using Platform.API.Http;
using Platform.API.Models;

using System.Text;

namespace Platform.API.Clients;

/// <summary>
/// Default implementation of <see cref="IImageClient"/>.
/// </summary>
internal sealed class ImageClient(HttpClient httpClient, ILogger<ImageClient> logger) : IImageClient
{
    /// <inheritdoc />
    public async Task<PagedResult<VerseImage>> GetImagesAsync(
        string? passageId = null,
        IReadOnlyList<string>? languageRanges = null,
        string? pageToken = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        if (passageId is not null && string.IsNullOrWhiteSpace(passageId))
            throw new ArgumentException("Passage id cannot be empty or whitespace.", nameof(passageId));

        if (pageToken is not null && string.IsNullOrWhiteSpace(pageToken))
            throw new ArgumentException("Page token cannot be empty or whitespace.", nameof(pageToken));

        if (pageSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than zero.");

        var url = BuildImagesUrl(passageId, languageRanges, pageToken, pageSize);
        logger.LogDebug("Fetching images (passageId={PassageId}).", passageId);

        var result = await ApiRequestHelper.GetJsonAsync<PagedResult<VerseImage>>(httpClient, url, logger, cancellationToken)
            .ConfigureAwait(false);

        var list = result ?? new PagedResult<VerseImage>();
        logger.LogDebug("Fetched {Count} image(s) from API.", list.Data.Count);
        return list;
    }

    /// <inheritdoc />
    public async Task<VerseImage> GetImageAsync(string imageId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageId))
            throw new ArgumentException("Image id is required.", nameof(imageId));

        logger.LogDebug("Fetching image {ImageId}.", imageId);

        var result = await ApiRequestHelper.GetJsonAsync<VerseImage>(httpClient, $"/v1/images/{imageId}", logger, cancellationToken)
            .ConfigureAwait(false);

        var image = result ?? throw new BibleEmptyResponseException(
            $"Image '{imageId}' was not found or returned an empty response.");

        logger.LogDebug("Fetched image {ImageId}.", imageId);
        return image;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string BuildImagesUrl(
        string? passageId, IReadOnlyList<string>? languageRanges, string? pageToken, int? pageSize)
    {
        var sb = new StringBuilder("/v1/images");
        var hasQuery = false;

        void AppendSeparator()
        {
            sb.Append(hasQuery ? '&' : '?');
            hasQuery = true;
        }

        if (passageId is not null)
        {
            AppendSeparator();
            sb.Append("passage_id=").Append(Uri.EscapeDataString(passageId));
        }

        if (languageRanges is not null)
        {
            foreach (var range in languageRanges)
            {
                AppendSeparator();
                sb.Append("language_ranges[]=").Append(Uri.EscapeDataString(range));
            }
        }

        if (pageToken is not null)
        {
            AppendSeparator();
            sb.Append("page_token=").Append(Uri.EscapeDataString(pageToken));
        }

        if (pageSize.HasValue)
        {
            AppendSeparator();
            sb.Append("page_size=").Append(pageSize.Value);
        }

        return sb.ToString();
    }
}
