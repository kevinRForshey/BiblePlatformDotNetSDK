using System.Net;
using System.Net.Http;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Platform.API.Exceptions;
using Platform.API.Http;
using Platform.API.Tests.Fakes;

using Xunit;

namespace Platform.API.Tests.Http;

public sealed class ApiRequestHelperTests
{
    [Fact]
    public async Task EnsureSuccessAsync_DoesNotThrow_WhenResponseIsSuccess()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        var act = () => ApiRequestHelper.EnsureSuccessAsync(response, "/v1/test", NullLogger.Instance, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureSuccessAsync_ThrowsBibleApiException_WithStatusCodeMessageAndBody_WhenResponseFails()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Not Found",
            Content = new StringContent("the error body"),
        };

        var act = () => ApiRequestHelper.EnsureSuccessAsync(response, "/v1/test", NullLogger.Instance, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<BibleApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("API request to '/v1/test' failed with status 404 (Not Found).");
        exception.Which.ResponseBody.Should().Be("the error body");
    }

    [Fact]
    public async Task EnsureSuccessAsync_LogsError_WhenResponseFails()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Not Found",
            Content = new StringContent("the error body"),
        };
        var logger = new CapturingLogger<ApiRequestHelperTests>();

        var act = () => ApiRequestHelper.EnsureSuccessAsync(response, "/v1/test", logger, CancellationToken.None);
        await act.Should().ThrowAsync<BibleApiException>();

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Error &&
            e.Message == "API request to '/v1/test' failed with HTTP 404 Not Found. Response body: the error body");
    }

    [Fact]
    public async Task GetJsonAsync_ReturnsDeserializedBody_WhenResponseIsSuccess()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"id": 1}""");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.youversion.com") };

        var result = await ApiRequestHelper.GetJsonAsync<TestPayload>(httpClient, "/v1/test", NullLogger.Instance, CancellationToken.None);

        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetJsonAsync_ThrowsBibleApiException_WithoutSendingToAnotherUrl_WhenResponseFails()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "boom");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.youversion.com") };

        var act = () => ApiRequestHelper.GetJsonAsync<TestPayload>(httpClient, "/v1/test", NullLogger.Instance, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<BibleApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        exception.Which.ResponseBody.Should().Be("boom");
        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/test");
    }

    [Fact]
    public async Task GetJsonAsync_AppliesConfigureRequest_BeforeSending()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"id": 1}""");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.youversion.com") };

        await ApiRequestHelper.GetJsonAsync<TestPayload>(
            httpClient,
            "/v1/test",
            NullLogger.Instance,
            CancellationToken.None,
            request => request.Headers.Add("Accept-Language", "es"));

        handler.LastRequest!.Headers.GetValues("Accept-Language").Should().ContainSingle().Which.Should().Be("es");
    }

    private sealed record TestPayload
    {
        public int Id { get; init; }
    }
}
