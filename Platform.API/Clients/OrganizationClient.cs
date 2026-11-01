using Microsoft.Extensions.Logging;

using Platform.API.Exceptions;
using Platform.API.Http;
using Platform.API.Models;

using System.Text;

namespace Platform.API.Clients;

/// <summary>
/// Default implementation of <see cref="IOrganizationClient"/>.
/// </summary>
internal sealed class OrganizationClient(HttpClient httpClient, ILogger<OrganizationClient> logger) : IOrganizationClient
{
    /// <inheritdoc />
    public async Task<PagedResult<Organization>> GetOrganizationsAsync(
        IReadOnlyList<int>? bibleIds = null,
        string? pageToken = null,
        int? pageSize = null,
        string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        if (pageToken is not null && string.IsNullOrWhiteSpace(pageToken))
            throw new ArgumentException("Page token cannot be empty or whitespace.", nameof(pageToken));

        if (pageSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than zero.");

        var url = BuildOrganizationsUrl(bibleIds, pageToken, pageSize);
        logger.LogDebug("Fetching organizations (pageToken={PageToken}).", pageToken);

        var result = await ApiRequestHelper.GetJsonAsync<PagedResult<Organization>>(
                httpClient, url, logger, cancellationToken, request => ApplyAcceptLanguage(request, acceptLanguage))
            .ConfigureAwait(false);

        var list = result ?? new PagedResult<Organization>();
        logger.LogDebug("Fetched {Count} organization(s) from API.", list.Data.Count);
        return list;
    }

    /// <inheritdoc />
    public async Task<Organization> GetOrganizationAsync(
        string organizationId,
        string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
            throw new ArgumentException("Organization id is required.", nameof(organizationId));

        logger.LogDebug("Fetching organization {OrganizationId}.", organizationId);

        var result = await ApiRequestHelper.GetJsonAsync<Organization>(
                httpClient, $"/v1/organizations/{organizationId}", logger, cancellationToken,
                request => ApplyAcceptLanguage(request, acceptLanguage))
            .ConfigureAwait(false);

        var organization = result ?? throw new BibleEmptyResponseException(
            $"Organization '{organizationId}' was not found or returned an empty response.");

        logger.LogDebug("Fetched organization {OrganizationId}.", organizationId);
        return organization;
    }

    /// <inheritdoc />
    public async Task<PagedResult<OrganizationBible>> GetOrganizationBiblesAsync(
        string organizationId,
        string? pageToken = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
            throw new ArgumentException("Organization id is required.", nameof(organizationId));

        if (pageToken is not null && string.IsNullOrWhiteSpace(pageToken))
            throw new ArgumentException("Page token cannot be empty or whitespace.", nameof(pageToken));

        if (pageSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than zero.");

        var url = BuildOrganizationBiblesUrl(organizationId, pageToken, pageSize);
        logger.LogDebug("Fetching Bible versions for organization {OrganizationId}.", organizationId);

        var result = await ApiRequestHelper.GetJsonAsync<PagedResult<OrganizationBible>>(httpClient, url, logger, cancellationToken)
            .ConfigureAwait(false);

        var list = result ?? new PagedResult<OrganizationBible>();
        logger.LogDebug("Fetched {Count} Bible version(s) for organization {OrganizationId}.", list.Data.Count, organizationId);
        return list;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void ApplyAcceptLanguage(HttpRequestMessage request, string? acceptLanguage)
    {
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
            request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
    }

    private static string BuildOrganizationsUrl(IReadOnlyList<int>? bibleIds, string? pageToken, int? pageSize)
    {
        var sb = new StringBuilder("/v1/organizations");
        var hasQuery = false;

        void AppendSeparator()
        {
            sb.Append(hasQuery ? '&' : '?');
            hasQuery = true;
        }

        if (bibleIds is not null)
        {
            foreach (var bibleId in bibleIds)
            {
                AppendSeparator();
                sb.Append("bible_ids[]=").Append(bibleId);
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

    private static string BuildOrganizationBiblesUrl(string organizationId, string? pageToken, int? pageSize)
    {
        var sb = new StringBuilder($"/v1/organizations/{Uri.EscapeDataString(organizationId)}/bibles");
        var hasQuery = false;

        if (pageToken is not null)
        {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append("page_token=").Append(Uri.EscapeDataString(pageToken));
            hasQuery = true;
        }

        if (pageSize.HasValue)
        {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append("page_size=").Append(pageSize.Value);
        }

        return sb.ToString();
    }
}
