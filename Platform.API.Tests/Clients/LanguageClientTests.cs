using System.Net;
using System.Net.Http;

using FluentAssertions;

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
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetLanguageAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.GetLanguageAsync("en");
        await act.Should().ThrowAsync<BibleEmptyResponseException>();
    }

    private static LanguageClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static LanguageClient BuildClientFromHandler(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new LanguageClient(httpClient, NullLogger<LanguageClient>.Instance);
    }
}
