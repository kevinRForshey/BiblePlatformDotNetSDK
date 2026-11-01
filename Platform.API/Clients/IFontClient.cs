using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Reads font metadata and stylesheets from the Bible Platform API.
/// </summary>
public interface IFontClient
{
    /// <summary>
    /// Lists all font families available on the platform.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The available font families.</returns>
    Task<IReadOnlyList<Font>> GetFontsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single font family by its numeric id.
    /// </summary>
    /// <param name="fontId">The font's numeric id.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The requested font family.</returns>
    Task<Font> GetFontAsync(int fontId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the CSS stylesheet (<c>@font-face</c> rules) for a font family.
    /// </summary>
    /// <param name="fontId">The font's numeric id.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The raw CSS stylesheet text.</returns>
    Task<string> GetStylesheetAsync(int fontId, CancellationToken cancellationToken = default);
}
