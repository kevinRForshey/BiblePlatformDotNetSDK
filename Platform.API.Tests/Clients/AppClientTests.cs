using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Platform.API.Clients;
using Platform.API.Exceptions;
using Platform.API.Models;
using Platform.API.Tests.Fakes;

using Xunit;

namespace Platform.API.Tests.Clients;

public sealed class AppClientTests
{
    [Fact]
    public async Task GetAppAsync_ReturnsApp_WhenApiSucceeds()
    {
        const string json = """
            {
              "app_id": "00000000-0000-0000-0000-000000000000",
              "name": "My App",
              "description": "A sample app",
              "website_url": "https://example.com",
              "status": "live"
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.GetAppAsync("00000000-0000-0000-0000-000000000000");

        result.AppId.Should().Be("00000000-0000-0000-0000-000000000000");
        result.Name.Should().Be("My App");
        result.Description.Should().Be("A sample app");
        result.WebsiteUrl.Should().Be("https://example.com");
        result.Status.Should().Be(AppStatus.Live);
    }

    [Theory]
    [InlineData(AppStatus.Development, "development")]
    [InlineData(AppStatus.Archived, "archived")]
    [InlineData(AppStatus.Unknown, "something_new")]
    public async Task GetAppAsync_MapsStatus(AppStatus expected, string wireValue)
    {
        var json = $$"""{"app_id": "a", "name": "n", "status": "{{wireValue}}"}""";
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.GetAppAsync("a");

        result.Status.Should().Be(expected);
    }

    [Fact]
    public async Task GetAppAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"app_id": "a", "name": "n", "status": "live"}""");
        var client = BuildClientFromHandler(handler);

        await client.GetAppAsync("a1b2");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/apps/a1b2");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetAppAsync_ThrowsArgumentException_WhenAppIdIsInvalid(string appId)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"app_id": "a", "name": "n", "status": "live"}""");
        var act = () => client.GetAppAsync(appId);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetAppAsync_ThrowsBibleApiException_WhenApiReturnsError(HttpStatusCode status)
    {
        var client = BuildClient(status, "error body");
        var act = () => client.GetAppAsync("a");
        await act.Should().ThrowAsync<BibleApiException>().Where(e => e.StatusCode == status);
    }

    [Fact]
    public async Task GetAppAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.GetAppAsync("a");
        await act.Should().ThrowAsync<BibleEmptyResponseException>();
    }

    private static AppClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static AppClient BuildClientFromHandler(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new AppClient(httpClient, NullLogger<AppClient>.Instance);
    }
}
