# 6. No `Platform.SDK.Services` wrapper for the nine newer API categories

## Status

Accepted

## Context

`Platform.SDK.Services` wraps `Platform.API` clients only where the wrapper adds real value —
friendlier exceptions, combining multiple calls, typed-reference overloads (`AuthSessionService`,
`BibleReaderStateService`, `BookService`, `ChapterService`, `HighlightService`, `PassageService`,
`VersionService`). The nine categories added to close the original platform-coverage gap (Apps,
Languages, Fonts, Licenses, Organizations, Images, VerseOfTheDay, Permissions, Search) ship with a
`Platform.API` client/model pair each, but no corresponding `Platform.SDK.Services` wrapper.

Without a record of this, the asymmetry reads as an oversight on every future audit of the
project — exactly the kind of ambiguity a stale checklist (`ComplianceToDoList.md`, before its
2026-11-01 correction) has already shown can erode trust in this repo's own documentation.

## Decision

Treat "no Services wrapper" as the default for a new client, not a gap to backfill. A wrapper is
added only when it would do one of the things a pure pass-through can't: normalize exceptions
(`HighlightAccessDeniedException`), combine multiple client calls into one operation, or accept a
typed domain reference instead of raw ids/strings. None of the nine newer categories currently
need any of that — each is either a single read-only endpoint or a small set of them with no
cross-call composition a caller couldn't do itself just as easily against the `Platform.API`
client directly.

## Consequences

- Consumers of the nine newer categories call `Platform.API` clients (`IImageClient`,
  `ISearchClient`, etc.) directly, same as any app that doesn't need the UI-facing abstraction
  `Platform.SDK.Services` exists for.
- If a future consumer need surfaces that a pass-through can't satisfy (e.g. `VerseOfTheDayService`
  resolving an `image_id` through `IImageClient` in one call, floated but not committed to when
  these categories were first built), add the wrapper then, scoped to that specific need, rather
  than wrapping all nine preemptively.
- This decision is specific to these nine categories as of 2026-11-01; it doesn't forbid adding a
  Services wrapper for a *future* category where one is actually warranted.
