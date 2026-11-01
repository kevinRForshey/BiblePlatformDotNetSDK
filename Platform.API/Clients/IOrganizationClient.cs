using Platform.API.Models;

namespace Platform.API.Clients;

/// <summary>
/// Reads publisher/ministry organization metadata from the Bible Platform API.
/// </summary>
public interface IOrganizationClient
{
    /// <summary>
    /// Lists organizations known to the platform.
    /// </summary>
    /// <param name="bibleIds">An optional set of Bible version ids to filter to organizations publishing those versions.</param>
    /// <param name="pageToken">An opaque token from a previous response's <c>next_page_token</c>, or <see langword="null"/> for the first page.</param>
    /// <param name="pageSize">The maximum number of organizations to return per page, or <see langword="null"/> to use the API default.</param>
    /// <param name="acceptLanguage">An optional BCP 47 language tag sent as the <c>Accept-Language</c> request header.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A page of organizations.</returns>
    Task<PagedResult<Organization>> GetOrganizationsAsync(
        IReadOnlyList<int>? bibleIds = null,
        string? pageToken = null,
        int? pageSize = null,
        string? acceptLanguage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single organization by its id.
    /// </summary>
    /// <param name="organizationId">The organization's unique id.</param>
    /// <param name="acceptLanguage">An optional BCP 47 language tag sent as the <c>Accept-Language</c> request header.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The requested organization.</returns>
    Task<Organization> GetOrganizationAsync(
        string organizationId,
        string? acceptLanguage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the Bible versions published by an organization.
    /// </summary>
    /// <param name="organizationId">The organization's unique id.</param>
    /// <param name="pageToken">An opaque token from a previous response's <c>next_page_token</c>, or <see langword="null"/> for the first page.</param>
    /// <param name="pageSize">The maximum number of versions to return per page, or <see langword="null"/> to use the API default.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A page of Bible versions published by the organization.</returns>
    Task<PagedResult<OrganizationBible>> GetOrganizationBiblesAsync(
        string organizationId,
        string? pageToken = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);
}
