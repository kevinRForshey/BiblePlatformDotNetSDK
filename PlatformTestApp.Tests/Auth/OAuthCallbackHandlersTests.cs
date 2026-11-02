using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using Platform.API.OAuth;

using PlatformTestApp.Auth;
using PlatformTestApp.Tests.Fakes;

using Xunit;

namespace PlatformTestApp.Tests.Auth;

/// <summary>
/// Regression coverage for the gap described in
/// <c>specs/0001-oauth-signin-and-data-exchange.md</c>: before this file existed, none of the
/// callback-shape dispatch methods in <see cref="OAuthCallbackHandlers"/> had any test coverage,
/// which is how the <c>granted_permissions</c>-on-the-wrong-hop bug shipped undetected.
/// </summary>
public class OAuthCallbackHandlersTests
{
    [Fact]
    public void TryHandleAuthorizationCodeCallback_ReturnsFalse_WhenNoCodeParameter()
    {
        var (ctx, _, _, _) = BuildContext("?state=xyz");

        var handled = OAuthCallbackHandlers.TryHandleAuthorizationCodeCallback(ctx);

        handled.Should().BeFalse();
    }

    [Fact]
    public void TryHandleAuthorizationCodeCallback_StoresCodeStateAndGrantedPermissions_InSession()
    {
        var (ctx, _, _, _) = BuildContext("?code=auth-code&state=xyz&granted_permissions=highlights");

        var handled = OAuthCallbackHandlers.TryHandleAuthorizationCodeCallback(ctx);

        handled.Should().BeTrue();
        ctx.Session.GetString("oauth_code").Should().Be("auth-code");
        ctx.Session.GetString("oauth_state_return").Should().Be("xyz");
        ctx.Session.GetString("oauth_granted_permissions").Should().Be("highlights");
        ctx.Response.Headers.Location.ToString().Should().Be("/auth/callback-complete");
    }

    [Fact]
    public async Task TryHandleDataExchangeCallbackAsync_ReturnsFalse_WhenNoDataExchangeStatusParameter()
    {
        var (ctx, _, _, _) = BuildContext("?state=xyz");

        var handled = await OAuthCallbackHandlers.TryHandleDataExchangeCallbackAsync(ctx);

        handled.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleDataExchangeCallbackAsync_SetsHighlightsGranted_AndRedirects_WhenGranted()
    {
        var (ctx, oauthClient, _, highlightsStore) = BuildContext("?data_exchange_status=granted");
        oauthClient
            .Setup(x => x.ParseDataExchangeCallback(It.IsAny<Uri>()))
            .Returns(new DataExchangeCallbackResult { Status = DataExchangeStatus.Granted });

        var handled = await OAuthCallbackHandlers.TryHandleDataExchangeCallbackAsync(ctx);

        handled.Should().BeTrue();
        ctx.Response.Headers.Location.ToString().Should().Be("/?auth_mode=code&highlights=granted");
        (await highlightsStore.GetGrantedAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleDataExchangeCallbackAsync_AppendsOAuthError_WhenStatusIsError()
    {
        var (ctx, oauthClient, _, _) = BuildContext("?data_exchange_status=error");
        oauthClient
            .Setup(x => x.ParseDataExchangeCallback(It.IsAny<Uri>()))
            .Returns(new DataExchangeCallbackResult { Status = DataExchangeStatus.Error, Error = "access_denied" });

        await OAuthCallbackHandlers.TryHandleDataExchangeCallbackAsync(ctx);

        ctx.Response.Headers.Location.ToString().Should().Contain("oauth_error=access_denied");
    }

    [Fact]
    public async Task TryHandleStateOnlyCallbackAsync_ReturnsFalse_WhenNoStateParameter()
    {
        var (ctx, _, _, _) = BuildContext("?code=abc");

        var handled = await OAuthCallbackHandlers.TryHandleStateOnlyCallbackAsync(ctx);

        handled.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleStateOnlyCallbackAsync_ReturnsFalse_WhenCodeParameterIsAlsoPresent()
    {
        var (ctx, _, _, _) = BuildContext("?state=xyz&code=abc");

        var handled = await OAuthCallbackHandlers.TryHandleStateOnlyCallbackAsync(ctx);

        handled.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleStateOnlyCallbackAsync_RedirectsWithOAuthError_WhenStateDoesNotMatch()
    {
        var (ctx, oauthClient, _, _) = BuildContext("?state=returned-state", sessionSetup: s =>
            s.SetString("oauth_state", "expected-state"));
        oauthClient.Setup(x => x.ValidateState("expected-state", "returned-state")).Returns(false);

        var handled = await OAuthCallbackHandlers.TryHandleStateOnlyCallbackAsync(ctx);

        handled.Should().BeTrue();
        ctx.Response.Headers.Location.ToString().Should().Contain("oauth_error=");
        ctx.Response.Headers.Location.ToString().Should().NotContain("auth_mode=direct&highlights");
    }

    [Fact]
    public async Task TryHandleStateOnlyCallbackAsync_RedirectsWithSessionExpiredError_WhenNoPkceVerifierStored()
    {
        var (ctx, oauthClient, _, _) = BuildContext("?state=returned-state", sessionSetup: s =>
            s.SetString("oauth_state", "expected-state"));
        oauthClient.Setup(x => x.ValidateState("expected-state", "returned-state")).Returns(true);

        var handled = await OAuthCallbackHandlers.TryHandleStateOnlyCallbackAsync(ctx);

        handled.Should().BeTrue();
        ctx.Response.Headers.Location.ToString().Should().Contain("Session%20expired");
    }

    /// <summary>
    /// Regression test for the bug fixed per <c>specs/0001-oauth-signin-and-data-exchange.md</c>:
    /// the platform attaches <c>granted_permissions</c> to this state-only redirect itself, not to
    /// the server-to-server replay response <see cref="IBibleOAuthClient.CompleteAuthorizationCallbackAsync"/>
    /// returns. A handler that only trusted the replay's <see cref="AuthorizationCallbackResult.GrantedPermissions"/>
    /// (null here, simulating that observed shape) would silently treat "highlights" as never granted.
    /// </summary>
    [Fact]
    public async Task TryHandleStateOnlyCallbackAsync_FallsBackToRedirectQueryString_ForGrantedPermissions()
    {
        var (ctx, oauthClient, _, highlightsStore) = BuildContext(
            "?state=returned-state&granted_permissions=highlights",
            sessionSetup: s =>
            {
                s.SetString("oauth_state", "expected-state");
                s.SetString("pkce_verifier", "verifier-123");
            });
        oauthClient.Setup(x => x.ValidateState("expected-state", "returned-state")).Returns(true);
        oauthClient
            .Setup(x => x.CompleteAuthorizationCallbackAsync("returned-state", "verifier-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationCallbackResult { Token = MakeToken(), GrantedPermissions = null });

        var handled = await OAuthCallbackHandlers.TryHandleStateOnlyCallbackAsync(ctx);

        handled.Should().BeTrue();
        ctx.Response.Headers.Location.ToString().Should().Be("/?auth_mode=direct&highlights=granted");
        (await highlightsStore.GetGrantedAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleStateOnlyCallbackAsync_PrefersReplayResult_OverRedirectQueryString_WhenBothPresent()
    {
        var (ctx, oauthClient, _, highlightsStore) = BuildContext(
            "?state=returned-state&granted_permissions=",
            sessionSetup: s =>
            {
                s.SetString("oauth_state", "expected-state");
                s.SetString("pkce_verifier", "verifier-123");
            });
        oauthClient.Setup(x => x.ValidateState("expected-state", "returned-state")).Returns(true);
        oauthClient
            .Setup(x => x.CompleteAuthorizationCallbackAsync("returned-state", "verifier-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationCallbackResult { Token = MakeToken(), GrantedPermissions = ["highlights"] });

        await OAuthCallbackHandlers.TryHandleStateOnlyCallbackAsync(ctx);

        ctx.Response.Headers.Location.ToString().Should().Be("/?auth_mode=direct&highlights=granted");
        (await highlightsStore.GetGrantedAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleStateOnlyCallbackAsync_RedirectsWithoutHighlightsParam_WhenPermissionsNeverRequested()
    {
        var (ctx, oauthClient, _, highlightsStore) = BuildContext(
            "?state=returned-state",
            sessionSetup: s =>
            {
                s.SetString("oauth_state", "expected-state");
                s.SetString("pkce_verifier", "verifier-123");
            });
        oauthClient.Setup(x => x.ValidateState("expected-state", "returned-state")).Returns(true);
        oauthClient
            .Setup(x => x.CompleteAuthorizationCallbackAsync("returned-state", "verifier-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationCallbackResult { Token = MakeToken(), GrantedPermissions = null });

        await OAuthCallbackHandlers.TryHandleStateOnlyCallbackAsync(ctx);

        ctx.Response.Headers.Location.ToString().Should().Be("/?auth_mode=direct");
        (await highlightsStore.GetGrantedAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleDevSignInAsync_ReturnsFalse_WhenNotDevelopmentEnvironment()
    {
        var (ctx, _, tokenProvider, _) = BuildContext("?dev_signin=1&user_name=admin");

        var handled = await OAuthCallbackHandlers.TryHandleDevSignInAsync(ctx, isDevelopmentEnvironment: false);

        handled.Should().BeFalse();
        tokenProvider.Verify(x => x.StoreTokenAsync(It.IsAny<OAuthTokenResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryHandleDevSignInAsync_ReturnsFalse_WhenNoDevSigninParameter()
    {
        var (ctx, _, _, _) = BuildContext("?state=xyz");

        var handled = await OAuthCallbackHandlers.TryHandleDevSignInAsync(ctx, isDevelopmentEnvironment: true);

        handled.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleDevSignInAsync_StoresSyntheticToken_AndRedirects_WhenDevelopmentEnvironment()
    {
        var (ctx, _, tokenProvider, _) = BuildContext("?dev_signin=1&user_name=admin&user_email=admin%40example.com");

        var handled = await OAuthCallbackHandlers.TryHandleDevSignInAsync(ctx, isDevelopmentEnvironment: true);

        handled.Should().BeTrue();
        ctx.Response.Headers.Location.ToString().Should().Be("/?auth_mode=direct");
        tokenProvider.Verify(x => x.StoreTokenAsync(It.IsAny<OAuthTokenResponse>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static OAuthTokenResponse MakeToken() => new()
    {
        AccessToken = "acc-tok",
        IdToken = "id-tok",
        ExpiresIn = 3600,
        ReceivedAt = DateTimeOffset.UtcNow,
    };

    private static (DefaultHttpContext Ctx, Mock<IBibleOAuthClient> OAuthClient, Mock<ITokenProvider> TokenProvider, HighlightsPermissionStore HighlightsStore)
        BuildContext(string queryString, Action<InMemorySession>? sessionSetup = null, string sessionId = "session-abc")
    {
        var ctx = new DefaultHttpContext
        {
            Request =
            {
                Scheme = "https",
                Host = new HostString("localhost"),
                Path = "/",
                QueryString = new QueryString(queryString)
            }
        };

        var session = new InMemorySession(sessionId);
        sessionSetup?.Invoke(session);
        ctx.Features.Set<ISessionFeature>(new FakeSessionFeature(session));

        var httpContextAccessor = new FakeHttpContextAccessor { HttpContext = ctx };
        var keyAccessor = new CircuitSessionKeyAccessor(httpContextAccessor, CreateEmptyPersistentState());
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var highlightsStore = new HighlightsPermissionStore(keyAccessor, cache);

        var oauthClient = new Mock<IBibleOAuthClient>();
        var tokenProvider = new Mock<ITokenProvider>();

        var services = new ServiceCollection();
        services.AddSingleton<ILogger<Program>>(NullLogger<Program>.Instance);
        services.AddSingleton(oauthClient.Object);
        services.AddSingleton(highlightsStore);
        services.AddSingleton(tokenProvider.Object);
        ctx.RequestServices = services.BuildServiceProvider();

        return (ctx, oauthClient, tokenProvider, highlightsStore);
    }

    private static Microsoft.AspNetCore.Components.PersistentComponentState CreateEmptyPersistentState()
    {
        var manager = new Microsoft.AspNetCore.Components.Infrastructure.ComponentStatePersistenceManager(
            NullLogger<Microsoft.AspNetCore.Components.Infrastructure.ComponentStatePersistenceManager>.Instance);
        return manager.State;
    }
}
