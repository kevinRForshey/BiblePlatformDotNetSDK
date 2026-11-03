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

public sealed class FontClientTests
{
    private const string FontJson = """
        {
          "id": 1,
          "slug": "untitled-serif",
          "family": "Untitled Serif",
          "variants": [
            {
              "weight": 400,
              "style": "normal",
              "sources": [
                { "format": "woff2", "url": "https://cdn.youversion.com/fonts/untitled-serif/Untitled%20Serif.woff2" }
              ]
            }
          ]
        }
        """;

    [Fact]
    public async Task GetFontsAsync_ReturnsFonts_WhenApiSucceeds()
    {
        var client = BuildClient(HttpStatusCode.OK, $$"""{"data": [{{FontJson}}]}""");

        var result = await client.GetFontsAsync();

        result.Should().HaveCount(1);
        result[0].Slug.Should().Be("untitled-serif");
        result[0].Variants.Should().HaveCount(1);
        result[0].Variants[0].Weight.Should().Be(400);
        result[0].Variants[0].Sources[0].Format.Should().Be("woff2");
    }

    [Fact]
    public async Task GetFontsAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, $$"""{"data": [{{FontJson}}]}""");
        var client = BuildClientFromHandler(handler);

        await client.GetFontsAsync();

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/fonts");
    }

    [Fact]
    public async Task GetFontsAsync_ReturnsEmptyList_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");

        var result = await client.GetFontsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFontsAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, $$"""{"data": [{{FontJson}}]}""");

        await client.GetFontsAsync();

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching font families.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched 1 font famil(y/ies) from API.");
    }

    [Fact]
    public async Task GetFontAsync_ReturnsFont_WhenApiSucceeds()
    {
        var client = BuildClient(HttpStatusCode.OK, FontJson);

        var result = await client.GetFontAsync(1);

        result.Id.Should().Be(1);
        result.Family.Should().Be("Untitled Serif");
    }

    [Fact]
    public async Task GetFontAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, FontJson);
        var client = BuildClientFromHandler(handler);

        await client.GetFontAsync(42);

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/fonts/42");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetFontAsync_ThrowsArgumentOutOfRangeException_WhenFontIdIsInvalid(int fontId)
    {
        var client = BuildClient(HttpStatusCode.OK, FontJson);
        var act = () => client.GetFontAsync(fontId);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("fontId")
            .WithMessage("*greater than zero*");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetFontAsync_ThrowsBibleApiException_WhenApiReturnsError(HttpStatusCode status)
    {
        var client = BuildClient(status, "error body");
        var act = () => client.GetFontAsync(1);
        await act.Should().ThrowAsync<BibleApiException>().Where(e => e.StatusCode == status);
    }

    [Fact]
    public async Task GetFontAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.GetFontAsync(7);
        await act.Should().ThrowAsync<BibleEmptyResponseException>()
            .WithMessage("*Font 7*");
    }

    [Fact]
    public async Task GetFontAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, FontJson);

        await client.GetFontAsync(1);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching font 1.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched font 1 (Untitled Serif).");
    }

    [Fact]
    public async Task GetStylesheetAsync_ReturnsRawCss_WhenApiSucceeds()
    {
        const string css = "@font-face { font-family: 'Untitled Serif'; }";
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            css,
            response => response.Content = new StringContent(css, System.Text.Encoding.UTF8, "text/css"));
        var client = BuildClientFromHandler(handler);

        var result = await client.GetStylesheetAsync(1);

        result.Should().Be(css);
        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/fonts/1/stylesheet");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetStylesheetAsync_ThrowsArgumentOutOfRangeException_WhenFontIdIsInvalid(int fontId)
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "@font-face {}");
        var client = BuildClientFromHandler(handler);

        var act = () => client.GetStylesheetAsync(fontId);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("fontId")
            .WithMessage("*greater than zero*");
        handler.LastRequest.Should().BeNull("the guard must reject the id before any HTTP call");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetStylesheetAsync_ThrowsBibleApiException_WhenApiReturnsError(HttpStatusCode status)
    {
        var client = BuildClient(status, "error body");
        var act = () => client.GetStylesheetAsync(1);
        await act.Should().ThrowAsync<BibleApiException>().Where(e => e.StatusCode == status);
    }

    [Fact]
    public async Task GetStylesheetAsync_LogsDebugMessages()
    {
        const string css = "@font-face {}";
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            css,
            response => response.Content = new StringContent(css, System.Text.Encoding.UTF8, "text/css"));
        var logger = new CapturingLogger<FontClient>();
        var client = BuildClientFromHandler(handler, logger);

        await client.GetStylesheetAsync(1);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching stylesheet for font 1.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == $"Fetched stylesheet for font 1 ({css.Length} chars).");
    }

    private static FontClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static FontClient BuildClientFromHandler(FakeHttpMessageHandler handler, ILogger<FontClient>? logger = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new FontClient(httpClient, logger ?? NullLogger<FontClient>.Instance);
    }

    private static (FontClient Client, CapturingLogger<FontClient> Logger) BuildClientWithLogger(HttpStatusCode status, string json)
    {
        var logger = new CapturingLogger<FontClient>();
        var client = BuildClientFromHandler(new FakeHttpMessageHandler(status, json), logger);
        return (client, logger);
    }
}
