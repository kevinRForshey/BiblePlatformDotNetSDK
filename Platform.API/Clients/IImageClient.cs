using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Reads shareable verse images from the Bible Platform API.
/// </summary>
public interface IImageClient
{
    /// <summary>
    /// Lists verse images, optionally filtered to a specific passage.
    /// </summary>
    /// <param name="passageId">
    /// An optional USFM passage id (a single verse or verse range within one chapter, e.g.
    /// <c>JHN.3.16</c> or <c>1CO.13.4-7</c>) to filter to images of that passage.
    /// </param>
    /// <param name="languageRanges">
    /// An optional ordered list of BCP 47 language ranges (e.g. <c>en</c>, <c>*</c>) to filter by language.
    /// </param>
    /// <param name="pageToken">An opaque token from a previous response's <c>next_page_token</c>, or <see langword="null"/> for the first page.</param>
    /// <param name="pageSize">The maximum number of images to return per page (1-99), or <see langword="null"/> to use the API default.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A page of verse images.</returns>
    Task<PagedResult<VerseImage>> GetImagesAsync(
        string? passageId = null,
        IReadOnlyList<string>? languageRanges = null,
        string? pageToken = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single verse image by its id.
    /// </summary>
    /// <param name="imageId">
    /// The image id, as returned from <see cref="GetImagesAsync"/> or another endpoint that
    /// references images (e.g. verse-of-the-day or search).
    /// </param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The requested verse image.</returns>
    Task<VerseImage> GetImageAsync(string imageId, CancellationToken cancellationToken = default);
}
