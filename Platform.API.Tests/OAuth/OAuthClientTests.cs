using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Platform.API.Configuration;
using Platform.API.Exceptions;
using Platform.API.OAuth;
using Platform.API.Tests.Fakes;
using Xunit;

namespace Platform.API.Tests.OAuth;

public sealed class OAuthClientTests
{
    private const string TokenJson = """
        { "access_token": "acc-tok", "refresh_token": "ref-tok",
          "token_type": "Bearer", "expires_in": 3600 }
        """;

    // -------------------------------------------------------------------------
    // ValidateState
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateState_ReturnsTrue_WhenValuesMatch()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        client.ValidateState("csrf-token-abc", "csrf-token-abc").Should().BeTrue();
    }

    [Fact]
    public void ValidateState_ReturnsFalse_WhenValuesDiffer()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        client.ValidateState("csrf-token-abc", "csrf-token-xyz").Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "state")]
    [InlineData("state", null)]
    [InlineData("", "state")]
    [InlineData("state", "")]
    [InlineData(null, null)]
    public void ValidateState_ReturnsFalse_WhenEitherValueIsMissing(string? expected, string? actual)
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        client.ValidateState(expected, actual).Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // BuildAuthorizationUrl
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildAuthorizationUrl_ReturnsUri_WithExpectedQueryParameters()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var authRequest = client.BuildAuthorizationUrl();

        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain("response_type=code");
        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain("client_id=test-client");
        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain("code_challenge_method%3DS256"   // encoded
            .Replace("%3D", "="));
        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain("redirect_uri=");
    }

    [Fact]
    public void BuildAuthorizationUrl_IncludesEscapedRedirectUriValue_InQueryString()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var authRequest = client.BuildAuthorizationUrl();

        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain(
            "redirect_uri=" + Uri.EscapeDataString("https://localhost/callback"));
    }

    [Fact]
    public void BuildAuthorizationUrl_IncludesNonEmptyNonceValue_InQueryString()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var authRequest = client.BuildAuthorizationUrl();

        var match = Regex.Match(authRequest.AuthorizationUrl.AbsoluteUri, "nonce=([^&]*)");
        match.Success.Should().BeTrue();
        match.Groups[1].Value.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildAuthorizationUrl_IncludesEscapedCodeChallengeValue_InQueryString()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var authRequest = client.BuildAuthorizationUrl();

        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain(
            "code_challenge=" + Uri.EscapeDataString(authRequest.Pkce.CodeChallenge));
    }

    [Fact]
    public void BuildAuthorizationUrl_GeneratesPkce_WithNonEmptyVerifierAndChallenge()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var authRequest = client.BuildAuthorizationUrl();

        authRequest.Pkce.CodeVerifier.Should().NotBeNullOrEmpty();
        authRequest.Pkce.CodeChallenge.Should().NotBeNullOrEmpty();
        authRequest.Pkce.CodeChallengeMethod.Should().Be("S256");
    }

    [Fact]
    public void BuildAuthorizationUrl_EachCallProducesUniquePkce()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var req1 = client.BuildAuthorizationUrl();
        var req2 = client.BuildAuthorizationUrl();

        req1.Pkce.CodeVerifier.Should().NotBe(req2.Pkce.CodeVerifier);
        req1.Pkce.CodeChallenge.Should().NotBe(req2.Pkce.CodeChallenge);
    }

    [Fact]
    public void BuildAuthorizationUrl_UsesProvidedState_InQueryString()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var authRequest = client.BuildAuthorizationUrl(state: "csrf-token-123");

        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain("state=csrf-token-123");
    }

    [Fact]
    public void BuildAuthorizationUrl_GeneratesState_WhenNotProvided()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var authRequest = client.BuildAuthorizationUrl();

        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain("state=");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void BuildAuthorizationUrl_ThrowsArgumentException_WhenProvidedStateIsInvalid(string state)
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);
        var act = () => client.BuildAuthorizationUrl(state);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildAuthorizationUrl_OmitsRedirectUri_WhenNotConfigured()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson, configureOptions: o => o.RedirectUri = null);

        var authRequest = client.BuildAuthorizationUrl();

        authRequest.AuthorizationUrl.AbsoluteUri.Should().NotContain("redirect_uri=");
    }

    [Fact]
    public void BuildAuthorizationUrl_AddsOpenIdScope_WhenConfiguredScopesOmitIt()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson, configureOptions: o => o.Scopes = "profile");

        var authRequest = client.BuildAuthorizationUrl();

        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain("scope=profile%20openid");
    }

    [Fact]
    public void BuildAuthorizationUrl_AddsOpenIdScope_WithoutLeadingSeparator_WhenConfiguredScopesAreEmpty()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson, configureOptions: o => o.Scopes = "");

        var authRequest = client.BuildAuthorizationUrl();

        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain("scope=openid");
        authRequest.AuthorizationUrl.AbsoluteUri.Should().NotContain("scope=%20openid");
    }

    [Fact]
    public void BuildAuthorizationUrl_DoesNotDuplicateOpenIdScope_WhenAlreadyPresent()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson, configureOptions: o => o.Scopes = "openid profile");

        var authRequest = client.BuildAuthorizationUrl();

        var scopeCount = Regex.Matches(authRequest.AuthorizationUrl.AbsoluteUri, "openid").Count;
        scopeCount.Should().Be(1);
    }

    [Fact]
    public void BuildAuthorizationUrl_OmitsRequestedPermissions_WhenNotProvided()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var authRequest = client.BuildAuthorizationUrl();

        authRequest.AuthorizationUrl.AbsoluteUri.Should().NotContain("requested_permissions");
    }

    // Bracket form (`requested_permissions[]=`) is required — the platform silently ignores a
    // bare `requested_permissions=` and never shows the consent UI. See
    // specs/0001-oauth-signin-and-data-exchange.md for the verified request/response shapes.
    [Fact]
    public void BuildAuthorizationUrl_AppendsRequestedPermissions_WhenProvided()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var authRequest = client.BuildAuthorizationUrl(requestedPermissions: ["highlights"]);

        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain("requested_permissions[]=highlights");
    }

    [Fact]
    public void BuildAuthorizationUrl_AppendsEachRequestedPermission_AsRepeatedQueryParameter()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var authRequest = client.BuildAuthorizationUrl(requestedPermissions: ["highlights", "other"]);

        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain("requested_permissions[]=highlights");
        authRequest.AuthorizationUrl.AbsoluteUri.Should().Contain("requested_permissions[]=other");
    }

    // -------------------------------------------------------------------------
    // ExchangeCodeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExchangeCodeAsync_ReturnsToken_WhenApiSucceeds()
    {
        var tokenProvider = new FakeTokenProvider();
        var client = BuildClient(HttpStatusCode.OK, TokenJson, tokenProvider);

        var token = await client.ExchangeCodeAsync("auth-code", "verifier");

        token.AccessToken.Should().Be("acc-tok");
        token.RefreshToken.Should().Be("ref-tok");
        token.ExpiresIn.Should().Be(3600);
    }

    [Fact]
    public async Task ExchangeCodeAsync_StoresTokenInProvider_AfterSuccess()
    {
        var tokenProvider = new FakeTokenProvider();
        var client = BuildClient(HttpStatusCode.OK, TokenJson, tokenProvider);

        await client.ExchangeCodeAsync("auth-code", "verifier");

        var stored = await tokenProvider.GetTokenAsync();
        stored.Should().NotBeNull();
        stored!.AccessToken.Should().Be("acc-tok");
    }

    [Fact]
    public async Task ExchangeCodeAsync_ThrowsBibleApiException_WhenApiReturnsError()
    {
        var client = BuildClient(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");

        var act = () => client.ExchangeCodeAsync("bad-code", "verifier");

        await act.Should().ThrowAsync<BibleApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ExchangeCodeAsync_ThrowsArgumentException_WhenCodeIsInvalid(string code)
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);
        var act = () => client.ExchangeCodeAsync(code, "verifier");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ExchangeCodeAsync_ThrowsArgumentException_WhenCodeVerifierIsInvalid(string codeVerifier)
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);
        var act = () => client.ExchangeCodeAsync("auth-code", codeVerifier);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExchangeCodeAsync_IncludesRedirectUri_WhenConfigured()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, TokenJson);
        var client = BuildClientFromHandler(handler, configureOptions: o => o.RedirectUri = new Uri("https://localhost/callback"));

        await client.ExchangeCodeAsync("auth-code", "verifier");

        handler.LastRequestBody.Should().Contain("redirect_uri");
    }

    [Fact]
    public async Task ExchangeCodeAsync_OmitsRedirectUri_WhenNotConfigured()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, TokenJson);
        var client = BuildClientFromHandler(handler, configureOptions: o => o.RedirectUri = null);

        await client.ExchangeCodeAsync("auth-code", "verifier");

        handler.LastRequestBody.Should().NotContain("redirect_uri");
    }

    // -------------------------------------------------------------------------
    // CompleteAuthorizationCallbackAsync (state-only redirect, per the platform's 2026-07-20 change)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_ReturnsToken_OnTwoHopSuccess()
    {
        var stateRedirect = new HttpResponseMessage(HttpStatusCode.Found);
        stateRedirect.Headers.Location = new Uri("https://auth.youversion.com/auth/callback?code=auth-code-xyz");
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
        };
        var handler = new SequencedHttpMessageHandler(stateRedirect, tokenResponse);
        var tokenProvider = new FakeTokenProvider();
        var client = BuildClientFromSequencedHandler(handler, tokenProvider);

        var result = await client.CompleteAuthorizationCallbackAsync("state-abc", "verifier");

        result.Token.AccessToken.Should().Be("acc-tok");
        result.Token.RefreshToken.Should().Be("ref-tok");
        result.Token.ExpiresIn.Should().Be(3600);
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_StoresToken_IncludingReceivedAt_OnTwoHopSuccess()
    {
        var stateRedirect = new HttpResponseMessage(HttpStatusCode.Found);
        stateRedirect.Headers.Location = new Uri("https://auth.youversion.com/auth/callback?code=auth-code-xyz");
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
        };
        var handler = new SequencedHttpMessageHandler(stateRedirect, tokenResponse);
        var tokenProvider = new FakeTokenProvider();
        var client = BuildClientFromSequencedHandler(handler, tokenProvider);

        var before = DateTimeOffset.UtcNow;
        await client.CompleteAuthorizationCallbackAsync("state-abc", "verifier");
        var after = DateTimeOffset.UtcNow;

        var stored = await tokenProvider.GetTokenAsync();
        stored.Should().NotBeNull();
        stored!.AccessToken.Should().Be("acc-tok");
        stored.ReceivedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_SendsOnlyState_OnFirstHop()
    {
        var stateRedirect = new HttpResponseMessage(HttpStatusCode.Found);
        stateRedirect.Headers.Location = new Uri("https://auth.youversion.com/auth/callback?code=auth-code-xyz");
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
        };
        var handler = new SequencedHttpMessageHandler(stateRedirect, tokenResponse);
        var client = BuildClientFromSequencedHandler(handler);

        await client.CompleteAuthorizationCallbackAsync("state-abc", "verifier");

        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri!.Query.Should().Be("?state=state-abc");
        handler.Requests[1].RequestUri!.Should().Be(new Uri("https://auth.youversion.com/oauth2/token"));
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_ReturnsGrantedPermissions_WhenPresentOnRedirect()
    {
        var stateRedirect = new HttpResponseMessage(HttpStatusCode.Found);
        stateRedirect.Headers.Location = new Uri(
            "https://auth.youversion.com/auth/callback?code=auth-code-xyz&granted_permissions=highlights");
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
        };
        var handler = new SequencedHttpMessageHandler(stateRedirect, tokenResponse);
        var client = BuildClientFromSequencedHandler(handler);

        var result = await client.CompleteAuthorizationCallbackAsync("state-abc", "verifier");

        result.GrantedPermissions.Should().NotBeNull();
        result.GrantedPermissions.Should().ContainSingle().Which.Should().Be("highlights");
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_ReturnsNullGrantedPermissions_WhenAbsentFromRedirect()
    {
        var stateRedirect = new HttpResponseMessage(HttpStatusCode.Found);
        stateRedirect.Headers.Location = new Uri("https://auth.youversion.com/auth/callback?code=auth-code-xyz");
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
        };
        var handler = new SequencedHttpMessageHandler(stateRedirect, tokenResponse);
        var client = BuildClientFromSequencedHandler(handler);

        var result = await client.CompleteAuthorizationCallbackAsync("state-abc", "verifier");

        result.GrantedPermissions.Should().BeNull();
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_ThrowsBibleApiException_WhenNoLocationHeaderReturned()
    {
        var noLocationResponse = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream error", Encoding.UTF8, "text/plain")
        };
        var handler = new SequencedHttpMessageHandler(noLocationResponse);
        var client = BuildClientFromSequencedHandler(handler);

        var act = () => client.CompleteAuthorizationCallbackAsync("state-abc", "verifier");

        await act.Should().ThrowAsync<BibleApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_ThrowsBibleEmptyResponseException_WhenCodeMissingFromLocation()
    {
        var stateRedirect = new HttpResponseMessage(HttpStatusCode.Found);
        stateRedirect.Headers.Location = new Uri("https://auth.youversion.com/auth/callback?other=value");
        var handler = new SequencedHttpMessageHandler(stateRedirect);
        var client = BuildClientFromSequencedHandler(handler);

        var act = () => client.CompleteAuthorizationCallbackAsync("state-abc", "verifier");

        await act.Should().ThrowAsync<BibleEmptyResponseException>();
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_ThrowsBibleEmptyResponseException_WhenCodeIsBlankInLocation()
    {
        var stateRedirect = new HttpResponseMessage(HttpStatusCode.Found);
        stateRedirect.Headers.Location = new Uri("https://auth.youversion.com/auth/callback?code=");
        var handler = new SequencedHttpMessageHandler(stateRedirect);
        var client = BuildClientFromSequencedHandler(handler);

        var act = () => client.CompleteAuthorizationCallbackAsync("state-abc", "verifier");

        await act.Should().ThrowAsync<BibleEmptyResponseException>();
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_ResolvesRelativeLocationHeader_AgainstRequestUri()
    {
        var stateRedirect = new HttpResponseMessage(HttpStatusCode.Found);
        stateRedirect.Headers.Location = new Uri("/auth/callback?code=auth-code-xyz", UriKind.Relative);
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
        };
        var handler = new SequencedHttpMessageHandler(stateRedirect, tokenResponse);
        var client = BuildClientFromSequencedHandler(handler);

        var result = await client.CompleteAuthorizationCallbackAsync("state-abc", "verifier");

        result.Token.AccessToken.Should().Be("acc-tok");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CompleteAuthorizationCallbackAsync_ThrowsArgumentException_WhenStateIsInvalid(string state)
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);
        var act = () => client.CompleteAuthorizationCallbackAsync(state, "verifier");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CompleteAuthorizationCallbackAsync_ThrowsArgumentException_WhenCodeVerifierIsInvalid(string codeVerifier)
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);
        var act = () => client.CompleteAuthorizationCallbackAsync("state-abc", codeVerifier);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // -------------------------------------------------------------------------
    // RefreshTokenAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshTokenAsync_ReturnsNewToken_WhenRefreshTokenExists()
    {
        const string newTokenJson = """
            { "access_token": "new-acc", "refresh_token": "new-ref",
              "token_type": "Bearer", "expires_in": 3600 }
            """;

        var existing = new OAuthTokenResponse
        {
            AccessToken = "old-acc", RefreshToken = "ref-tok",
            ExpiresIn = 3600, ReceivedAt = DateTimeOffset.UtcNow
        };
        var tokenProvider = new FakeTokenProvider(existing);
        var client = BuildClient(HttpStatusCode.OK, newTokenJson, tokenProvider);

        var token = await client.RefreshTokenAsync();

        token.AccessToken.Should().Be("new-acc");
        token.RefreshToken.Should().Be("new-ref");
    }

    [Fact]
    public async Task RefreshTokenAsync_UpdatesStoredToken_AfterRefresh()
    {
        const string newTokenJson = """
            { "access_token": "new-acc", "refresh_token": "new-ref",
              "token_type": "Bearer", "expires_in": 3600 }
            """;

        var existing = new OAuthTokenResponse
        {
            AccessToken = "old-acc", RefreshToken = "ref-tok",
            ExpiresIn = 3600, ReceivedAt = DateTimeOffset.UtcNow
        };
        var tokenProvider = new FakeTokenProvider(existing);
        var client = BuildClient(HttpStatusCode.OK, newTokenJson, tokenProvider);

        await client.RefreshTokenAsync();

        var stored = await tokenProvider.GetTokenAsync();
        stored!.AccessToken.Should().Be("new-acc");
    }

    [Fact]
    public async Task RefreshTokenAsync_ThrowsInvalidOperationException_WhenNoTokenStored()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson, new FakeTokenProvider(initial: null));

        var act = () => client.RefreshTokenAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sign in again*");
    }

    [Fact]
    public async Task RefreshTokenAsync_ThrowsInvalidOperationException_WhenRefreshTokenIsNull()
    {
        var existing = new OAuthTokenResponse
        {
            AccessToken = "acc", RefreshToken = null,
            ExpiresIn = 3600, ReceivedAt = DateTimeOffset.UtcNow
        };
        var client = BuildClient(HttpStatusCode.OK, TokenJson, new FakeTokenProvider(existing));

        var act = () => client.RefreshTokenAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // SignOutAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SignOutAsync_ClearsStoredToken()
    {
        var existing = new OAuthTokenResponse
        {
            AccessToken = "acc", RefreshToken = "ref",
            ExpiresIn = 3600, ReceivedAt = DateTimeOffset.UtcNow
        };
        var tokenProvider = new FakeTokenProvider(existing);
        var client = BuildClient(HttpStatusCode.OK, TokenJson, tokenProvider);

        await client.SignOutAsync();

        var stored = await tokenProvider.GetTokenAsync();
        stored.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // RequestPermissionsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RequestPermissionsAsync_ReturnsDataExchangeToken_WhenApiSucceeds()
    {
        const string responseJson = """{ "token": "dx-tok", "token_type": "data_exchange", "expires_in": 300 }""";
        var tokenProvider = new FakeTokenProvider(MakeStoredToken());
        var client = BuildClient(HttpStatusCode.Created, responseJson, tokenProvider);

        var result = await client.RequestPermissionsAsync(["highlights"]);

        result.Token.Should().Be("dx-tok");
        result.TokenType.Should().Be("data_exchange");
        result.ExpiresIn.Should().Be(300);
    }

    [Fact]
    public async Task RequestPermissionsAsync_SendsStoredAccessTokenAsBearerAndPermissionsInBody()
    {
        const string responseJson = """{ "token": "dx-tok", "token_type": "data_exchange", "expires_in": 300 }""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Created, responseJson);
        var tokenProvider = new FakeTokenProvider(MakeStoredToken());
        var client = BuildClientFromHandler(handler, tokenProvider);

        await client.RequestPermissionsAsync(["highlights"]);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization!.Parameter.Should().Be("acc-tok");
        handler.LastRequest.RequestUri!.Query.Should().Contain("x-yvp-app-key=test-app-key");
        handler.LastRequestBody.Should().Contain("\"requested_permissions\"").And.Contain("highlights");
    }

    [Fact]
    public async Task RequestPermissionsAsync_ThrowsInvalidOperationException_WhenNoTokenStored()
    {
        const string responseJson = """{ "token": "dx-tok", "token_type": "data_exchange", "expires_in": 300 }""";
        var client = BuildClient(HttpStatusCode.Created, responseJson, new FakeTokenProvider(initial: null));

        var act = () => client.RequestPermissionsAsync(["highlights"]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sign in*");
    }

    [Fact]
    public async Task RequestPermissionsAsync_ThrowsArgumentException_WhenPermissionsIsEmpty()
    {
        var tokenProvider = new FakeTokenProvider(MakeStoredToken());
        var client = BuildClient(HttpStatusCode.Created, "{}", tokenProvider);

        var act = () => client.RequestPermissionsAsync([]);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RequestPermissionsAsync_ThrowsBibleApiException_OnError()
    {
        var tokenProvider = new FakeTokenProvider(MakeStoredToken());
        var client = BuildClient(HttpStatusCode.Unauthorized, """{"error":"invalid_token"}""", tokenProvider);

        var act = () => client.RequestPermissionsAsync(["highlights"]);

        await act.Should().ThrowAsync<BibleApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // BuildDataExchangeApprovalUrl
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildDataExchangeApprovalUrl_ReturnsUrlWithTokenAndAppKeyQueryParameters()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var url = client.BuildDataExchangeApprovalUrl("dx-tok");

        url.AbsoluteUri.Should().Be("https://api.youversion.com/data-exchange?token=dx-tok&x-yvp-app-key=test-app-key");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void BuildDataExchangeApprovalUrl_ThrowsArgumentException_WhenTokenIsInvalid(string token)
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var act = () => client.BuildDataExchangeApprovalUrl(token);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildDataExchangeApprovalUrl_ThrowsInvalidOperationException_WhenAppKeyNotConfigured()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson, appKey: "");

        var act = () => client.BuildDataExchangeApprovalUrl("dx-tok");

        act.Should().Throw<InvalidOperationException>().WithMessage("*AppKey*");
    }

    // -------------------------------------------------------------------------
    // BuildDirectDataExchangeUrl
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildDirectDataExchangeUrl_ReturnsUrlWithAppKeyAndRequestedPermission()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var url = client.BuildDirectDataExchangeUrl(["highlights"]);

        url.AbsoluteUri.Should().Be(
            "https://api.youversion.com/data-exchange?x-yvp-app-key=test-app-key&requested_permissions[]=highlights");
    }

    [Fact]
    public void BuildDirectDataExchangeUrl_AppendsEachPermission_AsRepeatedQueryParameter_AndSkipsWhitespaceEntries()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var url = client.BuildDirectDataExchangeUrl(["highlights", " ", "bookmarks"]);

        url.AbsoluteUri.Should().Be(
            "https://api.youversion.com/data-exchange?x-yvp-app-key=test-app-key" +
            "&requested_permissions[]=highlights&requested_permissions[]=bookmarks");
    }

    [Fact]
    public void BuildDirectDataExchangeUrl_AppendsState_WhenProvided()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var url = client.BuildDirectDataExchangeUrl(["highlights"], state: "csrf-abc");

        url.AbsoluteUri.Should().EndWith("&state=csrf-abc");
    }

    [Fact]
    public void BuildDirectDataExchangeUrl_OmitsState_WhenNotProvided()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var url = client.BuildDirectDataExchangeUrl(["highlights"]);

        url.AbsoluteUri.Should().NotContain("state=");
    }

    [Fact]
    public void BuildDirectDataExchangeUrl_ThrowsArgumentNullException_WhenPermissionsIsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var act = () => client.BuildDirectDataExchangeUrl(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildDirectDataExchangeUrl_ThrowsArgumentException_WhenPermissionsIsEmpty()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var act = () => client.BuildDirectDataExchangeUrl([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildDirectDataExchangeUrl_ThrowsInvalidOperationException_WhenAppKeyNotConfigured()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson, appKey: "");

        var act = () => client.BuildDirectDataExchangeUrl(["highlights"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*AppKey*");
    }

    // -------------------------------------------------------------------------
    // ParseDataExchangeCallback
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseDataExchangeCallback_ReturnsGranted_WithPermissions()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var result = client.ParseDataExchangeCallback(
            new Uri("https://myapp.com/?data_exchange_status=granted&granted_permissions=highlights"));

        result.Status.Should().Be(DataExchangeStatus.Granted);
        result.GrantedPermissions.Should().ContainSingle().Which.Should().Be("highlights");
        result.DeniedPermissions.Should().BeEmpty();
    }

    [Fact]
    public void ParseDataExchangeCallback_ReturnsCancelled_WithDeniedPermissionsAndError()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var result = client.ParseDataExchangeCallback(new Uri(
            "https://myapp.com/?data_exchange_status=cancelled&denied_permissions=highlights&error=access_denied"));

        result.Status.Should().Be(DataExchangeStatus.Cancelled);
        result.DeniedPermissions.Should().ContainSingle().Which.Should().Be("highlights");
        result.Error.Should().Be("access_denied");
    }

    [Fact]
    public void ParseDataExchangeCallback_ReturnsError_WithErrorDescription()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var result = client.ParseDataExchangeCallback(new Uri(
            "https://myapp.com/?data_exchange_status=error&error=server_error&error_description=Something%20went%20wrong"));

        result.Status.Should().Be(DataExchangeStatus.Error);
        result.Error.Should().Be("server_error");
        result.ErrorDescription.Should().Be("Something went wrong");
    }

    [Fact]
    public void ParseDataExchangeCallback_ReturnsUnknown_WhenStatusMissing()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var result = client.ParseDataExchangeCallback(new Uri("https://myapp.com/"));

        result.Status.Should().Be(DataExchangeStatus.Unknown);
    }

    [Fact]
    public void ParseDataExchangeCallback_SplitsCommaSeparatedPermissions()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var result = client.ParseDataExchangeCallback(new Uri(
            "https://myapp.com/?data_exchange_status=granted&granted_permissions=highlights,bookmarks"));

        result.GrantedPermissions.Should().BeEquivalentTo(["highlights", "bookmarks"]);
    }

    [Fact]
    public void ParseDataExchangeCallback_TrimsWhitespaceAndRemovesEmptyEntries_InCommaSeparatedPermissions()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var result = client.ParseDataExchangeCallback(new Uri(
            "https://myapp.com/?data_exchange_status=granted&granted_permissions=" +
            Uri.EscapeDataString("highlights, ,bookmarks ")));

        result.GrantedPermissions.Should().BeEquivalentTo(["highlights", "bookmarks"]);
    }

    [Fact]
    public void ParseDataExchangeCallback_DecodesPlusSignsAsSpaces_InValues()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var result = client.ParseDataExchangeCallback(new Uri(
            "https://myapp.com/?data_exchange_status=error&error_description=Something+went+wrong"));

        result.ErrorDescription.Should().Be("Something went wrong");
    }

    [Fact]
    public void ParseDataExchangeCallback_TreatsQueryPairWithoutEqualsSign_AsEmptyValue()
    {
        var client = BuildClient(HttpStatusCode.OK, TokenJson);

        var result = client.ParseDataExchangeCallback(new Uri(
            "https://myapp.com/?error&data_exchange_status=error"));

        result.Error.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // CompleteDataExchangeApprovalAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CompleteDataExchangeApprovalAsync_ReturnsParsedResult_OnSeeOtherResponse()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.SeeOther, "", response =>
            response.Headers.Location = new Uri("https://myapp.com/?data_exchange_status=granted&granted_permissions=highlights"));
        var client = BuildClientFromHandler(handler);

        var result = await client.CompleteDataExchangeApprovalAsync("dx-tok");

        result.Status.Should().Be(DataExchangeStatus.Granted);
        result.GrantedPermissions.Should().ContainSingle().Which.Should().Be("highlights");
    }

    [Fact]
    public async Task CompleteDataExchangeApprovalAsync_SendsTokenAndAppKeyQueryParametersWithNoBody()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.SeeOther, "", response =>
            response.Headers.Location = new Uri("https://myapp.com/?data_exchange_status=granted"));
        var client = BuildClientFromHandler(handler);

        await client.CompleteDataExchangeApprovalAsync("dx-tok");

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.Headers.Authorization.Should().BeNull();
        handler.LastRequest.RequestUri!.Query.Should().Contain("token=dx-tok").And.Contain("x-yvp-app-key=test-app-key");
        handler.LastRequestBody.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task CompleteDataExchangeApprovalAsync_ThrowsInvalidOperationException_WhenAppKeyNotConfigured()
    {
        var client = BuildClient(HttpStatusCode.SeeOther, "", appKey: null);

        var act = () => client.CompleteDataExchangeApprovalAsync("dx-tok");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AppKey*");
    }

    [Fact]
    public async Task CompleteDataExchangeApprovalAsync_ThrowsArgumentException_WhenTokenIsEmpty()
    {
        var client = BuildClient(HttpStatusCode.SeeOther, "");

        var act = () => client.CompleteDataExchangeApprovalAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CompleteDataExchangeApprovalAsync_ThrowsBibleApiException_WhenResponseIsNotSeeOther()
    {
        var client = BuildClient(HttpStatusCode.Unauthorized, """{"error":"invalid_token"}""");

        var act = () => client.CompleteDataExchangeApprovalAsync("dx-tok");

        await act.Should().ThrowAsync<BibleApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CompleteDataExchangeApprovalAsync_ThrowsBibleApiException_WhenSeeOtherResponseHasNoLocationHeader()
    {
        // StatusCode alone isn't sufficient — a SeeOther response with no Location header to
        // follow must still be treated as a failure.
        var client = BuildClient(HttpStatusCode.SeeOther, "no redirect target");

        var act = () => client.CompleteDataExchangeApprovalAsync("dx-tok");

        await act.Should().ThrowAsync<BibleApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.SeeOther);
    }

    // -------------------------------------------------------------------------
    // Logging
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildAuthorizationUrl_LogsDebugMessage()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, TokenJson);

        client.BuildAuthorizationUrl();

        logger.Entries.Should().Contain(e => e.Message == "Building authorization URL.");
    }

    [Fact]
    public async Task ExchangeCodeAsync_LogsDebugAndInformationMessages()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, TokenJson);

        await client.ExchangeCodeAsync("auth-code", "verifier");

        logger.Entries.Should().Contain(e => e.Message == "Exchanging authorization code for tokens.");
        logger.Entries.Should().Contain(e =>
            e.Message == "Authorization code exchange succeeded. Token expires in 3600s.");
    }

    [Fact]
    public async Task ExchangeCodeAsync_LogsError_WhenApiReturnsError()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");

        var act = () => client.ExchangeCodeAsync("bad-code", "verifier");
        await act.Should().ThrowAsync<BibleApiException>();

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Error &&
            e.Message == "OAuth token request failed with HTTP 400 Bad Request.");
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_LogsDebugMessages_OnSuccess()
    {
        var stateRedirect = new HttpResponseMessage(HttpStatusCode.Found);
        stateRedirect.Headers.Location = new Uri("https://auth.youversion.com/auth/callback?code=auth-code-xyz");
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
        };
        var handler = new SequencedHttpMessageHandler(stateRedirect, tokenResponse);
        var logger = new CapturingLogger<BibleOAuthClient>();
        var client = BuildClientFromSequencedHandler(handler, logger: logger);

        await client.CompleteAuthorizationCallbackAsync("state-abc", "verifier");

        logger.Entries.Should().Contain(e => e.Message ==
            "Replaying state to the auth callback endpoint to obtain an authorization code.");
        logger.Entries.Should().Contain(e => e.Message ==
            "Auth callback exchange succeeded; redeeming the authorization code.");
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_LogsError_WhenNoLocationHeaderReturned()
    {
        var noLocationResponse = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream error", Encoding.UTF8, "text/plain")
        };
        var handler = new SequencedHttpMessageHandler(noLocationResponse);
        var logger = new CapturingLogger<BibleOAuthClient>();
        var client = BuildClientFromSequencedHandler(handler, logger: logger);

        var act = () => client.CompleteAuthorizationCallbackAsync("state-abc", "verifier");
        await act.Should().ThrowAsync<BibleApiException>();

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Error &&
            e.Message == "Auth callback exchange failed with HTTP 502 Bad Gateway and no Location header. " +
                "Response body: upstream error");
    }

    [Fact]
    public async Task RefreshTokenAsync_LogsDebugAndInformationMessages()
    {
        const string newTokenJson = """
            { "access_token": "new-acc", "refresh_token": "new-ref",
              "token_type": "Bearer", "expires_in": 3600 }
            """;
        var existing = new OAuthTokenResponse
        {
            AccessToken = "old-acc", RefreshToken = "ref-tok",
            ExpiresIn = 3600, ReceivedAt = DateTimeOffset.UtcNow
        };
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, newTokenJson, new FakeTokenProvider(existing));

        await client.RefreshTokenAsync();

        logger.Entries.Should().Contain(e => e.Message == "Refreshing access token.");
        logger.Entries.Should().Contain(e => e.Message == "Token refresh succeeded. New token expires in 3600s.");
    }

    [Fact]
    public async Task SignOutAsync_LogsInformationMessage()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, TokenJson);

        await client.SignOutAsync();

        logger.Entries.Should().Contain(e => e.Message == "User signed out; token cleared from provider.");
    }

    [Fact]
    public async Task RequestPermissionsAsync_LogsDebugMessage_WithJoinedPermissions()
    {
        const string responseJson = """{ "token": "dx-tok", "token_type": "data_exchange", "expires_in": 300 }""";
        var (client, logger) = BuildClientWithLogger(
            HttpStatusCode.Created, responseJson, new FakeTokenProvider(MakeStoredToken()));

        await client.RequestPermissionsAsync(["highlights", "bookmarks"]);

        logger.Entries.Should().Contain(e => e.Message ==
            "Requesting data exchange token for permissions: highlights, bookmarks.");
    }

    [Fact]
    public async Task RequestPermissionsAsync_LogsError_OnFailure()
    {
        var (client, logger) = BuildClientWithLogger(
            HttpStatusCode.Unauthorized, """{"error":"invalid_token"}""", new FakeTokenProvider(MakeStoredToken()));

        var act = () => client.RequestPermissionsAsync(["highlights"]);
        await act.Should().ThrowAsync<BibleApiException>();

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Error &&
            e.Message.StartsWith("Data exchange token request failed with HTTP 401 Unauthorized."));
    }

    [Fact]
    public void BuildDirectDataExchangeUrl_LogsDebugMessage()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.OK, TokenJson);

        client.BuildDirectDataExchangeUrl(["highlights"]);

        logger.Entries.Should().Contain(e => e.Message == "Building direct data exchange URL.");
    }

    [Fact]
    public async Task CompleteDataExchangeApprovalAsync_LogsDebugAndInformationMessages()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.SeeOther, "", response =>
            response.Headers.Location = new Uri("https://myapp.com/?data_exchange_status=granted"));
        var logger = new CapturingLogger<BibleOAuthClient>();
        var client = BuildClientFromHandler(handler, logger: logger);

        await client.CompleteDataExchangeApprovalAsync("dx-tok");

        logger.Entries.Should().Contain(e => e.Message == "Completing data exchange approval for token.");
        logger.Entries.Should().Contain(e => e.Message == "Data exchange approval completed with status Granted.");
    }

    [Fact]
    public async Task CompleteDataExchangeApprovalAsync_LogsError_OnFailure()
    {
        var (client, logger) = BuildClientWithLogger(HttpStatusCode.Unauthorized, """{"error":"invalid_token"}""");

        var act = () => client.CompleteDataExchangeApprovalAsync("dx-tok");
        await act.Should().ThrowAsync<BibleApiException>();

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Error &&
            e.Message.StartsWith("Data exchange completion failed with HTTP 401 Unauthorized."));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static BibleOAuthOptions DefaultOAuthOptions() => new()
    {
        ClientId = "test-client",
        RedirectUri = new Uri("https://localhost/callback"),
        AuthorizationEndpoint = new Uri("https://auth.youversion.com/oauth2/authorize"),
        TokenEndpoint = new Uri("https://auth.youversion.com/oauth2/token")
    };

    private static BibleOAuthClient BuildClient(
        HttpStatusCode status,
        string json,
        FakeTokenProvider? tokenProvider = null,
        string? appKey = "test-app-key",
        Action<BibleOAuthOptions>? configureOptions = null,
        ILogger<BibleOAuthClient>? logger = null)
    {
        var handler = new FakeHttpMessageHandler(status, json);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://auth.youversion.com")
        };
        var oauthOptions = DefaultOAuthOptions();
        configureOptions?.Invoke(oauthOptions);
        var options = Options.Create(oauthOptions);
        var apiOptions = Options.Create(new BibleApiOptions { AppKey = appKey ?? string.Empty });
        return new BibleOAuthClient(
            httpClient,
            options,
            apiOptions,
            tokenProvider ?? new FakeTokenProvider(),
            logger ?? NullLogger<BibleOAuthClient>.Instance);
    }

    /// <summary>Like <see cref="BuildClient"/>, but also returns the logger so tests can assert on captured log messages.</summary>
    private static (BibleOAuthClient Client, CapturingLogger<BibleOAuthClient> Logger) BuildClientWithLogger(
        HttpStatusCode status,
        string json,
        FakeTokenProvider? tokenProvider = null,
        string? appKey = "test-app-key",
        Action<BibleOAuthOptions>? configureOptions = null)
    {
        var logger = new CapturingLogger<BibleOAuthClient>();
        var client = BuildClient(status, json, tokenProvider, appKey, configureOptions, logger);
        return (client, logger);
    }

    private static BibleOAuthClient BuildClientFromHandler(
        FakeHttpMessageHandler handler,
        FakeTokenProvider? tokenProvider = null,
        string? appKey = "test-app-key",
        Action<BibleOAuthOptions>? configureOptions = null,
        ILogger<BibleOAuthClient>? logger = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.youversion.com") };
        var oauthOptions = DefaultOAuthOptions();
        configureOptions?.Invoke(oauthOptions);
        var options = Options.Create(oauthOptions);
        var apiOptions = Options.Create(new BibleApiOptions { AppKey = appKey ?? string.Empty });
        return new BibleOAuthClient(
            httpClient,
            options,
            apiOptions,
            tokenProvider ?? new FakeTokenProvider(),
            logger ?? NullLogger<BibleOAuthClient>.Instance);
    }

    private static BibleOAuthClient BuildClientFromSequencedHandler(
        SequencedHttpMessageHandler handler,
        FakeTokenProvider? tokenProvider = null,
        string? appKey = "test-app-key",
        ILogger<BibleOAuthClient>? logger = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.youversion.com") };
        var options = Options.Create(new BibleOAuthOptions
        {
            ClientId = "test-client",
            RedirectUri = new Uri("https://localhost/callback"),
            AuthorizationEndpoint = new Uri("https://auth.youversion.com/oauth2/authorize"),
            AuthCallbackEndpoint = new Uri("https://auth.youversion.com/auth/callback"),
            TokenEndpoint = new Uri("https://auth.youversion.com/oauth2/token")
        });
        var apiOptions = Options.Create(new BibleApiOptions { AppKey = appKey ?? string.Empty });
        return new BibleOAuthClient(
            httpClient,
            options,
            apiOptions,
            tokenProvider ?? new FakeTokenProvider(),
            logger ?? NullLogger<BibleOAuthClient>.Instance);
    }

    private static OAuthTokenResponse MakeStoredToken() => new()
    {
        AccessToken = "acc-tok",
        RefreshToken = "ref-tok",
        ExpiresIn = 3600,
        ReceivedAt = DateTimeOffset.UtcNow,
    };

}
