using Platform.API.OAuth;

using Xunit;

namespace Platform.API.Tests.OAuth;

public sealed class JwtHelperTests
{
    [Fact]
    public void GetStringClaim_ReturnsValue_WhenTokenHasExactlyTwoParts()
    {
        // header.payload with no signature segment — exactly 2 parts is still a decodable token.
        const string jwt = "header.eyJzdWIiOiAiYWJjMTIzIn0";

        var sub = JwtHelper.GetStringClaim(jwt, "sub");

        Assert.Equal("abc123", sub);
    }

    [Fact]
    public void GetStringClaim_ReturnsNull_WhenTokenHasOnlyOnePart()
    {
        var sub = JwtHelper.GetStringClaim("nodothere", "sub");

        Assert.Null(sub);
    }

    [Fact]
    public void GetStringClaim_ReturnsNull_WhenPayloadIsNotValidBase64()
    {
        var sub = JwtHelper.GetStringClaim("header.!!!not-base64!!!", "sub");

        Assert.Null(sub);
    }

    [Fact]
    public void GetStringClaim_ReturnsNull_WhenTokenIsNull()
    {
        Assert.Null(JwtHelper.GetStringClaim(null, "sub"));
    }

    [Fact]
    public void GetLongClaim_ReturnsValue_WhenClaimIsNumber()
    {
        // payload: {"exp": 1700000000}
        const string jwt = "header.eyJleHAiOiAxNzAwMDAwMDAwfQ";

        var exp = JwtHelper.GetLongClaim(jwt, "exp");

        Assert.Equal(1700000000L, exp);
    }

    [Fact]
    public void GetLongClaim_ReturnsValue_WhenClaimIsNumericString()
    {
        // payload: {"exp": "1700000000"}
        const string jwt = "header.eyJleHAiOiAiMTcwMDAwMDAwMCJ9";

        var exp = JwtHelper.GetLongClaim(jwt, "exp");

        Assert.Equal(1700000000L, exp);
    }

    [Fact]
    public void GetLongClaim_ReturnsNull_WhenClaimIsMissing()
    {
        const string jwt = "header.eyJzdWIiOiAiYWJjMTIzIn0";

        var exp = JwtHelper.GetLongClaim(jwt, "exp");

        Assert.Null(exp);
    }
}
