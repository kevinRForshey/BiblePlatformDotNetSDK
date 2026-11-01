using System.Net;
using System.Net.Http;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Platform.API.Clients;
using Platform.API.Exceptions;
using Platform.API.Tests.Fakes;

using Xunit;

namespace Platform.API.Tests.Clients;

public sealed class VerseOfTheDayClientTests
{
    [Fact]
    public async Task GetVerseOfTheDaysAsync_ReturnsCalendar_WhenApiSucceeds()
    {
        const string json = """
            {
              "data": [
                { "day": 1, "passage_id": "JHN.3.16", "image_id": "img-1" },
                { "day": 2, "passage_id": "PSA.23.1", "image_id": null }
              ]
            }
            """;
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.GetVerseOfTheDaysAsync();

        result.Should().HaveCount(2);
        result[0].PassageId.Should().Be("JHN.3.16");
        result[0].ImageId.Should().Be("img-1");
        result[1].ImageId.Should().BeNull();
    }

    [Fact]
    public async Task GetVerseOfTheDaysAsync_BuildsExpectedQueryString()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetVerseOfTheDaysAsync(languageRanges: ["en", "es"]);

        var query = handler.LastRequest!.RequestUri!.Query;
        query.Should().Contain("language_ranges[]=en");
        query.Should().Contain("language_ranges[]=es");
    }

    [Fact]
    public async Task GetVerseOfTheDaysAsync_OmitsQueryString_WhenNoLanguageRangesGiven()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetVerseOfTheDaysAsync();

        handler.LastRequest!.RequestUri!.Query.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetVerseOfTheDaysAsync_ThrowsBibleApiException_WhenApiReturnsError(HttpStatusCode status)
    {
        var client = BuildClient(status, "error body");
        var act = () => client.GetVerseOfTheDaysAsync();
        await act.Should().ThrowAsync<BibleApiException>().Where(e => e.StatusCode == status);
    }

    [Fact]
    public async Task GetVerseOfTheDayAsync_ReturnsEntry_WhenApiSucceeds()
    {
        const string json = """{ "day": 42, "passage_id": "ROM.8.28", "image_id": "img-42" }""";
        var client = BuildClient(HttpStatusCode.OK, json);

        var result = await client.GetVerseOfTheDayAsync(42);

        result.Day.Should().Be(42);
        result.PassageId.Should().Be("ROM.8.28");
    }

    [Fact]
    public async Task GetVerseOfTheDayAsync_RequestsExpectedPath()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"day": 1, "passage_id": "JHN.3.16"}""");
        var client = BuildClientFromHandler(handler);

        await client.GetVerseOfTheDayAsync(1);

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v1/verse-of-the-days/1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(367)]
    [InlineData(-1)]
    public async Task GetVerseOfTheDayAsync_ThrowsArgumentOutOfRangeException_WhenDayIsInvalid(int day)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"day": 1, "passage_id": "JHN.3.16"}""");
        var act = () => client.GetVerseOfTheDayAsync(day);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetVerseOfTheDayAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        var act = () => client.GetVerseOfTheDayAsync(1);
        await act.Should().ThrowAsync<BibleEmptyResponseException>();
    }

    private static VerseOfTheDayClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static VerseOfTheDayClient BuildClientFromHandler(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new VerseOfTheDayClient(httpClient, NullLogger<VerseOfTheDayClient>.Instance);
    }
}
