using System.Net;
using System.Net.Http;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Platform.API.Clients;
using Platform.API.Exceptions;
using Platform.API.Models;
using Platform.API.Tests.Fakes;

using Xunit;

namespace Platform.API.Tests.Clients;

public sealed class LanguageClientTests
{
    [Fact]
    public async Task GetLanguagesAsync_ReturnsPage_WhenApiSucceeds()
    {
        const string json = """
            {
              "data": [
                { "id": "en", "language": "en", "display_names": { "en": "English" } }
              ],
              "next_page_token": "abc",
              "total_size": 1
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.GetLanguagesAsync();

        result.Data.Should().HaveCount(1);
        result.Data[0].Id.Should().Be("en");
        result.NextPageToken.Should().Be("abc");
        result.TotalSize.Should().Be(1);
    }

    [Fact]
    public async Task GetLanguagesAsync_BuildsExpectedQueryString()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetLanguagesAsync(pageToken: "tok", pageSize: 10, country: "US", biblesAvailable: true);

        var query = handler.LastRequest!.RequestUri!.Query;
        query.Should().Contain("page_token=tok");
        query.Should().Contain("page_size=10");
        query.Should().Contain("country=US");
        query.Should().Contain("bibles_available=true");
    }

    [Fact]
    public async Task GetLanguagesAsync_RequestsExpectedPath_WhenNoFiltersProvided()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetLanguagesAsync();

        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be("/v1/languages");
    }

    [Fact]
    public async Task GetLanguagesAsync_BuildsExactQueryString_WithBiblesAvailableFalse()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetLanguagesAsync(pageToken: "tok", pageSize: 10, country: "US", biblesAvailable: false);

        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be(
            "/v1/languages?page_token=tok&page_size=10&country=US&bibles_available=false");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task GetLanguagesAsync_ThrowsArgumentException_WhenPageTokenIsWhitespace(string pageToken)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetLanguagesAsync(pageToken: pageToken);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Page token cannot be empty or whitespace*");
    }

    [Fact]
    public async Task GetLanguagesAsync_ThrowsArgumentOutOfRangeException_WhenPageSizeIsInvalid()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetLanguagesAsync(pageSize: 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*Page size must be greater than zero*");
    }

    [Fact]
    public async Task GetLanguagesAsync_DoesNotThrow_WhenPageSizeIsPositive()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetLanguagesAsync(pageSize: 5);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task GetLanguagesAsync_ThrowsArgumentException_WhenCountryIsWhitespace(string country)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetLanguagesAsync(country: country);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Country cannot be empty or whitespace*");
    }

    [Fact]
    public async Task GetLanguagesAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, """
            {"data": [{"id": "en", "language": "en", "display_names": {"en": "English"}}]}
            """);

        await client.GetLanguagesAsync(pageToken: "tok", country: "US");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug
            && e.Message == "Fetching languages (pageToken=tok, country=US).");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched 1 language(s) from API.");
    }

    [Fact]
    public async Task GetLanguagesAsync_SetsAcceptLanguageHeader_WhenProvided()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetLanguagesAsync(acceptLanguage: "fr");

        handler.LastRequest!.Headers.AcceptLanguage.ToString().Should().Contain("fr");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetLanguagesAsync_ThrowsBibleApiException_WhenApiReturnsError(HttpStatusCode status)
    {
        var client = BuildClient(status, "error body");
        var act = () => client.GetLanguagesAsync();
        await act.Should().ThrowAsync<BibleApiException>().Where(e => e.StatusCode == status);
    }

    [Fact]
    public async Task GetLanguageAsync_ReturnsLanguage_WhenApiSucceeds()
    {
        const string json = """
            {
              "id": "sr-Latn",
              "language": "sr",
              "script": "Latn",
              "script_name": "Latin",
              "display_names": { "en": "Serbian (Latin)" },
              "localized_name": "Srpski"
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.GetLanguageAsync("sr-Latn");

        result.Id.Should().Be("sr-Latn");
        result.LanguageSubtag.Should().Be("sr");
        result.Script.Should().Be("Latn");
        result.LocalizedName.Should().Be("Srpski");
    }

    [Fact]
    public async Task GetLanguageAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"id": "en", "language": "en"}""");
        var client = BuildClientFromHandler(handler);

        await client.GetLanguageAsync("en");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/languages/en");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetLanguageAsync_ThrowsArgumentException_WhenLanguageIdIsInvalid(string languageId)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"id": "en", "language": "en"}""");
        var act = () => client.GetLanguageAsync(languageId);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Language id is required*");
    }

    [Fact]
    public async Task GetLanguageAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.GetLanguageAsync("en");
        await act.Should().ThrowAsync<BibleEmptyResponseException>().WithMessage("*en*");
    }

    [Fact]
    public async Task GetLanguageAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, """{"id": "en", "language": "en"}""");

        await client.GetLanguageAsync("en");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching language en.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched language en.");
    }

    private static LanguageClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static LanguageClient BuildClientFromHandler(FakeHttpMessageHandler handler, ILogger<LanguageClient>? logger = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new LanguageClient(httpClient, logger ?? NullLogger<LanguageClient>.Instance);
    }

    private static (LanguageClient Client, CapturingLogger<LanguageClient> Logger) BuildClientWithLogger(HttpStatusCode status, string json)
    {
        var logger = new CapturingLogger<LanguageClient>();
        var client = BuildClientFromHandler(new FakeHttpMessageHandler(status, json), logger);
        return (client, logger);
    }
}
