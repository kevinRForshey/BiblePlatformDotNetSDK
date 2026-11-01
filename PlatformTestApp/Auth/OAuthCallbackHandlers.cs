using Microsoft.AspNetCore.Http.Extensions;
using Platform.API.OAuth;
using System.Text;
using System.Text.Json;

namespace PlatformTestApp.Auth;

/// <summary>
/// Handles the distinct shapes the platform's OAuth/PKCE redirect can take when it lands on "/".
/// Each shape gets its own method so the dispatch in Program.cs stays a flat list of checks
/// instead of a growing chain of inline <c>if</c> blocks.
/// </summary>
internal static class OAuthCallbackHandlers
{
    public static void LogIncomingQuery(HttpContext ctx)
    {
        // Every callback shape lands here first; log so an unhandled shape (e.g. ?error=...)
        // is visible instead of silently falling through to the home page.
        ctx.RequestServices.GetRequiredService<ILogger<Program>>()
            .LogDebug("Landed on \"/\" with query: {Query}", ctx.Request.QueryString);
    }

    public static bool TryHandleAuthorizationCodeCallback(HttpContext ctx)
    {
        if (!ctx.Request.Query.ContainsKey("code"))
            return false;

        ctx.Session.SetString("oauth_code", ctx.Request.Query["code"].ToString());
        ctx.Session.SetString("oauth_state_return", ctx.Request.Query["state"].ToString());
        // Comma-separated grant result, present only when the platform folds it into this callback
        // instead of sending a separate data_exchange_status redirect (both shapes occur live).
        // Empty string = requested but denied; absent = not requested.
        if (ctx.Request.Query.ContainsKey("granted_permissions"))
            ctx.Session.SetString("oauth_granted_permissions", ctx.Request.Query["granted_permissions"].ToString());
        ctx.Response.Redirect("/auth/callback-complete");
        return true;
    }

    /// <remarks>
    /// Standalone Data Exchange callback shape: consent result arrives as a separate hit on "/"
    /// rather than folded into the authorization-code callback above. Confirmed reachable via
    /// live sign-in — don't remove without re-verifying against a real browser flow.
    /// </remarks>
    public static async Task<bool> TryHandleDataExchangeCallbackAsync(HttpContext ctx)
    {
        if (!ctx.Request.Query.ContainsKey("data_exchange_status"))
            return false;

        var oauthClient = ctx.RequestServices.GetRequiredService<IBibleOAuthClient>();
        var result = oauthClient.ParseDataExchangeCallback(new Uri(ctx.Request.GetEncodedUrl()));

        var granted = result.Status == DataExchangeStatus.Granted;
        await ctx.RequestServices.GetRequiredService<HighlightsPermissionStore>().SetGrantedAsync(granted);

        var redirect = $"/?auth_mode=code&highlights={(granted ? "granted" : "denied")}";
        if (result.Status == DataExchangeStatus.Error)
            redirect += $"&oauth_error={Uri.EscapeDataString(result.ErrorDescription ?? result.Error ?? "Data exchange approval failed.")}";

        ctx.Response.Redirect(redirect);
        return true;
    }

    /// <remarks>
    /// Since the platform's 2026-07-20 sign-in change, the normal (non-fallback) step-1 shape for
    /// browser clients is a bare `?state=...` — no `code`, and no identity fields
    /// (https://developers.youversion.com/sign-in-apis). `CompleteAuthorizationCallbackAsync` runs
    /// steps 2-3 (replay `state` to `/auth/callback` for a `code`, then redeem it) and returns a
    /// real token; identity now comes from the token's `id_token` claims, not the redirect.
    /// </remarks>
    public static async Task<bool> TryHandleStateOnlyCallbackAsync(HttpContext ctx)
    {
        if (!ctx.Request.Query.ContainsKey("state") || ctx.Request.Query.ContainsKey("code"))
            return false;

        // In live testing, the platform attaches the Data Exchange grant result to THIS
        // state-only redirect, not to the server-to-server replay CompleteAuthorizationCallbackAsync
        // makes to /auth/callback in step 2 — that replay's response has never been observed to
        // carry granted_permissions, despite BuildAuthorizationUrl's docs describing it as an
        // either/or. Capture it here before it's lost; empty string (present, no value) still means
        // "requested and denied," distinct from the key being absent ("not requested").
        string[]? grantedPermissionsFromRedirect = ctx.Request.Query.ContainsKey("granted_permissions")
            ? ctx.Request.Query["granted_permissions"].ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : null;

        var expectedState = ctx.Session.GetString("oauth_state");
        var returnedState = ctx.Request.Query["state"].ToString();
        var oauthClient = ctx.RequestServices.GetRequiredService<IBibleOAuthClient>();
        if (!oauthClient.ValidateState(expectedState, returnedState))
        {
            ctx.Response.Redirect($"/?oauth_error={Uri.EscapeDataString("State mismatch — possible CSRF attempt. Please try signing in again.")}&auth_mode=direct");
            return true;
        }

        var verifier = ctx.Session.GetString("pkce_verifier");
        ctx.Session.Remove("oauth_state");
        ctx.Session.Remove("pkce_verifier");

        if (string.IsNullOrEmpty(verifier))
        {
            ctx.Response.Redirect($"/?oauth_error={Uri.EscapeDataString("Session expired mid sign-in. Please try again.")}");
            return true;
        }

        AuthorizationCallbackResult result;
        try
        {
            result = await oauthClient.CompleteAuthorizationCallbackAsync(returnedState, verifier);
        }
        catch (Exception ex)
        {
            ctx.RequestServices.GetRequiredService<ILogger<Program>>()
                .LogError(ex, "Completing the authorization callback failed.");
            ctx.Response.Redirect($"/?oauth_error={Uri.EscapeDataString("Sign-in failed while completing the callback. Please try again.")}");
            return true;
        }

        // Null = not requested this round trip, NOT denied — treating it as denied would wipe
        // out an existing grant from a prior /auth/request-highlights approval. Prefer whatever
        // the replay reported (per the platform's documented either/or), falling back to what
        // the initial redirect carried, since that's the shape actually observed live.
        var grantedPermissions = result.GrantedPermissions ?? grantedPermissionsFromRedirect;
        if (grantedPermissions is not null)
        {
            var highlightsGranted = grantedPermissions.Contains("highlights", StringComparer.OrdinalIgnoreCase);
            await ctx.RequestServices.GetRequiredService<HighlightsPermissionStore>().SetGrantedAsync(highlightsGranted);
            ctx.Response.Redirect($"/?auth_mode=direct&highlights={(highlightsGranted ? "granted" : "denied")}");
            return true;
        }

        ctx.Response.Redirect("/?auth_mode=direct");
        return true;
    }

    /// <remarks>
    /// Dev-only shortcut to exercise the UI without a live OAuth round trip. Builds an unsigned
    /// token from raw query params — must never be reachable outside Development, since anyone
    /// could pass ?dev_signin=1&amp;user_name=admin and appear signed in. Gated on the `dev_signin`
    /// marker so it can't collide with a real callback, which is keyed on `state` instead.
    /// </remarks>
    public static async Task<bool> TryHandleDevSignInAsync(HttpContext ctx, bool isDevelopmentEnvironment)
    {
        if (!isDevelopmentEnvironment || !ctx.Request.Query.ContainsKey("dev_signin"))
            return false;

        var userName = ctx.Request.Query["user_name"].ToString();
        var userEmail = ctx.Request.Query["user_email"].ToString();

        var tokenProvider = ctx.RequestServices.GetRequiredService<ITokenProvider>();
        var claims = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(userName))
            claims["name"] = userName;
        if (!string.IsNullOrWhiteSpace(userEmail))
            claims["email"] = userEmail;

        var syntheticIdToken = BuildUnsignedJwt(claims);
        await tokenProvider.StoreTokenAsync(new OAuthTokenResponse
        {
            AccessToken = "oauth-session-user",
            IdToken = syntheticIdToken,
            ExpiresIn = 3600,
            ReceivedAt = DateTimeOffset.UtcNow
        });

        ctx.Response.Redirect("/?auth_mode=direct");
        return true;
    }

    private static string BuildUnsignedJwt(IReadOnlyDictionary<string, string> claims)
    {
        var payloadJson = JsonSerializer.Serialize(claims);
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return $"header.{payload}.signature";
    }
}
