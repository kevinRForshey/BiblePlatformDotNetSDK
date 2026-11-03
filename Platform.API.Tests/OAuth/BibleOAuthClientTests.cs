using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Platform.API.Configuration;
using Platform.API.Exceptions;
using Platform.API.OAuth;
using Platform.API.Tests.Fakes;

using Xunit;

namespace Platform.API.Tests.OAuth;

public sealed class BibleOAuthClientTests
{
    // -------------------------------------------------------------------------
    // BuildAuthorizationUrl
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void BuildAuthorizationUrl_ThrowsArgumentException_WhenStateIsEmptyOrWhitespace(string state)
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.BuildAuthorizationUrl(state);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*State cannot be empty or whitespace when provided*");
    }

    [Fact]
    public void BuildAuthorizationUrl_DefaultsScopeToOpenId_WhenScopesIsNull()
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"), options => options.Scopes = null!);

        var result = client.BuildAuthorizationUrl();

        result.AuthorizationUrl.Query.Should().Contain("scope=openid");
    }

    [Fact]
    public void BuildAuthorizationUrl_SkipsWhitespaceOrEmptyRequestedPermissions()
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var result = client.BuildAuthorizationUrl(requestedPermissions: ["highlights", " ", ""]);

        var occurrences = CountOccurrences(result.AuthorizationUrl.Query, "requested_permissions");
        occurrences.Should().Be(1);
        result.AuthorizationUrl.Query.Should().Contain("highlights");
    }

    // -------------------------------------------------------------------------
    // ExchangeCodeAsync
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ExchangeCodeAsync_ThrowsArgumentException_WhenCodeIsEmptyOrWhitespace(string code)
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.ExchangeCodeAsync(code, "verifier");

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Authorization code is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ExchangeCodeAsync_ThrowsArgumentException_WhenCodeVerifierIsEmptyOrWhitespace(string codeVerifier)
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.ExchangeCodeAsync("code", codeVerifier);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*PKCE code verifier is required*");
    }

    [Fact]
    public async Task ExchangeCodeAsync_PostsExactExpectedFormData()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"access_token":"tok","token_type":"Bearer","expires_in":3600}""");
        var client = BuildClient(handler, options => options.ClientId = "my-client");

        await client.ExchangeCodeAsync("auth-code", "verifier-123");

        ParseForm(handler.LastRequestBody!).Should().Equal(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = "my-client",
            ["code"] = "auth-code",
            ["code_verifier"] = "verifier-123",
        });
    }

    [Fact]
    public async Task ExchangeCodeAsync_ThrowsBibleApiException_WhenTokenRequestFails()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, "boom",
            r => r.ReasonPhrase = "Bad Request");
        var client = BuildClient(handler);

        var act = () => client.ExchangeCodeAsync("code", "verifier");

        await act.Should().ThrowAsync<BibleApiException>()
            .WithMessage("*OAuth token request failed with status 400 (Bad Request)*");
    }

    [Fact]
    public async Task ExchangeCodeAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "null");
        var client = BuildClient(handler);

        var act = () => client.ExchangeCodeAsync("code", "verifier");

        await act.Should().ThrowAsync<BibleEmptyResponseException>()
            .WithMessage("*OAuth token endpoint returned an empty response body*");
    }

    // -------------------------------------------------------------------------
    // CompleteAuthorizationCallbackAsync
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CompleteAuthorizationCallbackAsync_ThrowsArgumentException_WhenStateIsEmptyOrWhitespace(string state)
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.CompleteAuthorizationCallbackAsync(state, "verifier");

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*State is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CompleteAuthorizationCallbackAsync_ThrowsArgumentException_WhenCodeVerifierIsEmptyOrWhitespace(string codeVerifier)
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.CompleteAuthorizationCallbackAsync("state", codeVerifier);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*PKCE code verifier is required*");
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_ThrowsBibleApiException_WhenNoLocationHeader()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, "boom",
            r => r.ReasonPhrase = "Bad Request");
        var client = BuildClient(handler);

        var act = () => client.CompleteAuthorizationCallbackAsync("state", "verifier");

        await act.Should().ThrowAsync<BibleApiException>()
            .WithMessage("*Auth callback exchange failed with status 400 (Bad Request) and no redirect to follow*");
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_ThrowsBibleEmptyResponseException_WhenNoCodeInRedirect()
    {
        var callbackResponse = new HttpResponseMessage(HttpStatusCode.Found);
        callbackResponse.Headers.Location = new Uri("https://example.com/callback?state=xyz");
        var handler = new SequencedHttpMessageHandler(callbackResponse);
        var client = BuildClient(handler);

        var act = () => client.CompleteAuthorizationCallbackAsync("xyz", "verifier");

        await act.Should().ThrowAsync<BibleEmptyResponseException>()
            .WithMessage("*did not include an authorization code*");
    }

    [Fact]
    public async Task CompleteAuthorizationCallbackAsync_RedeemsCodeAndReturnsGrantedPermissions()
    {
        var callbackResponse = new HttpResponseMessage(HttpStatusCode.Found);
        callbackResponse.Headers.Location =
            new Uri("https://example.com/callback?code=auth-code-123&state=xyz&granted_permissions=highlights");
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"access_token":"tok","token_type":"Bearer","expires_in":3600}""",
                Encoding.UTF8, "application/json"),
        };
        var handler = new SequencedHttpMessageHandler(callbackResponse, tokenResponse);
        var client = BuildClient(handler);

        var result = await client.CompleteAuthorizationCallbackAsync("xyz", "verifier");

        result.Token.AccessToken.Should().Be("tok");
        result.GrantedPermissions.Should().ContainSingle().Which.Should().Be("highlights");
    }

    // -------------------------------------------------------------------------
    // RefreshTokenAsync / SignOutAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshTokenAsync_ThrowsInvalidOperationException_WhenNoRefreshTokenStored()
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"),
            tokenProvider: new FakeTokenProvider());

        var act = () => client.RefreshTokenAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No refresh token is available*");
    }

    [Fact]
    public async Task SignOutAsync_ClearsStoredToken()
    {
        var provider = new FakeTokenProvider(new OAuthTokenResponse { AccessToken = "tok" });
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"), tokenProvider: provider);

        await client.SignOutAsync();

        (await provider.GetTokenAsync()).Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // RequestPermissionsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RequestPermissionsAsync_ThrowsArgumentNullException_WithPermissionsParamName_WhenPermissionsIsNull()
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.RequestPermissionsAsync(null!);

        var exception = await act.Should().ThrowAsync<ArgumentNullException>();
        exception.Which.ParamName.Should().Be("permissions");
    }

    [Fact]
    public async Task RequestPermissionsAsync_ThrowsArgumentException_WhenPermissionsIsEmpty()
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.RequestPermissionsAsync([]);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*At least one permission must be requested*");
    }

    [Fact]
    public async Task RequestPermissionsAsync_ThrowsInvalidOperationException_WhenNoAccessTokenStored()
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"),
            tokenProvider: new FakeTokenProvider());

        var act = () => client.RequestPermissionsAsync(["highlights"]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No access token is stored*");
    }

    [Fact]
    public async Task RequestPermissionsAsync_ThrowsInvalidOperationException_WhenAppKeyIsNotConfigured()
    {
        var provider = new FakeTokenProvider(new OAuthTokenResponse { AccessToken = "tok" });
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"),
            configureApiOptions: o => o.AppKey = "",
            tokenProvider: provider);

        var act = () => client.RequestPermissionsAsync(["highlights"]);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("BibleApiOptions.AppKey is not configured");
        exception.Which.Message.Should().Contain("cannot rely on the X-YVP-App-Key header");
        exception.Which.Message.Should().Contain("include it as a query parameter");
    }

    [Fact]
    public async Task RequestPermissionsAsync_ThrowsBibleApiException_WhenRequestFails()
    {
        var provider = new FakeTokenProvider(new OAuthTokenResponse { AccessToken = "tok" });
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Forbidden, "boom",
            r => r.ReasonPhrase = "Forbidden");
        var client = BuildClient(handler, tokenProvider: provider);

        var act = () => client.RequestPermissionsAsync(["highlights"]);

        await act.Should().ThrowAsync<BibleApiException>()
            .WithMessage("*Data exchange token request failed with status 403 (Forbidden)*");
    }

    [Fact]
    public async Task RequestPermissionsAsync_ThrowsBibleEmptyResponseException_WhenBodyIsNull()
    {
        var provider = new FakeTokenProvider(new OAuthTokenResponse { AccessToken = "tok" });
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "null");
        var client = BuildClient(handler, tokenProvider: provider);

        var act = () => client.RequestPermissionsAsync(["highlights"]);

        await act.Should().ThrowAsync<BibleEmptyResponseException>()
            .WithMessage("*Data exchange token endpoint returned an empty response body*");
    }

    // -------------------------------------------------------------------------
    // BuildDataExchangeApprovalUrl
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void BuildDataExchangeApprovalUrl_ThrowsArgumentException_WhenTokenIsEmptyOrWhitespace(string token)
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.BuildDataExchangeApprovalUrl(token);

        act.Should().Throw<ArgumentException>().WithMessage("*Data exchange token is required*");
    }

    // -------------------------------------------------------------------------
    // BuildDirectDataExchangeUrl
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildDirectDataExchangeUrl_ThrowsArgumentException_WhenPermissionsIsEmpty()
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.BuildDirectDataExchangeUrl([]);

        act.Should().Throw<ArgumentException>().WithMessage("*At least one permission must be requested*");
    }

    [Fact]
    public void BuildDirectDataExchangeUrl_ThrowsArgumentNullException_WithPermissionsParamName_WhenPermissionsIsNull()
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.BuildDirectDataExchangeUrl(null!);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("permissions");
    }

    // -------------------------------------------------------------------------
    // ParseDataExchangeCallback
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseDataExchangeCallback_ThrowsArgumentNullException_WithCallbackUrlParamName_WhenCallbackUrlIsNull()
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.ParseDataExchangeCallback(null!);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("callbackUrl");
    }

    // -------------------------------------------------------------------------
    // CompleteDataExchangeApprovalAsync
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CompleteDataExchangeApprovalAsync_ThrowsArgumentException_WhenTokenIsEmptyOrWhitespace(string token)
    {
        var client = BuildClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        var act = () => client.CompleteDataExchangeApprovalAsync(token);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Data exchange token is required*");
    }

    [Fact]
    public async Task CompleteDataExchangeApprovalAsync_ThrowsBibleApiException_WhenResponseIsNotSeeOtherRedirect()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "boom",
            r => r.ReasonPhrase = "Internal Server Error");
        var client = BuildClient(handler);

        var act = () => client.CompleteDataExchangeApprovalAsync("dx-token");

        await act.Should().ThrowAsync<BibleApiException>()
            .WithMessage("*Data exchange completion failed with status 500 (Internal Server Error)*");
    }

    // -------------------------------------------------------------------------
    // Private helper boundary tests (via reflection)
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseQueryString_TreatsLeadingEqualsSign_AsEmptyKey()
    {
        var method = typeof(BibleOAuthClient).GetMethod("ParseQueryString", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (Dictionary<string, List<string>>)method.Invoke(null, ["=foo"])!;

        result.Should().ContainKey("");
        result[""].Should().ContainSingle().Which.Should().Be("foo");
    }

    [Fact]
    public void ParseQueryString_DecodesPlusSign_AsSpace_InKeyAndValue()
    {
        var method = typeof(BibleOAuthClient).GetMethod("ParseQueryString", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (Dictionary<string, List<string>>)method.Invoke(null, ["a+b=c+d"])!;

        result.Should().ContainKey("a b");
        result["a b"].Should().ContainSingle().Which.Should().Be("c d");
    }

    [Fact]
    public void FirstOrDefaultHelper_ReturnsNull_WhenValuesListIsEmpty()
    {
        var method = typeof(BibleOAuthClient).GetMethod("FirstOrDefault", BindingFlags.NonPublic | BindingFlags.Static)!;
        var dict = new Dictionary<string, List<string>> { ["key"] = [] };

        var result = method.Invoke(null, [dict, "key"]);

        result.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }

    private static Dictionary<string, string> ParseForm(string formBody)
        => formBody.Split('&')
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]),
                parts => Uri.UnescapeDataString(parts[1]));

    private static BibleOAuthClient BuildClient(
        HttpMessageHandler handler,
        Action<BibleOAuthOptions>? configureOptions = null,
        Action<BibleApiOptions>? configureApiOptions = null,
        ITokenProvider? tokenProvider = null)
    {
        var options = new BibleOAuthOptions { ClientId = "client-id" };
        configureOptions?.Invoke(options);

        var apiOptions = new BibleApiOptions { AppKey = "app-key" };
        configureApiOptions?.Invoke(apiOptions);

        var httpClient = new HttpClient(handler);
        return new BibleOAuthClient(
            httpClient,
            Options.Create(options),
            Options.Create(apiOptions),
            tokenProvider ?? new FakeTokenProvider(),
            NullLogger<BibleOAuthClient>.Instance);
    }
}
