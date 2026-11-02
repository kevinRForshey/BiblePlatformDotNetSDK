# 1. OAuth sign-in and Data Exchange permission flow

## Status

Verified

## Scope

The platform's authorization-code + PKCE sign-in flow (`IBibleOAuthClient.BuildAuthorizationUrl` /
`CompleteAuthorizationCallbackAsync` / `ExchangeCodeAsync` in
[`Platform.API/OAuth/BibleOAuthClient.cs`](../Platform.API/OAuth/BibleOAuthClient.cs)), and the
Data Exchange permission-consent flow layered on top of it (`RequestPermissionsAsync` /
`BuildDataExchangeApprovalUrl` / `ParseDataExchangeCallback`), as implemented and consumed by
`PlatformTestApp` (dispatch logic in
[`PlatformTestApp/Auth/OAuthCallbackHandlers.cs`](../PlatformTestApp/Auth/OAuthCallbackHandlers.cs)).

## The flow, as actually observed

1. **`GET {AuthorizationEndpoint}`** — app redirects the browser here with
   `response_type=code`, `client_id`, `redirect_uri`, `nonce`, `state`, `code_challenge`,
   `code_challenge_method=S256`, `scope` (always includes `openid`).

   To also request a Data Exchange permission (e.g. `highlights`) as part of this same sign-in,
   append **`requested_permissions[]=<permission>`** — repeated once per permission, **with the
   array-bracket suffix**. A bare `requested_permissions=<permission>` (no brackets) is silently
   ignored by the platform: the consent UI never appears, and no `granted_permissions` is ever
   reported back, even though the sign-in itself still succeeds. This was shipped broken once
   already — see History below.

2. **Browser lands back on the app's own callback route** with a redirect carrying, since the
   platform's 2026-07-20 sign-in change, **only `state`** — no `code`, no identity fields. This is
   the "state-only" shape `OAuthCallbackHandlers.TryHandleStateOnlyCallbackAsync` dispatches on
   (`Request.Query.ContainsKey("state") && !Request.Query.ContainsKey("code")`).

   **If `requested_permissions[]` was sent in step 1, `granted_permissions` is attached to *this*
   redirect** — a comma-separated list, empty string if requested-and-denied, the key absent
   entirely if nothing was requested this round trip. This is the flow's central gotcha: nothing
   in the published docs states which of the two possible hops (this one, or step 3 below) carries
   it, and it was wrong once already — see History.

3. **`CompleteAuthorizationCallbackAsync(state, codeVerifier)`** replays `state` as
   `GET {AuthCallbackEndpoint}?state=...` server-to-server. The platform responds with a redirect
   whose `Location` carries `code` (and, per the API's documented either/or, *could* also carry
   `granted_permissions` here instead of step 2 — the code in `BibleOAuthClient.cs` extracts it
   from this response if present via `ExtractPermissionsOrNull`). **In every live trace captured
   this session, this response has never actually carried `granted_permissions`** — only `code`.
   Treat step 2 as the authoritative source for `granted_permissions` until re-verified otherwise.

4. The resulting `code` is redeemed via `ExchangeCodeAsync` → `POST {TokenEndpoint}` → a real
   `OAuthTokenResponse`, stored via `ITokenProvider`. User identity comes from the token's
   `id_token` claims, not from any redirect query string (identity fields were removed from the
   redirect by the same 2026-07-20 platform change).

### Alternate shape: `code` arrives directly

Some redirects carry `code` (and optionally `granted_permissions`) directly instead of the
state-only shape above — `OAuthCallbackHandlers.TryHandleAuthorizationCodeCallback` handles this by
redirecting to `/auth/callback-complete`, which calls `ExchangeCodeAsync` itself. Both shapes are
reachable live; don't remove either without re-confirming via a real sign-in.

### Alternate shape: standalone Data Exchange approval

Requesting a permission *after* sign-in (via `RequestPermissionsAsync` +
`BuildDataExchangeApprovalUrl`, rather than inline via `requested_permissions[]` at sign-in time)
produces a separate callback carrying `data_exchange_status` (`granted` / `cancelled` / `error`),
`granted_permissions`, `denied_permissions`, `error`, `error_description` — parsed by
`ParseDataExchangeCallback`. `BuildDataExchangeApprovalUrl` and `BuildDirectDataExchangeUrl` must
send the app key as an **`x-yvp-app-key` query parameter**, not the usual `X-YVP-App-Key` header —
these are top-level browser navigations, which can't carry custom headers.

## Request vs. response parameter naming asymmetry

| Direction | Parameter | Form |
|---|---|---|
| Request (step 1, `/auth/authorize`) | `requested_permissions[]` | repeated, **bracketed** |
| Response (step 2 or 3) | `granted_permissions` | single value, **comma-separated, no brackets** |

Confirmed against `https://developers.youversion.com/sign-in-apis`'s literal HTML on
2026-11-01 — the bracket form is required on the request side only.

## History

- **2026-11-01** — `BuildAuthorizationUrl`/`BuildDirectDataExchangeUrl` sent the unbracketed
  `requested_permissions=highlights`. Fixed to `requested_permissions[]=highlights`. See
  `CHANGELOG.md` `[Unreleased]`.
- **2026-11-01** — `OAuthCallbackHandlers.TryHandleStateOnlyCallbackAsync` only read
  `granted_permissions` from step 3's replay response, which — per this spec — has never been
  observed to carry it; the real value is on step 2's redirect. Fixed by capturing it from the
  incoming request's query string in step 2 as well, preferring step 3's value only if present.
  Regression-tested in `PlatformTestApp.Tests/Auth/OAuthCallbackHandlersTests.cs`
  (`TryHandleStateOnlyCallbackAsync_FallsBackToRedirectQueryString_ForGrantedPermissions`).

Both bugs shipped past a fully-green test suite because nothing encoded this spec's claims as an
assertion — the tests asserted whatever the code already did, and no test existed at all for
`OAuthCallbackHandlers.cs`. Any future change to this flow should update this spec and its
corresponding tests in the same change, not after a live failure.
