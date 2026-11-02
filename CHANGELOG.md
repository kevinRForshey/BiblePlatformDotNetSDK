# Changelog

All notable changes to this project are documented here. Format loosely follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Package versions are MinVer-derived from
git tags (see `README.md` — "Versioning & Releases").

## [Unreleased]

### Added
- `specs/` — versioned, verified records of the platform's actual wire contract (as opposed to
  `docs/adr/`'s records of decisions this repo made), starting with
  `0001-oauth-signin-and-data-exchange.md`. Added in response to the `requested_permissions[]`
  and `granted_permissions`-hop bugs below, both of which shipped past a fully green test suite
  because nothing encoded the platform's actual observed behavior as a checked assertion.
- `PlatformTestApp.Tests/Auth/OAuthCallbackHandlersTests.cs` — the OAuth callback dispatch logic
  in `OAuthCallbackHandlers.cs` had no test coverage at all before this; now covers all four
  callback shapes, including a regression test for the `granted_permissions`-hop bug below.
- `docs/adr/0006-no-services-wrapper-for-newer-categories.md` — records, as a deliberate decision
  rather than a gap, that the nine newer API categories intentionally ship client-only with no
  `Platform.SDK.Services` wrapper.
- Clients for the nine remaining Bible Platform API categories that had no .NET coverage:
  `IAppClient`, `ILanguageClient`, `IFontClient`, `ILicenseClient`, `IOrganizationClient`,
  `IImageClient`, `IVerseOfTheDayClient`, `IPermissionClient` (OAuth-authenticated), and
  `ISearchClient` (query suggestions, unified/topic/verse/image search). Registered via
  `AddBibleApiClients`/`AddBibleOAuth` alongside the existing Bible/Passage/Highlight clients, with
  matching models in `Platform.API.Models`.

- Mutation-testing coverage (via Stryker.NET) for the files it previously found weakest:
  `CachingBibleClientTests`/`CachingPassageClientTests` (new; both decorators were previously at
  0% mutation score, every mutant `NoCoverage`) and an expanded `OAuthClientTests` covering
  `BibleOAuthClient`'s redirect-uri/scope wiring, `BuildDirectDataExchangeUrl` (previously almost
  entirely untested), the Data Exchange approval's status/Location check, and query-string
  parsing edge cases. `BibleOAuthClient.cs`'s mutation score moved from 52.27% to 67.05%; see
  `docs/adr/0002-caching-decorator-over-hybridcache.md` for the caching-side fix this also
  uncovered. Project-wide mutation score moved from 48.89% to 53.99% as a result. Also added a
  full `ServiceCollectionExtensionsTests` suite (client registration for all twelve typed clients,
  both `AddBibleApiClients` overloads, `AddBibleOAuth` null checks/options propagation/handler
  pipeline, and a previously-untested `AddBibleCaching`), moving `ServiceCollectionExtensions.cs`
  from 28.21% to 89.74% (the remaining survivors are confirmed-equivalent mutants — see commit
  history for the empirical verification). `FontClientTests` likewise grew to cover the
  `GetFontsAsync` request path and its null-body fallback, the exception messages both guard
  clauses produce, and — the one real bug risk the gap analysis surfaced — `GetStylesheetAsync`'s
  `fontId <= 0` guard, which had no test at all, leaving its boundary and its throw uncovered.
  `FontClient.cs` moved from 30.30% to 51.52%; every mutant that survives it is now either a
  `logger.LogDebug` template/statement mutation or `ConfigureAwait(false)` → `(true)`, neither of
  which is observable behavior in this codebase (all tests use `NullLogger`, and no host here
  installs a `SynchronizationContext`). Added `ApiRequestHelperTests.cs` — the shared
  `EnsureSuccessAsync`/`GetJsonAsync` helper every typed client calls through had no dedicated
  test file and relied entirely on incidental coverage from individual client tests; the new
  tests assert directly on the error-path `BibleApiException`'s status code, message text, and
  response body, and on the `configureRequest` overload actually applying per-request headers
  (e.g. `Accept-Language`) before the request is sent. `ApiRequestHelper.cs` moved from 33.33% to
  41.67%; the remaining survivors are the same two established non-issues (`ConfigureAwait`
  true/false equivalence and unasserted `logger.LogError` calls). `HighlightClientTests` grew to
  cover the null-passage/null-color guard clauses across `GetHighlightsAsync`,
  `CreateOrUpdateHighlightAsync`, and `ClearHighlightsAsync`; the null-body-returns-empty-list
  fallback for `GetHighlightsAsync`/`GetRecentColorsAsync`; the previously `NoCoverage`
  `BibleEmptyResponseException` thrown when `CreateOrUpdateHighlightAsync` gets an empty response
  body; and the previously `NoCoverage` `ToNormalizedUsfm` catch block, which wraps a
  `Reference.ToString()` failure as a `BibleApiException` — confirmed reachable (not
  defensive/dead code) via `Reference.FromString("GEN")`, a book-only reference whose `ToString()`
  throws `InvalidOperationException` by its own documented contract. `HighlightClient.cs` moved
  from 40.35% to 63.16%; the remaining survivors are the same two established non-issues.
  `OrganizationClientTests` grew to cover the previously-untested `pageToken`/`pageSize` guard
  clauses across all three methods (including the `pageSize == 0` boundary, which a weaker `< 0`
  check would have let through), the `Accept-Language` header actually being attached when
  provided, and exact (not just substring) query-string assertions for `GetOrganizationsAsync`'s
  and `GetOrganizationBiblesAsync`'s URL builders — the prior substring (`.Contain(...)`)
  assertions let query-separator bugs (`?` vs `&` placement) survive undetected.
  `OrganizationClient.cs` moved from 44.09% to 84.95%; the lone non-logging/non-ConfigureAwait
  survivor (`BuildOrganizationBiblesUrl`'s first separator check) is a mathematically equivalent
  mutant — `hasQuery` is provably `false` at that call site on every reachable path, since it's
  the first possible separator-insertion point in the method. Added a dedicated
  `OAuthTokenResponseTests.cs`, consolidating and extending the handful of `OAuthTokenResponse`
  tests that previously lived incidentally inside `OAuthClientTests.cs`: direct tests for
  `IsExpired`'s JWT-`exp`-claim fallback path, including which of `IdToken`/`AccessToken` takes
  precedence when both carry an `exp` claim and the buffer-seconds boundary around it; the
  previously `NoCoverage` early-return when `AccessToken` is null/empty/whitespace; and full
  precedence coverage for the 4-way claim-fallback chains in `GetUserName()`, `GetEmail()`, and
  `GetDisplayIdentity()` (each claim source tested in isolation plus the priority ordering between
  adjacent sources, since none of those branches had dedicated tests before — only incidental,
  partial coverage via two higher-level `GetDisplayIdentity` tests). `OAuthTokenResponse.cs` moved
  from 33.90% to 96.61%; the only two survivors are `>=` → `>` equality mutations on the two
  expiration-boundary comparisons, both empirically confirmed equivalent — `DateTimeOffset.UtcNow`
  is a live, monotonically-advancing clock with no injectable abstraction in this type, so by the
  time it's read inside `IsExpired`, it has always already advanced strictly past any
  previously-computed threshold, making exact equality unreachable in a real test. Remaining weak
  spots are concentrated in `OAuthBearerTokenHandler.cs` (~39%), `InMemoryTokenProvider.cs`
  (~42%), `VerseOfTheDayClient.cs` (~44%), `AppClient.cs` (~46%), and `PermissionClient.cs` (50%)
  — not yet addressed.

### Changed
- Relicensed from MIT to Apache License 2.0 (all five packages, including
  `BiblePlatform.UsfmReferences`). See `LICENSE` and the new `NOTICE` file.
- Repositioned this repository as a clone-and-build reference implementation rather than an
  installable SDK, and removed remaining "YouVersion"/"Life.Church" branding from identifiers,
  doc comments, and messages (factual/technical references — API URLs, header names, JSON
  property names, the required non-affiliation disclaimer — are unaffected).
- CI now merges every test project's coverage output and fails the build if merged line coverage
  drops below 65%, instead of only publishing a coverage summary with no enforced threshold. See
  the "Enforce coverage floor" step in `.github/workflows/ci.yml`.

### Fixed
- `CachingBibleClient.GetIndexAsync` (backing `GetBooksAsync`/`GetChaptersAsync`/`GetVersesAsync`)
  was wired to `BibleCacheOptions.VersionsTtl` instead of `BooksTtl`, making `BooksTtl` dead
  config. Found via mutation testing — both caching decorators had zero test coverage before this.
  See `docs/adr/0002-caching-decorator-over-hybridcache.md`.
- `IBibleOAuthClient.CompleteIdentityCallbackAsync` sent identity fields (`yvp_id`, `user_name`,
  `user_email`, `profile_picture`) from the OAuth callback redirect back to the platform to
  retrieve a code. Following the platform's 2026-07-20/21 sign-in change, the callback redirect no
  longer carries those fields at all — only `state` — so that method could never succeed. Replaced
  it with `CompleteAuthorizationCallbackAsync(state, codeVerifier)`, which replays `state` alone to
  `AuthCallbackEndpoint` and returns the new `AuthorizationCallbackResult` (the redeemed token, plus
  `GrantedPermissions` distinguishing "not requested" from "requested and empty"). User identity is
  now read from the token's `id_token` claims instead of the callback query string. Updated
  `PlatformTestApp`'s callback handling and `docs/oauth-guide.md` to match.
- `Platform.API/README.md`'s install snippet showed a stale `Version="1.0.0"` that doesn't exist on
  the feed (actual published version was `0.1.0`), which would cause a restore failure for anyone
  copying it as-is.
- The "Additional docs" links (Getting started / Authentication / OAuth guide) on
  `Platform.API/README.md` were relative markdown links that resolve fine on GitHub but render as
  dead links on NuGet.org's README renderer. Converted to absolute GitHub URLs; same fix applied to
  the top-level `README.md`'s OAuth/PKCE guide link. `docs/getting-started.md` and
  `docs/authentication.md` were also missing entirely (the "Getting started" and "Authentication"
  links pointed at files that had never been created) — both added.
- `BuildAuthorizationUrl`/`BuildDirectDataExchangeUrl` sent requested Data Exchange permissions as
  a bare `requested_permissions=highlights` query parameter. The platform requires the array-bracket
  form (`requested_permissions[]=highlights`) and silently ignores the unbracketed name, so the
  consent UI never showed the permission and the resulting callback never carried
  `granted_permissions` — a sign-in would succeed but a subsequent "grant highlights access" request
  made during that same sign-in redirect would never actually be granted.

## [0.1.0] - 2026-07-14

### Added
- `IYouVersionOAuthClient.BuildAuthorizationUrl` now accepts an optional `requestedPermissions`
  parameter, appended to `/auth/authorize` as repeated `requested_permissions=...` query
  parameters. This shows the Data Exchange consent UI as part of the sign-in redirect itself,
  rather than requiring a separate `RequestPermissionsAsync` + `BuildDataExchangeApprovalUrl`
  round trip after sign-in completes; the result comes back as `granted_permissions` on the same
  callback that carries `code`/`state`. This is the recommended way to request a resource
  permission (e.g. `highlights`) at sign-in time — `RequestPermissionsAsync`/
  `BuildDataExchangeApprovalUrl` remain for requesting a permission later, without a full
  sign-in round trip.
- `IYouVersionOAuthClient.ParseDataExchangeCallback` — parses the `data_exchange_status`,
  `granted_permissions`, `denied_permissions`, `error`, and `error_description` query parameters
  YouVersion appends to the app's callback URL after the Data Exchange approval page, returning a
  typed `DataExchangeCallbackResult`/`DataExchangeStatus`.
- `IYouVersionOAuthClient.CompleteDataExchangeApprovalAsync(dataExchangeToken)` — completes a Data
  Exchange approval via `POST /data-exchange?token={token}` using a previously-issued
  `RequestPermissionsAsync` token, for confidential clients that can skip the browser-rendered
  approval page. Requires the OAuth `HttpClient` to have `AllowAutoRedirect` disabled so the `303`
  response's `Location` header can be read directly.
- Data Exchange documentation section in `Platform.API/README.md` covering the full
  request-token → approval-page → callback flow.

### Fixed
- The Data Exchange approval page never appeared during `PlatformTestApp`'s sign-in flow.
  Compounding causes: (1) `PlatformTestApp` was missing `requested_permissions` on
  `/auth/authorize` entirely, which is what actually triggers the consent UI during sign-in (see
  the `Added` entry for `BuildAuthorizationUrl`'s new `requestedPermissions` parameter);
  (2) `Results.Redirect` was called with `Uri.ToString()` instead of `Uri.AbsoluteUri` on the
  authorization/approval URLs in `PlatformTestApp/Program.cs` — `.ToString()` un-escapes
  percent-encoded characters for display (e.g. turning `scope=openid%20profile%20email` back into
  a literal, RFC-invalid `scope=openid profile email` with raw spaces), silently corrupting the
  outbound redirect. Note: in live testing, the completed consent grant has been observed coming
  back both as `granted_permissions` on the same callback as `code` *and* as a separate
  `data_exchange_status` callback (the same shape `ParseDataExchangeCallback`/
  `BuildDataExchangeApprovalUrl` use) — `PlatformTestApp` handles both; don't remove either path
  without confirming via a real completed sign-in that it's unreachable.
- `BuildDataExchangeApprovalUrl` was missing the app key entirely. The Data Exchange approval
  page is a top-level browser redirect and can't carry the `X-YVP-App-Key` header, so the app key
  must be sent as an `x-yvp-app-key` query parameter instead — the URL would otherwise fail to
  resolve the calling app. `YouVersionOAuthClient` now reads `YouVersionApiOptions.AppKey` for
  this (and throws `InvalidOperationException` if it isn't configured).
- `RequestPermissionsAsync` (`POST /data-exchange/token`) wasn't sending `x-yvp-app-key` either,
  even though the API reference lists it as an accepted query parameter for resolving the calling
  app on that endpoint; it's now included alongside the existing bearer token.
- `CompleteDataExchangeApprovalAsync` sent a `requested_permissions` JSON body and an
  `Authorization` bearer header to `POST /data-exchange` — neither matches the documented
  contract. That endpoint takes **no request body**; the permissions granted are whatever was
  fixed when the Data Exchange token was created via `RequestPermissionsAsync`. The method now
  takes that token directly (`CompleteDataExchangeApprovalAsync(dataExchangeToken)`) and sends it
  as the `token` query parameter, matching `POST /data-exchange?token={token}`. **Breaking change**
  to the (unreleased) method signature.
- `PlatformTestApp`'s `/auth/callback-complete` swallowed any failure from
  `RequestPermissionsAsync`/`BuildDataExchangeApprovalUrl` and silently redirected to the same URL
  as a successful sign-in (`/?auth_mode=code`), so a broken Data Exchange request looked identical
  to the user never being asked for `highlights` at all. The error now reaches the `oauth_error`
  query parameter the home page already renders.
- Cross-user OAuth token leakage: `PlatformTestApp` now registers a scoped, session-backed
  `SessionTokenProvider` before calling `AddYouVersionOAuth`, instead of relying on the library's
  default singleton `InMemoryTokenProvider` (which is documented as console/test-only).
- `IHighlightClient`'s HTTP pipeline was missing `OAuthBearerTokenHandler`, causing authenticated
  highlight requests to fail silently.
- OAuth token exchange used a relative URI against an `HttpClient` with no `BaseAddress`.
- `InMemoryTokenProvider` is now thread-safe (`SemaphoreSlim`-guarded) and `IDisposable`.
- `OAuthBearerTokenHandler` now single-flights concurrent token refreshes instead of firing a
  separate refresh call per in-flight request at token expiry.
- `Verse.cs` used the incorrect namespace `Platform.API.Models.Models`; corrected to
  `Platform.API.Models`.
- `BibleVersionSummary.Copyright` was missing `[JsonPropertyName("copyright")]` and likely never
  deserialized from real API responses.
- `YouVersion.UsfmReferences` package metadata incorrectly attributed copyright to "YouVersion";
  corrected to reflect it as an unofficial, independently-authored port.

### Changed
- `YouVersionApiException` no longer overloads `HttpStatusCode.OK`/`NotFound` to mean "empty body" —
  introduced a dedicated `YouVersionEmptyResponseException` for that case.
- `Reference`/`VerseRange` construction centralized in `PassageService` instead of being duplicated
  across `BibleReader.razor.cs`, `PassageDisplay.razor.cs`, and `CustomReader.razor`.
- The synthetic unsigned-JWT query-parameter sign-in shortcut in `PlatformTestApp` is now gated
  behind `IsDevelopment()`.

### Removed
- `Microsoft.FluentUI.AspNetCore.Components` (+ `.Emoji`/`.Icons`) dependency from
  `Platform.SDK.Components` — the SDK's Bible components never used any Fluent tags; the dependency
  moved to `PlatformTestApp`, the actual consumer of Fluent's toast/dialog/tooltip providers.
- Dead code: `OAuthCallback.razor` (superseded, unreachable OAuth callback duplicate), `PassageDisplay`
  component (broken, unreferenced), empty `BibleVerseComponent.*` scaffolding, `Areas/MyFeature`
  Razor Class Library template page, unused `VerseComponent.razor.js`, five unused `[Parameter]`s
  on `VersePicker`, and the default `Counter.razor`/`Weather.razor` scaffold pages (with their
  `NavMenu` links).
- `RichardSzalay.MockHttp` test dependency (unused) and consolidated duplicate hand-rolled
  `CapturingHandler` test fakes into one shared implementation.

### Added
- `Platform.API.Tests/Architecture/ApiClientBoundaryTests.cs` — architecture-fitness test enforcing
  that `PlatformTestApp`/`Platform.SDK.Components` never reference `Platform.API` client types
  directly.
- Integration test resolving `IHighlightClient` from DI and asserting a real `Authorization` header
  reaches the outgoing request.
- `[Required]` validation on `YouVersionOAuthOptions.ClientId`; both API-client and OAuth options
  now use `AddOptions<T>().ValidateOnStart()` so misconfiguration fails at startup, not mid-request.
