using System.Net;
using System.Net.Http;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Platform.API.Clients;
using Platform.API.Exceptions;
using Platform.API.Tests.Fakes;

using Xunit;

namespace Platform.API.Tests.Clients;

public sealed class LicenseClientTests
{
    private const string LicenseJson = """
        {
          "id": 1,
          "name": "Standard License",
          "version": 2,
          "organization_id": "00000000-0000-0000-0000-000000000000",
          "html": "<p>License text</p>",
          "bible_ids": [3034],
          "content_types": { "bible": 68 },
          "uri": "https://www.example.com/license",
          "agreed_dt": "2024-08-25T15:00:00Z",
          "yvp_user_id": "11111111-1111-1111-1111-111111111111"
        }
        """;

    [Fact]
    public async Task GetLicensesAsync_ReturnsLicenses_WhenApiSucceeds()
    {
        var client = BuildClient(HttpStatusCode.OK, $$"""{"data": [{{LicenseJson}}]}""");

        var result = await client.GetLicensesAsync(3034);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Standard License");
        result[0].BibleIds.Should().Contain(3034);
        result[0].ContentTypes["bible"].Should().Be(68);
        result[0].YvpUserId.Should().Be("11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public async Task GetLicensesAsync_BuildsExpectedQueryString()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": []}""");
        var client = BuildClientFromHandler(handler);

        await client.GetLicensesAsync(3034, developerId: "dev-1", allAvailable: true, contentType: "bible");

        var query = handler.LastRequest!.RequestUri!.Query;
        query.Should().Contain("bible_id=3034");
        query.Should().Contain("developer_id=dev-1");
        query.Should().Contain("all_available=true");
        query.Should().Contain("content_type=bible");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLicensesAsync_ThrowsArgumentOutOfRangeException_WhenBibleIdIsInvalid(int bibleId)
    {
        var client = BuildClient(HttpStatusCode.OK, """{"data": []}""");
        var act = () => client.GetLicensesAsync(bibleId);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetLicensesAsync_ThrowsBibleApiException_WhenApiReturnsError(HttpStatusCode status)
    {
        var client = BuildClient(status, "error body");
        var act = () => client.GetLicensesAsync(3034);
        await act.Should().ThrowAsync<BibleApiException>().Where(e => e.StatusCode == status);
    }

    private static LicenseClient BuildClient(HttpStatusCode status, string json)
        => BuildClientFromHandler(new FakeHttpMessageHandler(status, json));

    private static LicenseClient BuildClientFromHandler(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.youversion.com") };
        return new LicenseClient(httpClient, NullLogger<LicenseClient>.Instance);
    }
}
