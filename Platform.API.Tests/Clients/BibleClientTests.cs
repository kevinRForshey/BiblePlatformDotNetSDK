using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.API.Clients;
using Platform.API.Exceptions;
using Platform.API.Tests.Fakes;
using Xunit;

namespace Platform.API.Tests.Clients;

public sealed class BibleClientTests
{
    // -------------------------------------------------------------------------
    // GetVersionsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetVersionsAsync_ReturnsVersionSummaries_WhenApiSucceeds()
    {
        // Arrange
        const string json = """
            {
              "data": [
                { "id": 3034, "abbreviation": "BSB", "localized_abbreviation": "BSB",
                  "title": "Berean Standard Bible", "localized_title": "Berean Standard Bible",
                  "language_tag": "en" }
              ],
              "next_page_token": null
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        // Act
        var result = await client.GetVersionsAsync("en");

        // Assert
        result.Data.Should().HaveCount(1);
        result.Data[0].Id.Should().Be(3034);
        result.Data[0].Abbreviation.Should().Be("BSB");
        result.NextPageToken.Should().BeNull();
    }

    [Fact]
    public async Task GetVersionsAsync_ReturnsEmptyResult_WhenApiReturnsEmptyData()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data":[],"next_page_token":null}""");
        var result = await client.GetVersionsAsync("en");
        result.Data.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetVersionsAsync_ThrowsArgumentException_WhenLanguageRangeIsInvalid(string languageRange)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data":[],"next_page_token":null}""");
        var act = () => client.GetVersionsAsync(languageRange);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Language range is required*");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task GetVersionsAsync_ThrowsArgumentException_WhenPageTokenIsWhitespace(string pageToken)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data":[],"next_page_token":null}""");
        var act = () => client.GetVersionsAsync("en", pageToken: pageToken);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Page token cannot be empty or whitespace*");
    }

    [Fact]
    public async Task GetVersionsAsync_ThrowsArgumentOutOfRangeException_WhenPageSizeIsNotPositive()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data":[],"next_page_token":null}""");
        var act = () => client.GetVersionsAsync("en", pageSize: 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*Page size must be greater than zero*");
    }

    [Fact]
    public async Task GetVersionsAsync_IncludesPageToken_WhenProvided()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data":[],"next_page_token":null}""");
        var client = BuildClientFromHandler(handler);

        await client.GetVersionsAsync("en", pageToken: "tok123");

        handler.LastRequest!.RequestUri!.Query.Should().Contain("page_token=tok123");
    }

    [Fact]
    public async Task GetVersionsAsync_IncludesPageSize_WhenProvided()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data":[],"next_page_token":null}""");
        var client = BuildClientFromHandler(handler);

        await client.GetVersionsAsync("en", pageSize: 5);

        handler.LastRequest!.RequestUri!.Query.Should().Contain("page_size=5");
    }

    [Fact]
    public async Task GetVersionsAsync_EscapesLanguageRange_InQueryString()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data":[],"next_page_token":null}""");
        var client = BuildClientFromHandler(handler);

        await client.GetVersionsAsync("en-US");

        handler.LastRequest!.RequestUri!.Query.Should().Be("?language_ranges[]=en-US");
    }

    [Fact]
    public async Task GetVersionsAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, """
            {"data": [{"id": 1, "abbreviation": "X", "localized_abbreviation": "X", "title": "X", "localized_title": "X", "language_tag": "en"}], "next_page_token": null}
            """);

        await client.GetVersionsAsync("en");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug
            && e.Message == "Fetching Bible versions for language range 'en' (pageToken=(null)).");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched 1 Bible version(s) from API.");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetVersionsAsync_ThrowsBibleApiException_WhenApiReturnsError(HttpStatusCode statusCode)
    {
        var client = BuildClient(statusCode, """{"error":"fail"}""");
        var act = () => client.GetVersionsAsync("en");
        await act.Should().ThrowAsync<BibleApiException>()
            .Where(e => e.StatusCode == statusCode);
    }

    // -------------------------------------------------------------------------
    // GetVersionAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetVersionAsync_ReturnsVersion_WhenApiSucceeds()
    {
        const string json = """
            {
              "id": 3034, "abbreviation": "BSB", "localized_abbreviation": "BSB",
              "title": "Berean Standard Bible", "localized_title": "Berean Standard Bible",
              "language_tag": "en", "copyright": "Public Domain",
              "books": ["GEN","EXO","REV"]
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        var version = await client.GetVersionAsync(3034);

        version.Id.Should().Be(3034);
        version.Abbreviation.Should().Be("BSB");
        version.Books.Should().Contain("GEN").And.Contain("REV");
        version.Copyright.Should().Be("Public Domain");
    }

    [Fact]
    public async Task GetVersionAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"id": 3034, "abbreviation": "BSB"}""");
        var client = BuildClientFromHandler(handler);

        await client.GetVersionAsync(3034);

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/bibles/3034");
    }

    [Fact]
    public async Task GetVersionAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.GetVersionAsync(3034);
        await act.Should().ThrowAsync<BibleEmptyResponseException>().WithMessage("*3034*");
    }

    [Fact]
    public async Task GetVersionAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, """{"id": 3034, "abbreviation": "BSB"}""");

        await client.GetVersionAsync(3034);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching Bible version 3034.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched Bible version 3034 (BSB).");
    }

    [Fact]
    public async Task GetVersionAsync_ThrowsBibleApiException_WhenNotFound()
    {
        var client = BuildClient(HttpStatusCode.NotFound, """{"error":"not found"}""");
        var act = () => client.GetVersionAsync(9999);
        await act.Should().ThrowAsync<BibleApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVersionAsync_ThrowsArgumentOutOfRangeException_WhenVersionIdIsNotPositive()
    {
        var client = BuildClient(HttpStatusCode.OK, "{}");
        var act = () => client.GetVersionAsync(0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*Version id must be greater than zero*");
    }

    // -------------------------------------------------------------------------
    // GetIndexAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetIndexAsync_ReturnsIndex_WhenApiSucceeds()
    {
        var client = BuildClient(HttpStatusCode.OK, SampleIndexJson);

        var index = await client.GetIndexAsync(3034);

        index.TextDirection.Should().Be("ltr");
        index.Books.Should().HaveCount(2);
        index.Books[0].Usfm.Should().Be("GEN");
        index.Books[0].Title.Should().Be("Genesis");
        index.Books[0].Canon.Should().Be(Platform.API.Models.BookCanon.OldTestament);
    }

    [Fact]
    public async Task GetIndexAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SampleIndexJson);
        var client = BuildClientFromHandler(handler);

        await client.GetIndexAsync(3034);

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/bibles/3034/index");
    }

    [Fact]
    public async Task GetIndexAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.GetIndexAsync(3034);
        await act.Should().ThrowAsync<BibleEmptyResponseException>().WithMessage("*3034*");
    }

    [Fact]
    public async Task GetIndexAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, SampleIndexJson);

        await client.GetIndexAsync(3034);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching index for Bible version 3034.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched index for Bible version 3034 with 2 book(s).");
    }

    [Fact]
    public async Task GetIndexAsync_ThrowsBibleApiException_WhenNotFound()
    {
        var client = BuildClient(HttpStatusCode.NotFound, """{"error":"not found"}""");
        var act = () => client.GetIndexAsync(9999);
        await act.Should().ThrowAsync<BibleApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetIndexAsync_ThrowsArgumentOutOfRangeException_WhenVersionIdIsNotPositive()
    {
        var client = BuildClient(HttpStatusCode.OK, "{}");
        var act = () => client.GetIndexAsync(0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*Version id must be greater than zero*");
    }

    // -------------------------------------------------------------------------
    // GetBooksAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetBooksAsync_ReturnsBooksFromIndex_WithRealChapterCounts()
    {
        var client = BuildClient(HttpStatusCode.OK, SampleIndexJson);

        var books = await client.GetBooksAsync(3034);

        books.Should().HaveCount(2);
        books[0].Usfm.Should().Be("GEN");
        books[0].Human.Should().Be("Genesis");
        books[0].ChapterCount.Should().Be(2);
        books[1].Usfm.Should().Be("EXO");
        books[1].ChapterCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBooksAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, SampleIndexJson);

        await client.GetBooksAsync(3034);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetching books for Bible version 3034.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug && e.Message == "Fetched 2 book(s) for Bible version 3034.");
    }

    [Fact]
    public async Task GetBooksAsync_ThrowsArgumentOutOfRangeException_WhenVersionIdIsNotPositive()
    {
        var client = BuildClient(HttpStatusCode.OK, "{}");
        var act = () => client.GetBooksAsync(0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // -------------------------------------------------------------------------
    // GetChaptersAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetChaptersAsync_ReturnsChaptersForBook_WithRealVerseCounts()
    {
        var client = BuildClient(HttpStatusCode.OK, SampleIndexJson);

        var chapters = await client.GetChaptersAsync(3034, "GEN");

        chapters.Should().HaveCount(2);
        chapters[0].Usfm.Should().Be("GEN.1");
        chapters[0].Human.Should().Be("Genesis 1");
        chapters[0].VerseCount.Should().Be(2);
        chapters[1].Usfm.Should().Be("GEN.2");
        chapters[1].Human.Should().Be("Genesis 2");
        chapters[1].VerseCount.Should().Be(1);
    }

    [Fact]
    public async Task GetChaptersAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, SampleIndexJson);

        await client.GetChaptersAsync(3034, "GEN");

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug
            && e.Message == "Fetching chapters for book GEN in Bible version 3034.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug
            && e.Message == "Fetched 2 chapter(s) for book GEN in Bible version 3034.");
    }

    [Fact]
    public async Task GetChaptersAsync_IsCaseInsensitive_ForBookUsfm()
    {
        var client = BuildClient(HttpStatusCode.OK, SampleIndexJson);

        var chapters = await client.GetChaptersAsync(3034, "gen");

        chapters.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetChaptersAsync_ThrowsBibleApiException_WhenBookNotInIndex()
    {
        var client = BuildClient(HttpStatusCode.OK, SampleIndexJson);
        var act = () => client.GetChaptersAsync(3034, "REV");
        await act.Should().ThrowAsync<BibleApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.NotFound)
            .WithMessage("*Book 'REV' was not found in Bible version 3034*");
    }

    [Fact]
    public async Task GetChaptersAsync_ThrowsArgumentOutOfRangeException_WhenVersionIdIsNotPositive()
    {
        var client = BuildClient(HttpStatusCode.OK, "{}");
        var act = () => client.GetChaptersAsync(0, "GEN");
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetChaptersAsync_ThrowsArgumentException_WhenBookUsfmIsInvalid(string bookUsfm)
    {
        var client = BuildClient(HttpStatusCode.OK, "{}");
        var act = () => client.GetChaptersAsync(3034, bookUsfm);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Book USFM code is required*");
    }

    // -------------------------------------------------------------------------
    // GetVersesAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetVersesAsync_ReturnsVersesForChapter()
    {
        var client = BuildClient(HttpStatusCode.OK, SampleIndexJson);

        var verses = await client.GetVersesAsync(3034, "GEN", 1);

        verses.Should().HaveCount(2);
        verses[0].Usfm.Should().Be("GEN.1.1");
        verses[0].Human.Should().Be("Genesis 1:1");
        verses[0].Text.Should().BeEmpty();
        verses[1].Usfm.Should().Be("GEN.1.2");
    }

    [Fact]
    public async Task GetVersesAsync_ThrowsBibleApiException_WhenChapterNotInBook()
    {
        var client = BuildClient(HttpStatusCode.OK, SampleIndexJson);
        var act = () => client.GetVersesAsync(3034, "GEN", 99);
        await act.Should().ThrowAsync<BibleApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.NotFound)
            .WithMessage("*Chapter 99 was not found in book 'GEN' of Bible version 3034*");
    }

    [Fact]
    public async Task GetVersesAsync_ThrowsBibleApiException_WhenBookNotInIndex()
    {
        var client = BuildClient(HttpStatusCode.OK, SampleIndexJson);
        var act = () => client.GetVersesAsync(3034, "REV", 1);
        await act.Should().ThrowAsync<BibleApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.NotFound)
            .WithMessage("*Book 'REV' was not found in Bible version 3034*");
    }

    [Fact]
    public async Task GetVersesAsync_ThrowsArgumentOutOfRangeException_WhenVersionIdIsNotPositive()
    {
        var client = BuildClient(HttpStatusCode.OK, "{}");
        var act = () => client.GetVersesAsync(0, "GEN", 1);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetVersesAsync_ThrowsArgumentException_WhenBookUsfmIsEmpty()
    {
        var client = BuildClient(HttpStatusCode.OK, "{}");
        var act = () => client.GetVersesAsync(3034, "", 1);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Book USFM code is required*");
    }

    [Fact]
    public async Task GetVersesAsync_ThrowsArgumentOutOfRangeException_WhenChapterNumberIsNotPositive()
    {
        var client = BuildClient(HttpStatusCode.OK, "{}");
        var act = () => client.GetVersesAsync(3034, "GEN", 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*Chapter number must be greater than zero*");
    }

    [Fact]
    public async Task GetVersesAsync_LogsDebugMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, SampleIndexJson);

        await client.GetVersesAsync(3034, "GEN", 1);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug
            && e.Message == "Fetching verses for GEN 1 in Bible version 3034.");
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Debug
            && e.Message == "Fetched 2 verse(s) for GEN 1 in Bible version 3034.");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

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

    private static BibleClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static BibleClient BuildClientFromHandler(FakeHttpMessageHandler handler, ILogger<BibleClient>? logger = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new BibleClient(httpClient, logger ?? NullLogger<BibleClient>.Instance);
    }

    private static (BibleClient Client, CapturingLogger<BibleClient> Logger) BuildClientWithLogger(HttpStatusCode status, string json)
    {
        var logger = new CapturingLogger<BibleClient>();
        var client = BuildClientFromHandler(new FakeHttpMessageHandler(status, json), logger);
        return (client, logger);
    }
}
