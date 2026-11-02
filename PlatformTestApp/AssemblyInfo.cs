using System.Runtime.CompilerServices;

// OAuthCallbackHandlers (and the top-level-statement Program class it logs through) are internal
// by default; exposing them to the test project lets PlatformTestApp.Tests exercise the callback
// dispatch logic directly instead of only through the already-public SessionTokenProvider/
// CircuitSessionKeyAccessor types.
[assembly: InternalsVisibleTo("PlatformTestApp.Tests")]
