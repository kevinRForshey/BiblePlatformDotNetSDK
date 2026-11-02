using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Platform.API.Clients;
using Platform.API.Configuration;
using Platform.API.Models;
using Platform.API.Tests.Fakes;

using BiblePlatform.UsfmReferences;

using Xunit;

namespace Platform.API.Tests.Clients;

public sealed class CachingPassageClientTests
{
    private const string PassageJson = """
        { "id": "JHN.3.16", "content": "For God so loved the world...", "reference": "John 3:16" }
        """;

    [Fact]
    public async Task GetPassageAsync_HitsInnerClientOnlyOnce_ForRepeatedCallsWithSameParameters()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PassageJson);
        var client = BuildClient(handler);

        await client.GetPassageAsync(3034, TestReferences.John316);
        await client.GetPassageAsync(3034, TestReferences.John316);

        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPassageAsync_HitsInnerClientTwice_ForDifferentVersionIds()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PassageJson);
        var client = BuildClient(handler);

        await client.GetPassageAsync(3034, TestReferences.John316);
        await client.GetPassageAsync(111, TestReferences.John316);

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPassageAsync_HitsInnerClientTwice_ForDifferentReferences()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PassageJson);
        var client = BuildClient(handler);

        await client.GetPassageAsync(3034, TestReferences.John316);
        await client.GetPassageAsync(3034, TestReferences.Genesis1);

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPassageAsync_HitsInnerClientTwice_ForDifferentFormats()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PassageJson);
        var client = BuildClient(handler);

        await client.GetPassageAsync(3034, TestReferences.John316, new PassageRequestOptions { Format = PassageFormat.Text });
        await client.GetPassageAsync(3034, TestReferences.John316, new PassageRequestOptions { Format = PassageFormat.Html });

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPassageAsync_HitsInnerClientTwice_ForDifferentIncludeHeadings()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PassageJson);
        var client = BuildClient(handler);

        await client.GetPassageAsync(3034, TestReferences.John316, new PassageRequestOptions { IncludeHeadings = false });
        await client.GetPassageAsync(3034, TestReferences.John316, new PassageRequestOptions { IncludeHeadings = true });

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPassageAsync_HitsInnerClientTwice_ForDifferentIncludeNotes()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, PassageJson);
        var client = BuildClient(handler);

        await client.GetPassageAsync(3034, TestReferences.John316, new PassageRequestOptions { IncludeNotes = false });
        await client.GetPassageAsync(3034, TestReferences.John316, new PassageRequestOptions { IncludeNotes = true });

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPassageAsync_UsesPassageTtl()
    {
        var opts = new BibleCacheOptions
        {
            VersionsTtl = TimeSpan.FromMinutes(1),
            BooksTtl = TimeSpan.FromMinutes(2),
            PassageTtl = TimeSpan.FromMinutes(3),
        };
        var cache = new FakeHybridCache();
        var client = new CachingPassageClient(BuildInner(new FakeHttpMessageHandler(HttpStatusCode.OK, PassageJson)), cache, opts);

        await client.GetPassageAsync(3034, TestReferences.John316);

        var key = $"yv:passage:3034:{TestReferences.John316}:{PassageRequestOptions.Default.Format}:{PassageRequestOptions.Default.IncludeHeadings}:{PassageRequestOptions.Default.IncludeNotes}";
        cache.ExpirationFor(key).Should().Be(opts.PassageTtl);
    }

    private static CachingPassageClient BuildClient(FakeHttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        return new CachingPassageClient(BuildInner(handler), cache, new BibleCacheOptions());
    }

    private static PassageClient BuildInner(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.youversion.com") };
        var usfmService = new UsfmReferenceService();
        return new PassageClient(httpClient, NullLogger<PassageClient>.Instance, usfmService);
    }
}
