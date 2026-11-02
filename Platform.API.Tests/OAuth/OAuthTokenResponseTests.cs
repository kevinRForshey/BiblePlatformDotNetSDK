using System;
using System.Linq;
using System.Text;

using FluentAssertions;

using Platform.API.OAuth;

using Xunit;

namespace Platform.API.Tests.OAuth;

public sealed class OAuthTokenResponseTests
{
    // -------------------------------------------------------------------------
    // Default values
    // -------------------------------------------------------------------------

    [Fact]
    public void AccessToken_DefaultsToEmptyString()
    {
        new OAuthTokenResponse().AccessToken.Should().Be(string.Empty);
    }

    [Fact]
    public void TokenType_DefaultsToBearer()
    {
        new OAuthTokenResponse().TokenType.Should().Be("Bearer");
    }

    // -------------------------------------------------------------------------
    // IsExpired
    // -------------------------------------------------------------------------

    [Fact]
    public void IsExpired_ReturnsFalse_WhenTokenIsFresh()
    {
        var token = new OAuthTokenResponse
        {
            AccessToken = "tok", ExpiresIn = 3600,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        token.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ReturnsTrue_WhenTokenHasExpired()
    {
        var token = new OAuthTokenResponse
        {
            AccessToken = "tok", ExpiresIn = 10,
            ReceivedAt = DateTimeOffset.UtcNow.AddSeconds(-100)
        };

        token.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ReturnsTrue_WhenTokenIsWithinBuffer()
    {
        var token = new OAuthTokenResponse
        {
            AccessToken = "tok", ExpiresIn = 3600,
            ReceivedAt = DateTimeOffset.UtcNow.AddSeconds(-(3600 - 30))
        };

        // 60-second default buffer — token has only 30s remaining
        token.IsExpired(bufferSeconds: 60).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ReturnsFalse_WhenExpiresInMissing_ButAccessTokenExists()
    {
        var token = new OAuthTokenResponse
        {
            AccessToken = "opaque-access-token",
            ExpiresIn = 0,
            ReceivedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        token.IsExpired().Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void IsExpired_ReturnsTrue_WhenAccessTokenIsNullOrWhitespace(string? accessToken)
    {
        var token = new OAuthTokenResponse { AccessToken = accessToken! };

        token.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_UsesAccessTokenJwtExpClaim_WhenIdTokenExpClaimMissing()
    {
        var exp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        var token = new OAuthTokenResponse
        {
            AccessToken = BuildUnsignedJwt(("exp", exp)),
            ExpiresIn = 0,
            ReceivedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        token.IsExpired(bufferSeconds: 60).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_PrefersIdTokenJwtExpClaim_WhenAccessTokenExpClaimMissing()
    {
        var exp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        var token = new OAuthTokenResponse
        {
            IdToken = BuildUnsignedJwt(("exp", exp)),
            AccessToken = "opaque-access-token",
            ExpiresIn = 0,
            ReceivedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        token.IsExpired(bufferSeconds: 60).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_PrefersIdTokenJwtExpClaim_OverAccessTokenExpClaim()
    {
        var idTokenExp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString();
        var accessTokenExp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        var token = new OAuthTokenResponse
        {
            IdToken = BuildUnsignedJwt(("exp", idTokenExp)),
            AccessToken = BuildUnsignedJwt(("exp", accessTokenExp)),
            ExpiresIn = 0,
            ReceivedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        // IdToken's exp claim is far in the future, so it must win over AccessToken's expired claim.
        token.IsExpired(bufferSeconds: 60).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ReturnsTrue_WhenJwtExpClaimIsWithinBuffer()
    {
        var exp = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds().ToString();
        var token = new OAuthTokenResponse
        {
            AccessToken = BuildUnsignedJwt(("exp", exp)),
            ExpiresIn = 0,
            ReceivedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        // 60-second default buffer — claim has only 30s remaining
        token.IsExpired(bufferSeconds: 60).Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // GetClaim
    // -------------------------------------------------------------------------

    [Fact]
    public void GetClaim_ReturnsIdTokenClaimValue_WhenPresent()
    {
        var token = new OAuthTokenResponse { IdToken = BuildUnsignedJwt(("custom_claim", "custom-value")) };

        token.GetClaim("custom_claim").Should().Be("custom-value");
    }

    [Fact]
    public void GetClaim_ReturnsNull_WhenIdTokenIsAbsent()
    {
        new OAuthTokenResponse().GetClaim("custom_claim").Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // GetUserName
    // -------------------------------------------------------------------------

    [Fact]
    public void GetUserName_ReturnsIdTokenName_WhenPresent()
    {
        var token = new OAuthTokenResponse
        {
            IdToken = BuildUnsignedJwt(("name", "Alice"), ("preferred_username", "alice123")),
            AccessToken = BuildUnsignedJwt(("name", "Bob"))
        };

        token.GetUserName().Should().Be("Alice");
    }

    [Fact]
    public void GetUserName_FallsBackToIdTokenPreferredUsername_WhenNameMissing()
    {
        var token = new OAuthTokenResponse
        {
            IdToken = BuildUnsignedJwt(("preferred_username", "alice123")),
            AccessToken = BuildUnsignedJwt(("name", "Bob"))
        };

        token.GetUserName().Should().Be("alice123");
    }

    [Fact]
    public void GetUserName_FallsBackToAccessTokenName_WhenIdTokenClaimsMissing()
    {
        var token = new OAuthTokenResponse
        {
            AccessToken = BuildUnsignedJwt(("name", "Bob"), ("preferred_username", "bob123"))
        };

        token.GetUserName().Should().Be("Bob");
    }

    [Fact]
    public void GetUserName_FallsBackToAccessTokenPreferredUsername_WhenAllElseMissing()
    {
        var token = new OAuthTokenResponse
        {
            AccessToken = BuildUnsignedJwt(("preferred_username", "bob123"))
        };

        token.GetUserName().Should().Be("bob123");
    }

    // -------------------------------------------------------------------------
    // GetEmail
    // -------------------------------------------------------------------------

    [Fact]
    public void GetEmail_ReturnsIdTokenEmail_WhenPresent()
    {
        var token = new OAuthTokenResponse
        {
            IdToken = BuildUnsignedJwt(("email", "alice@example.com"), ("upn", "alice@upn.example.com")),
            AccessToken = BuildUnsignedJwt(("email", "bob@example.com"))
        };

        token.GetEmail().Should().Be("alice@example.com");
    }

    [Fact]
    public void GetEmail_FallsBackToIdTokenUpn_WhenEmailMissing()
    {
        var token = new OAuthTokenResponse
        {
            IdToken = BuildUnsignedJwt(("upn", "alice@upn.example.com")),
            AccessToken = BuildUnsignedJwt(("email", "bob@example.com"))
        };

        token.GetEmail().Should().Be("alice@upn.example.com");
    }

    [Fact]
    public void GetEmail_FallsBackToAccessTokenEmail_WhenIdTokenClaimsMissing()
    {
        var token = new OAuthTokenResponse
        {
            AccessToken = BuildUnsignedJwt(("email", "bob@example.com"), ("upn", "bob@upn.example.com"))
        };

        token.GetEmail().Should().Be("bob@example.com");
    }

    [Fact]
    public void GetEmail_FallsBackToAccessTokenUpn_WhenAllElseMissing()
    {
        var token = new OAuthTokenResponse
        {
            AccessToken = BuildUnsignedJwt(("upn", "bob@upn.example.com"))
        };

        token.GetEmail().Should().Be("bob@upn.example.com");
    }

    // -------------------------------------------------------------------------
    // GetDisplayIdentity
    // -------------------------------------------------------------------------

    [Fact]
    public void GetDisplayIdentity_PrefersUserName_OverEmailAndSubject()
    {
        var token = new OAuthTokenResponse
        {
            IdToken = BuildUnsignedJwt(("name", "Alice"), ("email", "alice@example.com"), ("sub", "id-sub")),
            AccessToken = "not-a-jwt"
        };

        token.GetDisplayIdentity().Should().Be("Alice");
    }

    [Fact]
    public void GetDisplayIdentity_ReturnsEmail_WhenNameIsMissing()
    {
        var token = new OAuthTokenResponse
        {
            AccessToken = BuildUnsignedJwt(("email", "kevin@example.com")),
            ExpiresIn = 3600,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        token.GetDisplayIdentity().Should().Be("kevin@example.com");
    }

    [Fact]
    public void GetDisplayIdentity_PrefersEmail_OverSubject()
    {
        var token = new OAuthTokenResponse
        {
            IdToken = BuildUnsignedJwt(("email", "alice@example.com"), ("sub", "id-sub")),
            AccessToken = "not-a-jwt"
        };

        token.GetDisplayIdentity().Should().Be("alice@example.com");
    }

    [Fact]
    public void GetDisplayIdentity_FallsBackToSubject_WhenNameAndEmailMissing()
    {
        var token = new OAuthTokenResponse
        {
            IdToken = BuildUnsignedJwt(("sub", "user-123")),
            AccessToken = "not-a-jwt",
            ExpiresIn = 3600,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        token.GetDisplayIdentity().Should().Be("user-123");
    }

    [Fact]
    public void GetDisplayIdentity_FallsBackToAccessTokenSubject_WhenIdTokenSubjectMissing()
    {
        var token = new OAuthTokenResponse
        {
            AccessToken = BuildUnsignedJwt(("sub", "access-sub"))
        };

        token.GetDisplayIdentity().Should().Be("access-sub");
    }

    [Fact]
    public void GetDisplayIdentity_PrefersIdTokenSubject_OverAccessTokenSubject()
    {
        var token = new OAuthTokenResponse
        {
            IdToken = BuildUnsignedJwt(("sub", "id-sub")),
            AccessToken = BuildUnsignedJwt(("sub", "access-sub"))
        };

        token.GetDisplayIdentity().Should().Be("id-sub");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string BuildUnsignedJwt(params (string Name, string Value)[] claims)
    {
        var payload = "{" + string.Join(",", claims.Select(c => $"\"{c.Name}\":\"{c.Value}\"")) + "}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var payloadBase64Url = Convert.ToBase64String(payloadBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return $"header.{payloadBase64Url}.signature";
    }
}
