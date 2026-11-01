namespace Platform.API.OAuth;

/// <summary>
/// The outcome of <see cref="IBibleOAuthClient.CompleteAuthorizationCallbackAsync"/>: the redeemed
/// token, plus any resource permissions the platform reported as granted or denied alongside it.
/// </summary>
public sealed record AuthorizationCallbackResult
{
    /// <summary>The token redeemed from the authorization code. Already stored via <see cref="ITokenProvider"/>.</summary>
    public required OAuthTokenResponse Token { get; init; }

    /// <summary>
    /// The permissions the user granted or denied, if <c>requested_permissions</c> was passed to
    /// <see cref="IBibleOAuthClient.BuildAuthorizationUrl"/>. <see langword="null"/> means no
    /// permissions were requested this round trip — distinct from an empty list, which means
    /// permissions were requested and all were denied. Treat <see langword="null"/> as "no change"
    /// rather than "denied," since overwriting an existing grant from an earlier round trip would
    /// be wrong.
    /// </summary>
    public IReadOnlyList<string>? GrantedPermissions { get; init; }
}
