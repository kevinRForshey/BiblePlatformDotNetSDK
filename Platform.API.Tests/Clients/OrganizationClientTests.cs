using System.Net;
using Microsoft.Extensions.Logging;
using System.Net.Http;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Platform.API.Clients;
using Platform.API.Exceptions;
using Platform.API.Tests.Fakes;

using Xunit;

namespace Platform.API.Tests.Clients;

public sealed class OrganizationClientTests
{
    private const string OrganizationJson = """
        {
          "id": "550e8400-e29b-41d4-a716-446655440000",
          "name": "Example Publisher",
          "description": "A sample publisher",
          "email": "contact@example.com",
          "phone": "+1-555-0100",
          "primary_language": "en",
          "website_url": "https://example.com",
          "address": {
            "formatted_address": "123 Main St, Austin, TX, USA",
            "formatted_locality": "Austin, TX",
            "place_id": "abc123",
            "latitude": 30.2672,
            "longitude": -97.7431,
            "administrative_area_level_1": { "short_name": "TX", "long_name": "Texas" },
            "locality": { "short_name": "Austin", "long_name": "Austin" },
            "country": { "short_name": "US", "long_name": "United States" }
          }
        }
        """;

    [Fact]
    public async Task GetOrganizationsAsync_ReturnsPage_WhenApiSucceeds()
    {
        var client = BuildClient(HttpStatusCode.OK, $$"""{"data": [{{OrganizationJson}}], "next_page_token": "tok"}""");

        var result = await client.GetOrganizationsAsync();

        result.Data.Should().HaveCount(1);
        result.Data[0].Name.Should().Be("Example Publisher");
        result.Data[0].Address!.AdministrativeAreaLevel1!.ShortName.Should().Be("TX");
        result.NextPageToken.Should().Be("tok");
    }

    [Fact]
    public async Task GetOrganizationsAsync_BuildsExpectedQueryString()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetOrganizationsAsync(bibleIds: [111, 206], pageToken: "tok", pageSize: 5);

        handler.LastRequest!.RequestUri!.PathAndQuery.Should()
            .Be("/v1/organizations?bible_ids[]=111&bible_ids[]=206&page_token=tok&page_size=5");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetOrganizationsAsync_ThrowsArgumentException_WhenPageTokenIsEmptyOrWhitespace(string pageToken)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetOrganizationsAsync(pageToken: pageToken);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("pageToken")
            .WithMessage("*empty or whitespace*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetOrganizationsAsync_ThrowsArgumentOutOfRangeException_WhenPageSizeIsNotPositive(int pageSize)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetOrganizationsAsync(pageSize: pageSize);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("pageSize")
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task GetOrganizationsAsync_AppliesAcceptLanguageHeader_WhenProvided()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetOrganizationsAsync(acceptLanguage: "es");

        handler.LastRequest!.Headers.AcceptLanguage.ToString().Should().Be("es");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetOrganizationsAsync_ThrowsBibleApiException_WhenApiReturnsError(HttpStatusCode status)
    {
        var client = BuildClient(status, "error body");
        var act = () => client.GetOrganizationsAsync();
        await act.Should().ThrowAsync<BibleApiException>().Where(e => e.StatusCode == status);
    }

    [Fact]
    public async Task GetOrganizationsAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(
            HttpStatusCode.OK, $$"""{"data": [{{OrganizationJson}}]}""");

        await client.GetOrganizationsAsync(pageToken: "tok");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching organizations (pageToken=tok).");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched 1 organization(s) from API.");
    }

    [Fact]
    public async Task GetOrganizationAsync_ReturnsOrganization_WhenApiSucceeds()
    {
        var client = BuildClient(HttpStatusCode.OK, OrganizationJson);

        var result = await client.GetOrganizationAsync("550e8400-e29b-41d4-a716-446655440000");

        result.Name.Should().Be("Example Publisher");
        result.Email.Should().Be("contact@example.com");
    }

    [Fact]
    public async Task GetOrganizationAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, OrganizationJson);
        var client = BuildClientFromHandler(handler);

        await client.GetOrganizationAsync("org-1");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/organizations/org-1");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetOrganizationAsync_ThrowsArgumentException_WhenOrganizationIdIsInvalid(string organizationId)
    {
        var client = BuildClient(HttpStatusCode.OK, OrganizationJson);
        var act = () => client.GetOrganizationAsync(organizationId);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("organizationId")
            .WithMessage("*is required*");
    }

    [Fact]
    public async Task GetOrganizationAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.GetOrganizationAsync("org-1");
        await act.Should().ThrowAsync<BibleEmptyResponseException>()
            .WithMessage("*org-1*");
    }

    [Fact]
    public async Task GetOrganizationAsync_AppliesAcceptLanguageHeader_WhenProvided()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, OrganizationJson);
        var client = BuildClientFromHandler(handler);

        await client.GetOrganizationAsync("org-1", acceptLanguage: "es");

        handler.LastRequest!.Headers.AcceptLanguage.ToString().Should().Be("es");
    }

    [Fact]
    public async Task GetOrganizationAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, OrganizationJson);

        await client.GetOrganizationAsync("org-1");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching organization org-1.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched organization org-1.");
    }

    [Fact]
    public async Task GetOrganizationBiblesAsync_ReturnsPage_WhenApiSucceeds()
    {
        const string json = """
            {
              "data": [
                {
                  "id": 3034,
                  "abbreviation": "BSB",
                  "localized_abbreviation": "BSB",
                  "title": "Berean Standard Bible",
                  "localized_title": "Berean Standard Bible",
                  "language_tag": "en",
                  "books": ["GEN", "EXO"],
                  "organization_id": "org-1"
                }
              ],
              "next_page_token": "tok2",
              "total_size": 1
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.GetOrganizationBiblesAsync("org-1");

        result.Data.Should().HaveCount(1);
        result.Data[0].Abbreviation.Should().Be("BSB");
        result.Data[0].Books.Should().Equal("GEN", "EXO");
        result.TotalSize.Should().Be(1);
    }

    [Fact]
    public async Task GetOrganizationBiblesAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetOrganizationBiblesAsync("org-1");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/organizations/org-1/bibles");
    }

    [Fact]
    public async Task GetOrganizationBiblesAsync_BuildsExpectedQueryString_WithPageTokenAndPageSize()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetOrganizationBiblesAsync("org-1", pageToken: "tok2", pageSize: 7);

        handler.LastRequest!.RequestUri!.PathAndQuery.Should()
            .Be("/v1/organizations/org-1/bibles?page_token=tok2&page_size=7");
    }

    [Fact]
    public async Task GetOrganizationBiblesAsync_BuildsExpectedQueryString_WithPageSizeOnly()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetOrganizationBiblesAsync("org-1", pageSize: 7);

        handler.LastRequest!.RequestUri!.PathAndQuery.Should()
            .Be("/v1/organizations/org-1/bibles?page_size=7");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetOrganizationBiblesAsync_ThrowsArgumentException_WhenOrganizationIdIsInvalid(string organizationId)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetOrganizationBiblesAsync(organizationId);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("organizationId")
            .WithMessage("*is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetOrganizationBiblesAsync_ThrowsArgumentException_WhenPageTokenIsEmptyOrWhitespace(string pageToken)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetOrganizationBiblesAsync("org-1", pageToken: pageToken);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("pageToken")
            .WithMessage("*empty or whitespace*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetOrganizationBiblesAsync_ThrowsArgumentOutOfRangeException_WhenPageSizeIsNotPositive(int pageSize)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetOrganizationBiblesAsync("org-1", pageSize: pageSize);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("pageSize")
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task GetOrganizationBiblesAsync_LogsDebugMessages()
    {
        const string json = """
            {"data": [{ "id": 3034, "abbreviation": "BSB", "localized_abbreviation": "BSB",
            "title": "Berean Standard Bible", "localized_title": "Berean Standard Bible",
            "language_tag": "en", "books": ["GEN"], "organization_id": "org-1" }]}
            """;
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, json);

        await client.GetOrganizationBiblesAsync("org-1");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug
            && e.Message == "Fetching Bible versions for organization org-1.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug
            && e.Message == "Fetched 1 Bible version(s) for organization org-1.");
    }

    private static OrganizationClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static OrganizationClient BuildClientFromHandler(FakeHttpMessageHandler handler, ILogger<OrganizationClient>? logger = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new OrganizationClient(httpClient, logger ?? NullLogger<OrganizationClient>.Instance);
    }

    private static (OrganizationClient Client, CapturingLogger<OrganizationClient> Logger) BuildClientWithLogger(HttpStatusCode status, string json)
    {
        var logger = new CapturingLogger<OrganizationClient>();
        var client = BuildClientFromHandler(new FakeHttpMessageHandler(status, json), logger);
        return (client, logger);
    }
}
