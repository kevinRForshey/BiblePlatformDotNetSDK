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

public sealed class SearchClientTests
{
    [Fact]
    public async Task GetSearchQueriesAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetSearchQueriesAsync(["en"]);

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/search-queries");
    }

    [Fact]
    public async Task GetSearchQueriesAsync_ReturnsEmptyList_WhenDataIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": null}""");

        var result = await client.GetSearchQueriesAsync(["en"]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSearchQueriesAsync_BuildsExactQueryString_WithMultipleParameters()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetSearchQueriesAsync(["en"], query: "love", trending: true);

        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be(
            "/v1/search-queries?language_ranges[]=en&query=love&trending=true");
    }

    [Fact]
    public async Task GetSearchQueriesAsync_IncludesTrendingFalse_WhenTrendingIsExplicitlyFalse()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetSearchQueriesAsync(["en"], trending: false);

        var query = handler.LastRequest!.RequestUri!.Query;
        query.Should().Contain("trending=false");
        query.Should().NotContain("trending=true");
    }

    [Fact]
    public async Task GetSearchQueriesAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, """
            {"data": [{"text": "love", "source": "trending"}]}
            """);

        await client.GetSearchQueriesAsync(["en"], query: "lov");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching search query suggestions (query=lov).");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched 1 search query suggestion(s) from API.");
    }

    [Fact]
    public async Task GetSearchQueriesAsync_ReturnsSuggestions_WhenApiSucceeds()
    {
        const string json = """
            {
              "data": [
                { "text": "love", "source": "trending" },
                { "text": "love your neighbor", "source": "autocomplete" }
              ]
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.GetSearchQueriesAsync(["en"]);

        result.Should().HaveCount(2);
        result[0].Text.Should().Be("love");
        result[0].Source.Should().Be("trending");
    }

    [Fact]
    public async Task GetSearchQueriesAsync_BuildsExpectedQueryString()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetSearchQueriesAsync(["en", "es"], query: "lov", trending: true);

        var query = handler.LastRequest!.RequestUri!.Query;
        query.Should().Contain("language_ranges[]=en");
        query.Should().Contain("language_ranges[]=es");
        query.Should().Contain("query=lov");
        query.Should().Contain("trending=true");
    }

    [Fact]
    public async Task GetSearchQueriesAsync_ThrowsArgumentException_WhenLanguageRangesIsEmpty()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetSearchQueriesAsync([]);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*At least one language range is required*");
    }

    [Fact]
    public async Task GetSearchQueriesAsync_BuildsQueryString_WithCorrectSeparators_ForMultipleParams()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetSearchQueriesAsync(["en", "es"], query: "lov");

        handler.LastRequest!.RequestUri!.Query.Should().Be("?language_ranges[]=en&language_ranges[]=es&query=lov");
    }

    [Fact]
    public async Task SearchUnifiedAsync_ReturnsCombinedResult_WhenApiSucceeds()
    {
        const string json = """
            {
              "verses": [ { "reference": "JHN.3.16" } ],
              "topics": [ { "id": 1, "text": "love", "subtopics": ["grace"] } ],
              "images": [ { "id": "img-1", "passage_id": "JHN.3.16" } ],
              "user_intent": "text",
              "did_you_mean": [],
              "search_instead_for": null
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.SearchUnifiedAsync("love", 111, ["en"]);

        result.Verses.Should().ContainSingle().Which.Reference.Should().Be("JHN.3.16");
        result.Topics.Should().ContainSingle().Which.Text.Should().Be("love");
        result.Images.Should().ContainSingle().Which.Id.Should().Be("img-1");
        result.UserIntent.Should().Be("text");
    }

    [Fact]
    public async Task SearchUnifiedAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"verses": [], "topics": [], "images": []}""");
        var client = BuildClientFromHandler(handler);

        await client.SearchUnifiedAsync("love", 111, ["en"]);

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/search-unified");
    }

    [Fact]
    public async Task SearchUnifiedAsync_ThrowsArgumentException_WhenLanguageRangesIsEmpty()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"verses": [], "topics": [], "images": []}""");
        var act = () => client.SearchUnifiedAsync("love", 111, []);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SearchUnifiedAsync_LogsDebugMessage()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, """
            {"verses": [], "topics": [], "images": []}
            """);

        await client.SearchUnifiedAsync("love", 111, ["en"]);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug
            && e.Message == "Performing unified search for query 'love' in Bible version 111.");
    }

    [Fact]
    public async Task SearchUnifiedAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.SearchUnifiedAsync("love", 111, ["en"]);
        await act.Should().ThrowAsync<BibleEmptyResponseException>().WithMessage("*love*");
    }

    [Fact]
    public async Task SearchUnifiedAsync_BuildsExpectedQueryString()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """
            {"verses": [], "topics": [], "images": []}
            """);
        var client = BuildClientFromHandler(handler);

        await client.SearchUnifiedAsync("love", 111, ["en"], userIntent: "topical", fields: ["verses", "topics"]);

        var query = handler.LastRequest!.RequestUri!.Query;
        query.Should().Contain("query=love");
        query.Should().Contain("bible_id=111");
        query.Should().Contain("language_ranges[]=en");
        query.Should().Contain("user_intent=topical");
        query.Should().Contain("fields[]=verses");
        query.Should().Contain("fields[]=topics");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SearchUnifiedAsync_ThrowsArgumentException_WhenQueryIsInvalid(string query)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"verses": [], "topics": [], "images": []}""");
        var act = () => client.SearchUnifiedAsync(query, 111, ["en"]);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Query is required*");
    }

    [Fact]
    public async Task SearchUnifiedAsync_ThrowsArgumentOutOfRangeException_WhenBibleIdIsInvalid()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"verses": [], "topics": [], "images": []}""");
        var act = () => client.SearchUnifiedAsync("love", 0, ["en"]);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*Bible id must be greater than zero*");
    }

    [Fact]
    public async Task SearchTopicsAsync_ReturnsTopics_WhenApiSucceeds()
    {
        const string json = """
            {
              "topics": [ { "id": 1, "text": "love", "subtopics": ["grace"] } ],
              "did_you_mean": [],
              "search_instead_for": null,
              "total_size": 1
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.SearchTopicsAsync("love", ["en"]);

        result.Topics.Should().ContainSingle().Which.Text.Should().Be("love");
        result.TotalSize.Should().Be(1);
    }

    [Fact]
    public async Task SearchTopicsAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """
            {"topics": [], "did_you_mean": [], "total_size": 0}
            """);
        var client = BuildClientFromHandler(handler);

        await client.SearchTopicsAsync("love", ["en"]);

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/search-topics");
    }

    [Fact]
    public async Task SearchTopicsAsync_BuildsExpectedQueryString()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """
            {"topics": [], "did_you_mean": [], "total_size": 0}
            """);
        var client = BuildClientFromHandler(handler);

        await client.SearchTopicsAsync("love", ["en", "es"]);

        var query = handler.LastRequest!.RequestUri!.Query;
        query.Should().Contain("query=love");
        query.Should().Contain("language_ranges[]=en");
        query.Should().Contain("language_ranges[]=es");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SearchTopicsAsync_ThrowsArgumentException_WhenQueryIsInvalid(string query)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"topics": [], "did_you_mean": [], "total_size": 0}""");
        var act = () => client.SearchTopicsAsync(query, ["en"]);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SearchTopicsAsync_ThrowsArgumentException_WhenLanguageRangesIsEmpty()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"topics": [], "did_you_mean": [], "total_size": 0}""");
        var act = () => client.SearchTopicsAsync("love", []);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SearchTopicsAsync_LogsDebugMessage()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, """
            {"topics": [], "did_you_mean": [], "total_size": 0}
            """);

        await client.SearchTopicsAsync("love", ["en"]);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Searching topics for query 'love'.");
    }

    [Fact]
    public async Task SearchTopicsAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.SearchTopicsAsync("love", ["en"]);
        await act.Should().ThrowAsync<BibleEmptyResponseException>().WithMessage("*love*");
    }

    [Fact]
    public async Task SearchVersesAsync_ReturnsVerses_WhenApiSucceeds()
    {
        const string json = """
            {
              "verses": [ { "reference": "JHN.3.16" } ],
              "user_intent": "reference",
              "did_you_mean": [],
              "search_instead_for": null,
              "next_page_token": "token-1"
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.SearchVersesAsync("JHN 3 16", 111);

        result.Verses.Should().ContainSingle().Which.Reference.Should().Be("JHN.3.16");
        result.NextPageToken.Should().Be("token-1");
    }

    [Fact]
    public async Task SearchVersesAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"verses": []}""");
        var client = BuildClientFromHandler(handler);

        await client.SearchVersesAsync("love", 111);

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/search-verses");
    }

    [Fact]
    public async Task SearchVersesAsync_BuildsExpectedQueryString()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"verses": []}""");
        var client = BuildClientFromHandler(handler);

        await client.SearchVersesAsync("love", 111, userIntent: "topical", pageToken: "tok", pageSize: 10);

        var query = handler.LastRequest!.RequestUri!.Query;
        query.Should().Contain("query=love");
        query.Should().Contain("bible_id=111");
        query.Should().Contain("user_intent=topical");
        query.Should().Contain("page_token=tok");
        query.Should().Contain("page_size=10");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SearchVersesAsync_ThrowsArgumentException_WhenQueryIsInvalid(string query)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"verses": []}""");
        var act = () => client.SearchVersesAsync(query, 111);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SearchVersesAsync_ThrowsArgumentOutOfRangeException_WhenBibleIdIsInvalid()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"verses": []}""");
        var act = () => client.SearchVersesAsync("love", 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task SearchVersesAsync_ThrowsArgumentOutOfRangeException_WhenPageSizeIsInvalid()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"verses": []}""");
        var act = () => client.SearchVersesAsync("love", 111, pageSize: 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Page size must be greater than zero*");
    }

    [Fact]
    public async Task SearchVersesAsync_DoesNotThrow_WhenPageSizeIsPositive()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"verses": []}""");
        var act = () => client.SearchVersesAsync("love", 111, pageSize: 5);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SearchVersesAsync_LogsDebugMessage()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, """{"verses": []}""");

        await client.SearchVersesAsync("love", 111);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug
            && e.Message == "Searching verses for query 'love' in Bible version 111.");
    }

    [Fact]
    public async Task SearchVersesAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.SearchVersesAsync("love", 111);
        await act.Should().ThrowAsync<BibleEmptyResponseException>().WithMessage("*love*");
    }

    [Fact]
    public async Task SearchImagesAsync_ReturnsImages_WhenApiSucceeds()
    {
        const string json = """
            {
              "images": [ { "id": "img-1", "passage_id": "JHN.3.16" } ],
              "next_page_token": null
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.SearchImagesAsync("love", 111);

        result.Images.Should().ContainSingle().Which.Id.Should().Be("img-1");
        result.NextPageToken.Should().BeNull();
    }

    [Fact]
    public async Task SearchImagesAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"images": []}""");
        var client = BuildClientFromHandler(handler);

        await client.SearchImagesAsync("love", 111);

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/search-images");
    }

    [Fact]
    public async Task SearchImagesAsync_BuildsExpectedQueryString()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"images": []}""");
        var client = BuildClientFromHandler(handler);

        await client.SearchImagesAsync("love", 111, pageToken: "tok", pageSize: 10);

        var query = handler.LastRequest!.RequestUri!.Query;
        query.Should().Contain("query=love");
        query.Should().Contain("bible_id=111");
        query.Should().Contain("page_token=tok");
        query.Should().Contain("page_size=10");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SearchImagesAsync_ThrowsArgumentException_WhenQueryIsInvalid(string query)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"images": []}""");
        var act = () => client.SearchImagesAsync(query, 111);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SearchImagesAsync_ThrowsArgumentOutOfRangeException_WhenBibleIdIsInvalid()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"images": []}""");
        var act = () => client.SearchImagesAsync("love", 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task SearchImagesAsync_ThrowsArgumentOutOfRangeException_WhenPageSizeIsInvalid()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"images": []}""");
        var act = () => client.SearchImagesAsync("love", 111, pageSize: 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Page size must be greater than zero*");
    }

    [Fact]
    public async Task SearchImagesAsync_DoesNotThrow_WhenPageSizeIsPositive()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"images": []}""");
        var act = () => client.SearchImagesAsync("love", 111, pageSize: 5);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SearchImagesAsync_LogsDebugMessage()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, """{"images": []}""");

        await client.SearchImagesAsync("love", 111);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug
            && e.Message == "Searching images for query 'love' in Bible version 111.");
    }

    [Fact]
    public async Task SearchImagesAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.SearchImagesAsync("love", 111);
        await act.Should().ThrowAsync<BibleEmptyResponseException>().WithMessage("*love*");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SearchImagesAsync_ThrowsBibleApiException_WhenApiReturnsError(HttpStatusCode status)
    {
        var client = BuildClient(status, "error body");
        var act = () => client.SearchImagesAsync("love", 111);
        await act.Should().ThrowAsync<BibleApiException>().Where(e => e.StatusCode == status);
    }

    private static SearchClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static SearchClient BuildClientFromHandler(FakeHttpMessageHandler handler, ILogger<SearchClient>? logger = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new SearchClient(httpClient, logger ?? NullLogger<SearchClient>.Instance);
    }

    private static (SearchClient Client, CapturingLogger<SearchClient> Logger) BuildClientWithLogger(HttpStatusCode status, string json)
    {
        var logger = new CapturingLogger<SearchClient>();
        var client = BuildClientFromHandler(new FakeHttpMessageHandler(status, json), logger);
        return (client, logger);
    }
}
