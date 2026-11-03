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

public sealed class PermissionClientTests
{
    [Fact]
    public async Task GetPermissionsAsync_ReturnsPermissions_WhenApiSucceeds()
    {
        const string json = """{"permissions": ["highlights"]}""";
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.GetPermissionsAsync("app-1");

        result.Permissions.Should().ContainSingle().Which.Should().Be("highlights");
    }

    [Fact]
    public async Task GetPermissionsAsync_ReturnsEmpty_WhenNoneGranted()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"permissions": []}""");

        var result = await client.GetPermissionsAsync("app-1");

        result.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionsAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"permissions": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetPermissionsAsync("app-42");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/apps/app-42/permissions");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetPermissionsAsync_ThrowsArgumentException_WhenAppIdIsInvalid(string appId)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"permissions": []}""");
        var act = () => client.GetPermissionsAsync(appId);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*App id is required*");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetPermissionsAsync_ThrowsBibleApiException_WhenApiReturnsError(HttpStatusCode status)
    {
        var client = BuildClient(status, "error body");
        var act = () => client.GetPermissionsAsync("app-1");
        await act.Should().ThrowAsync<BibleApiException>().Where(e => e.StatusCode == status);
    }

    [Fact]
    public async Task GetPermissionsAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.GetPermissionsAsync("app-1");
        await act.Should().ThrowAsync<BibleEmptyResponseException>()
            .WithMessage("*app-1*");
    }

    [Fact]
    public async Task GetPermissionsAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, """{"permissions": ["highlights"]}""");

        await client.GetPermissionsAsync("app-1");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching permissions granted to app app-1.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched 1 permission(s) for app app-1.");
    }

    private static PermissionClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static PermissionClient BuildClientFromHandler(FakeHttpMessageHandler handler, ILogger<PermissionClient>? logger = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new PermissionClient(httpClient, logger ?? NullLogger<PermissionClient>.Instance);
    }

    private static (PermissionClient Client, CapturingLogger<PermissionClient> Logger) BuildClientWithLogger(HttpStatusCode status, string json)
    {
        var logger = new CapturingLogger<PermissionClient>();
        var client = BuildClientFromHandler(new FakeHttpMessageHandler(status, json), logger);
        return (client, logger);
    }
}
