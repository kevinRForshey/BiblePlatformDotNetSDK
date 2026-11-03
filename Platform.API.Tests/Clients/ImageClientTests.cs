using System.Net;
using System.Net.Http;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Platform.API.Clients;
using Platform.API.Exceptions;
using Platform.API.Tests.Fakes;

using Xunit;

namespace Platform.API.Tests.Clients;

public sealed class ImageClientTests
{
    private const string ImageJson = """
        {
          "id": "img-1",
          "image_url": "https://cdn.youversion.com/images/img-1.jpg",
          "passage_id": "JHN.3.16",
          "attribution": "© YouVersion",
          "language_tag": "en",
          "width": 1080,
          "height": 1920,
          "aspect_ratio": 0.562
        }
        """;

    [Fact]
    public async Task GetImagesAsync_ReturnsPage_WhenApiSucceeds()
    {
        var client = BuildClient(HttpStatusCode.OK, $$"""{"data": [{{ImageJson}}], "total_size": 1}""");

        var result = await client.GetImagesAsync();

        result.Data.Should().HaveCount(1);
        result.Data[0].PassageId.Should().Be("JHN.3.16");
        result.Data[0].AspectRatio.Should().Be(0.562);
        result.TotalSize.Should().Be(1);
    }

    [Fact]
    public async Task GetImagesAsync_BuildsExpectedQueryString()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetImagesAsync(passageId: "JHN.3.16", languageRanges: ["en", "es"], pageToken: "tok", pageSize: 10);

        var query = handler.LastRequest!.RequestUri!.Query;
        query.Should().Contain("passage_id=JHN.3.16");
        query.Should().Contain("language_ranges[]=en");
        query.Should().Contain("language_ranges[]=es");
        query.Should().Contain("page_token=tok");
        query.Should().Contain("page_size=10");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetImagesAsync_ThrowsBibleApiException_WhenApiReturnsError(HttpStatusCode status)
    {
        var client = BuildClient(status, "error body");
        var act = () => client.GetImagesAsync();
        await act.Should().ThrowAsync<BibleApiException>().Where(e => e.StatusCode == status);
    }

    [Fact]
    public async Task GetImagesAsync_RequestsExpectedPath_WhenNoFiltersProvided()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetImagesAsync();

        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be("/v1/images");
    }

    [Fact]
    public async Task GetImagesAsync_BuildsExactQueryString_WithAllFiltersInOrder()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetImagesAsync(passageId: "JHN.3.16", languageRanges: ["en", "es"], pageToken: "tok", pageSize: 10);

        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be(
            "/v1/images?passage_id=JHN.3.16&language_ranges[]=en&language_ranges[]=es&page_token=tok&page_size=10");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task GetImagesAsync_ThrowsArgumentException_WhenPassageIdIsWhitespace(string passageId)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetImagesAsync(passageId: passageId);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Passage id cannot be empty or whitespace*");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task GetImagesAsync_ThrowsArgumentException_WhenPageTokenIsWhitespace(string pageToken)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetImagesAsync(pageToken: pageToken);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Page token cannot be empty or whitespace*");
    }

    [Fact]
    public async Task GetImagesAsync_ThrowsArgumentOutOfRangeException_WhenPageSizeIsInvalid()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetImagesAsync(pageSize: 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*Page size must be greater than zero*");
    }

    [Fact]
    public async Task GetImagesAsync_DoesNotThrow_WhenPageSizeIsPositive()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetImagesAsync(pageSize: 5);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetImagesAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, $$"""{"data": [{{ImageJson}}]}""");

        await client.GetImagesAsync(passageId: "JHN.3.16");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching images (passageId=JHN.3.16).");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched 1 image(s) from API.");
    }

    [Fact]
    public async Task GetImageAsync_ReturnsImage_WhenApiSucceeds()
    {
        var client = BuildClient(HttpStatusCode.OK, ImageJson);

        var result = await client.GetImageAsync("img-1");

        result.Id.Should().Be("img-1");
        result.Width.Should().Be(1080);
        result.Height.Should().Be(1920);
    }

    [Fact]
    public async Task GetImageAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, ImageJson);
        var client = BuildClientFromHandler(handler);

        await client.GetImageAsync("img-42");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/images/img-42");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetImageAsync_ThrowsArgumentException_WhenImageIdIsInvalid(string imageId)
    {
        var client = BuildClient(HttpStatusCode.OK, ImageJson);
        var act = () => client.GetImageAsync(imageId);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Image id is required*");
    }

    [Fact]
    public async Task GetImageAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.GetImageAsync("img-1");
        await act.Should().ThrowAsync<BibleEmptyResponseException>().WithMessage("*img-1*");
    }

    [Fact]
    public async Task GetImageAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, ImageJson);

        await client.GetImageAsync("img-1");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching image img-1.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched image img-1.");
    }

    private static ImageClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static ImageClient BuildClientFromHandler(FakeHttpMessageHandler handler, ILogger<ImageClient>? logger = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new ImageClient(httpClient, logger ?? NullLogger<ImageClient>.Instance);
    }

    private static (ImageClient Client, CapturingLogger<ImageClient> Logger) BuildClientWithLogger(HttpStatusCode status, string json)
    {
        var logger = new CapturingLogger<ImageClient>();
        var client = BuildClientFromHandler(new FakeHttpMessageHandler(status, json), logger);
        return (client, logger);
    }
}
