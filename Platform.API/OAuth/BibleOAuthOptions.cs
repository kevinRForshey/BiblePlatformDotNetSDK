using System;
using System.ComponentModel.DataAnnotations;

namespace Platform.API.OAuth;

/// <summary>
/// Configuration options for the platform's OAuth 2.0 flow with PKCE.
/// Bind to the <c>BibleOAuth</c> configuration section or configure inline.
/// </summary>
public sealed class BibleOAuthOptions
{
    /// <summary>The configuration section name used when binding from <c>IConfiguration</c>.</summary>
    public const string SectionName = "BibleOAuth";

    /// <summary>
    /// The OAuth 2.0 client identifier registered in the platform's developer portal.
    /// </summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The URI the authorization server redirects to after the user grants or denies access.
    /// Must match a URI registered in the platform's developer portal.
    /// </summary>
    public Uri? RedirectUri { get; set; }

    /// <summary>
    /// The OAuth 2.0 authorization endpoint URL.
    /// Defaults to the platform's authorization server.
    /// </summary>
    public Uri AuthorizationEndpoint { get; set; } =
        new("https://api.youversion.com/auth/authorize");

    /// <summary>
    /// The auth callback endpoint URL. Redirecting to <see cref="AuthorizationEndpoint"/> sends
    /// the user back with only a <c>state</c> query parameter (no <c>code</c>, and, as of the
    /// platform's 2026-07-20 sign-in change, no identity fields either) — this is step 2 of the
    /// platform's documented sign-in flow (https://developers.youversion.com/sign-in-apis):
    /// <c>state</c> is replayed here to obtain the actual authorization code. See
    /// <see cref="IBibleOAuthClient.CompleteAuthorizationCallbackAsync"/>.
    /// Defaults to the platform's callback server.
    /// </summary>
    public Uri AuthCallbackEndpoint { get; set; } =
        new("https://api.youversion.com/auth/callback");

    /// <summary>
    /// The OAuth 2.0 token endpoint URL.
    /// Defaults to the platform's token server.
    /// </summary>
    public Uri TokenEndpoint { get; set; } =
        new("https://api.youversion.com/auth/token");

    /// <summary>
    /// The base Data Exchange endpoint URL, used to request additional per-resource permissions
    /// (e.g. <c>highlights</c>) beyond basic sign-in. See
    /// <see cref="IBibleOAuthClient.RequestPermissionsAsync"/>.
    /// </summary>
    public Uri DataExchangeEndpoint { get; set; } =
        new("https://api.youversion.com/data-exchange");

    /// <summary>
    /// Space-separated OAuth scopes to request. The only scopes the platform's sign-in API
    /// supports are <c>openid</c>, <c>profile</c>, and <c>email</c> — there is no separate
    /// scope for passages or highlights. Sign-in alone only grants identity; resource
    /// permissions like <c>highlights</c> require the separate Data Exchange consent flow
    /// (see <see cref="IBibleOAuthClient.RequestPermissionsAsync"/>).
    /// </summary>
    public string Scopes { get; set; } = "openid profile email";

    /// <summary>
    /// How many seconds before a token's actual expiry to proactively refresh it inside
    /// <see cref="Platform.API.Http.OAuthBearerTokenHandler"/>.
    /// Defaults to 60 seconds.
    /// </summary>
    public int OAuthTokenExpiryBufferSeconds { get; set; } = 60;
}
