using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Platform.API.Clients;
using Platform.API.Configuration;
using Platform.API.Tests.Fakes;

using Xunit;

namespace Platform.API.Tests.Clients;

public sealed class CachingBibleClientTests
{
    // -------------------------------------------------------------------------
    // GetVersionsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetVersionsAsync_HitsInnerClientOnlyOnce_ForRepeatedCallsWithSameParameters()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data":[],"next_page_token":null}""");
        var client = BuildClient(handler);

        await client.GetVersionsAsync("en");
        await client.GetVersionsAsync("en");

        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task GetVersionsAsync_HitsInnerClientTwice_ForDifferentLanguageRanges()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data":[],"next_page_token":null}""");
        var client = BuildClient(handler);

        await client.GetVersionsAsync("en");
        await client.GetVersionsAsync("es");

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task GetVersionsAsync_HitsInnerClientTwice_ForDifferentPageTokens()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data":[],"next_page_token":null}""");
        var client = BuildClient(handler);

        await client.GetVersionsAsync("en", pageToken: "abc");
        await client.GetVersionsAsync("en", pageToken: "xyz");

        handler.RequestCount.Should().Be(2);
    }

    // -------------------------------------------------------------------------
    // GetVersionAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetVersionAsync_HitsInnerClientOnlyOnce_ForRepeatedCallsWithSameVersionId()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"id":3034,"abbreviation":"BSB"}""");
        var client = BuildClient(handler);

        var first = await client.GetVersionAsync(3034);
        var second = await client.GetVersionAsync(3034);

        handler.RequestCount.Should().Be(1);
        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task GetVersionAsync_HitsInnerClientTwice_ForDifferentVersionIds()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"id":3034,"abbreviation":"BSB"}""");
        var client = BuildClient(handler);

        await client.GetVersionAsync(3034);
        await client.GetVersionAsync(111);

        handler.RequestCount.Should().Be(2);
    }

    // -------------------------------------------------------------------------
    // GetIndexAsync / GetBooksAsync / GetChaptersAsync / GetVersesAsync share one cached index
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetIndexAsync_HitsInnerClientOnlyOnce_ForRepeatedCallsWithSameVersionId()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SampleIndexJson);
        var client = BuildClient(handler);

        await client.GetIndexAsync(3034);
        await client.GetIndexAsync(3034);

        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task GetIndexAsync_HitsInnerClientTwice_ForDifferentVersionIds()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SampleIndexJson);
        var client = BuildClient(handler);

        await client.GetIndexAsync(3034);
        await client.GetIndexAsync(111);

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task GetBooksAsync_ReusesIndexCachedByGetIndexAsync()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SampleIndexJson);
        var client = BuildClient(handler);

        await client.GetIndexAsync(3034);
        var books = await client.GetBooksAsync(3034);

        handler.RequestCount.Should().Be(1);
        books.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBooksAsync_GetChaptersAsync_GetVersesAsync_ShareOneCachedIndexFetch_ForSameVersion()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SampleIndexJson);
        var client = BuildClient(handler);

        await client.GetBooksAsync(3034);
        await client.GetChaptersAsync(3034, "GEN");
        var verses = await client.GetVersesAsync(3034, "GEN", 1);

        handler.RequestCount.Should().Be(1);
        verses.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetChaptersAsync_ThrowsArgumentException_WhenBookUsfmIsEmptyOrWhitespace(string bookUsfm)
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, SampleIndexJson));

        var act = () => client.GetChaptersAsync(3034, bookUsfm);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetVersesAsync_ThrowsArgumentException_WhenBookUsfmIsEmptyOrWhitespace(string bookUsfm)
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, SampleIndexJson));

        var act = () => client.GetVersesAsync(3034, bookUsfm, 1);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetVersesAsync_ThrowsArgumentOutOfRangeException_WhenChapterNumberIsNotPositive(int chapterNumber)
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, SampleIndexJson));

        var act = () => client.GetVersesAsync(3034, "GEN", chapterNumber);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // -------------------------------------------------------------------------
    // TTL wiring (which BibleCacheOptions value backs which cache entry)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetVersionsAsync_UsesVersionsTtl()
    {
        var opts = new BibleCacheOptions
        {
            VersionsTtl = TimeSpan.FromMinutes(1),
            BooksTtl = TimeSpan.FromMinutes(2),
            PassageTtl = TimeSpan.FromMinutes(3),
        };
        var (client, cache) = BuildClientWithFakeCache(
            new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data":[],"next_page_token":null}"""), opts);

        await client.GetVersionsAsync("en");

        cache.ExpirationFor("yv:versions:en::").Should().Be(opts.VersionsTtl);
    }

    [Fact]
    public async Task GetVersionAsync_UsesVersionsTtl()
    {
        var opts = new BibleCacheOptions
        {
            VersionsTtl = TimeSpan.FromMinutes(1),
            BooksTtl = TimeSpan.FromMinutes(2),
            PassageTtl = TimeSpan.FromMinutes(3),
        };
        var (client, cache) = BuildClientWithFakeCache(
            new FakeHttpMessageHandler(HttpStatusCode.OK, """{"id":3034,"abbreviation":"BSB"}"""), opts);

        await client.GetVersionAsync(3034);

        cache.ExpirationFor("yv:version:3034").Should().Be(opts.VersionsTtl);
    }

    [Fact]
    public async Task GetIndexAsync_UsesBooksTtl_NotVersionsTtl()
    {
        var opts = new BibleCacheOptions
        {
            VersionsTtl = TimeSpan.FromMinutes(1),
            BooksTtl = TimeSpan.FromMinutes(2),
            PassageTtl = TimeSpan.FromMinutes(3),
        };
        var (client, cache) = BuildClientWithFakeCache(
            new FakeHttpMessageHandler(HttpStatusCode.OK, SampleIndexJson), opts);

        await client.GetIndexAsync(3034);

        cache.ExpirationFor("yv:index:3034").Should().Be(opts.BooksTtl);
    }

    [Fact]
    public async Task GetBooksAsync_UsesBooksTtl_ForTheUnderlyingIndexFetch()
    {
        var opts = new BibleCacheOptions
        {
            VersionsTtl = TimeSpan.FromMinutes(1),
            BooksTtl = TimeSpan.FromMinutes(2),
            PassageTtl = TimeSpan.FromMinutes(3),
        };
        var (client, cache) = BuildClientWithFakeCache(
            new FakeHttpMessageHandler(HttpStatusCode.OK, SampleIndexJson), opts);

        await client.GetBooksAsync(3034);

        cache.ExpirationFor("yv:index:3034").Should().Be(opts.BooksTtl);
    }

    private const string SampleIndexJson = """
        {
          "text_direction": "ltr",
          "books": [
            {
              "id": "GEN", "title": "Genesis", "full_title": "The Book of Genesis",
              "abbreviation": "Gen.", "canon": "old_testament",
              "chapters": [
                {
                  "id": 1, "passage_id": "GEN.1", "title": 1,
                  "verses": [
                    { "id": 1, "passage_id": "GEN.1.1", "title": 1 },
                    { "id": 2, "passage_id": "GEN.1.2", "title": 2 }
                  ]
                },
                {
                  "id": 2, "passage_id": "GEN.2", "title": 2,
                  "verses": [
                    { "id": 1, "passage_id": "GEN.2.1", "title": 1 }
                  ]
                }
              ],
              "intro": { "id": "INTRO", "passage_id": "GEN.INTRO", "title": "Intro" }
            },
            {
              "id": "EXO", "title": "Exodus", "full_title": "The Book of Exodus",
              "abbreviation": "Exo.", "canon": "old_testament",
              "chapters": [
                {
                  "id": 1, "passage_id": "EXO.1", "title": 1,
                  "verses": [
                    { "id": 1, "passage_id": "EXO.1.1", "title": 1 }
                  ]
                }
              ]
            }
          ]
        }
        """;

    private static CachingBibleClient BuildClient(FakeHttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        return new CachingBibleClient(BuildInner(handler), cache, new BibleCacheOptions());
    }

    private static (CachingBibleClient Client, FakeHybridCache Cache) BuildClientWithFakeCache(
        FakeHttpMessageHandler handler, BibleCacheOptions opts)
    {
        var cache = new FakeHybridCache();
        return (new CachingBibleClient(BuildInner(handler), cache, opts), cache);
    }

    private static BibleClient BuildInner(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new BibleClient(httpClient, NullLogger<BibleClient>.Instance);
    }
}
