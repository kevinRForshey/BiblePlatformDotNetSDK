using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Platform.API.Configuration;
using Platform.API.Http;
using Platform.API.Tests.Fakes;
using Xunit;

namespace Platform.API.Tests.Http;

public sealed class OutboundRateLimitingHandlerTests
{
    [Fact]
    public async Task SendAsync_AllowsRequest_WhenPermitAvailable()
    {
        var (inner, httpClient) = BuildPipeline(new BibleApiOptions
        {
            AppKey = "key",
            OutboundRequestsPerSecond = 10,
            OutboundBurstSize = 20,
            OutboundQueueLimit = 10
        });

        var response = await httpClient.GetAsync("/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_ThrowsHttpRequestException_WhenRateLimitIsExceededAndNoQueue()
    {
        var (_, httpClient) = BuildPipeline(new BibleApiOptions
        {
            AppKey = "key",
            OutboundRequestsPerSecond = 1,
            OutboundBurstSize = 1,
            OutboundQueueLimit = 0
        });

        await httpClient.GetAsync("/test");
        var act = () => httpClient.GetAsync("/test");

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*rate-limited*");
    }

    [Fact]
    public async Task SendAsync_LogsWarning_WhenRateLimited()
    {
        var inner = new CapturingHandler();
        var logger = new CapturingLogger<OutboundRateLimitingHandler>();
        var limiter = new OutboundRateLimitingHandler(
            Options.Create(new BibleApiOptions
            {
                AppKey = "key",
                OutboundRequestsPerSecond = 1,
                OutboundBurstSize = 1,
                OutboundQueueLimit = 0
            }),
            logger)
        {
            InnerHandler = inner
        };
        var httpClient = new HttpClient(limiter) { BaseAddress = new Uri("https://api.youversion.com") };

        await httpClient.GetAsync("/test");
        var act = () => httpClient.GetAsync("/test");
        await act.Should().ThrowAsync<HttpRequestException>();

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Warning
            && e.Message == "Outbound request rate-limited locally: GET https://api.youversion.com/test");
    }

    [Fact]
    public async Task SendAsync_AutomaticallyReplenishesTokens_AfterPeriodElapses()
    {
        var (inner, httpClient) = BuildPipeline(new BibleApiOptions
        {
            AppKey = "key",
            OutboundRequestsPerSecond = 1,
            OutboundBurstSize = 1,
            OutboundQueueLimit = 0
        });

        await httpClient.GetAsync("/test");
        await Task.Delay(TimeSpan.FromSeconds(1.2));
        var response = await httpClient.GetAsync("/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.CallCount.Should().Be(2);
    }

    [Fact]
    public void Ctor_ThrowsInvalidOperationException_WhenRequestsPerSecondIsZero()
    {
        var options = Options.Create(new BibleApiOptions
        {
            AppKey = "key",
            OutboundRequestsPerSecond = 0,
            OutboundBurstSize = 5,
            OutboundQueueLimit = 0
        });

        var act = () => new OutboundRateLimitingHandler(options, NullLogger<OutboundRateLimitingHandler>.Instance);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OutboundRequestsPerSecond must be greater than zero*");
    }

    [Fact]
    public void Ctor_ThrowsInvalidOperationException_WhenQueueLimitIsNegative()
    {
        var options = Options.Create(new BibleApiOptions
        {
            AppKey = "key",
            OutboundRequestsPerSecond = 1,
            OutboundBurstSize = 1,
            OutboundQueueLimit = -1
        });

        var act = () => new OutboundRateLimitingHandler(options, NullLogger<OutboundRateLimitingHandler>.Instance);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OutboundQueueLimit cannot be negative*");
    }

    [Fact]
    public void Ctor_ThrowsInvalidOperationException_WhenBurstIsLessThanRequestsPerSecond()
    {
        var options = Options.Create(new BibleApiOptions
        {
            AppKey = "key",
            OutboundRequestsPerSecond = 10,
            OutboundBurstSize = 5,
            OutboundQueueLimit = 0
        });

        var act = () => new OutboundRateLimitingHandler(options, NullLogger<OutboundRateLimitingHandler>.Instance);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OutboundBurstSize*");
    }

    [Fact]
    public async Task Dispose_DisposesRateLimiter()
    {
        var limiter = new OutboundRateLimitingHandler(
            Options.Create(new BibleApiOptions
            {
                AppKey = "key",
                OutboundRequestsPerSecond = 10,
                OutboundBurstSize = 10,
                OutboundQueueLimit = 0
            }),
            NullLogger<OutboundRateLimitingHandler>.Instance);

        var disposeMethod = typeof(OutboundRateLimitingHandler)
            .GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(bool) })!;
        disposeMethod.Invoke(limiter, new object[] { true });

        var rateLimiterField = typeof(OutboundRateLimitingHandler)
            .GetField("_rateLimiter", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var rateLimiter = (RateLimiter)rateLimiterField.GetValue(limiter)!;

        var act = () => rateLimiter.AcquireAsync(1).AsTask();
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_WithFalse_DoesNotDisposeRateLimiter()
    {
        var limiter = new OutboundRateLimitingHandler(
            Options.Create(new BibleApiOptions
            {
                AppKey = "key",
                OutboundRequestsPerSecond = 10,
                OutboundBurstSize = 10,
                OutboundQueueLimit = 0
            }),
            NullLogger<OutboundRateLimitingHandler>.Instance);

        var disposeMethod = typeof(OutboundRateLimitingHandler)
            .GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(bool) })!;
        disposeMethod.Invoke(limiter, new object[] { false });

        var rateLimiterField = typeof(OutboundRateLimitingHandler)
            .GetField("_rateLimiter", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var rateLimiter = (RateLimiter)rateLimiterField.GetValue(limiter)!;

        var lease = rateLimiter.AttemptAcquire(1);
        lease.IsAcquired.Should().BeTrue();
    }

    [Fact]
    public void Dispose_WithTrue_DisposesInnerHandler()
    {
        var inner = new CapturingHandler();
        var limiter = new OutboundRateLimitingHandler(
            Options.Create(new BibleApiOptions
            {
                AppKey = "key",
                OutboundRequestsPerSecond = 10,
                OutboundBurstSize = 10,
                OutboundQueueLimit = 0
            }),
            NullLogger<OutboundRateLimitingHandler>.Instance)
        {
            InnerHandler = inner
        };

        var disposeMethod = typeof(OutboundRateLimitingHandler)
            .GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(bool) })!;
        disposeMethod.Invoke(limiter, new object[] { true });

        inner.Disposed.Should().BeTrue();
    }

    private static (CapturingHandler inner, HttpClient httpClient) BuildPipeline(BibleApiOptions options)
    {
        var inner = new CapturingHandler();
        var limiter = new OutboundRateLimitingHandler(
            Options.Create(options),
            NullLogger<OutboundRateLimitingHandler>.Instance)
        {
            InnerHandler = inner
        };

        var httpClient = new HttpClient(limiter)
        {
            BaseAddress = new Uri("https://api.youversion.com")
        };

        return (inner, httpClient);
    }
}

