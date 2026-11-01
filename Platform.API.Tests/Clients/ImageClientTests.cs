using System.Net;
using System.Net.Http;

using FluentAssertions;

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
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetImageAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.GetImageAsync("img-1");
        await act.Should().ThrowAsync<BibleEmptyResponseException>();
    }

    private static ImageClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static ImageClient BuildClientFromHandler(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new ImageClient(httpClient, NullLogger<ImageClient>.Instance);
    }
}
