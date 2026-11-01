# BiblePlatform.API.Models

Part of the [Bible Platform SDK for .NET](../README.md).

Domain model types for the [Platform REST API](https://developers.youversion.com).

This package is a zero-dependency, pure-POCO library - no external NuGet dependencies required.
All types are immutable record types with System.Text.Json property-name attributes.

## Included types

| Type | Description |
|---|---|
| BibleVersionSummary | Lightweight version item returned by the list endpoint |
| BibleVersion | Full version metadata including available books |
| Book | A book within a Bible version (USFM code + human name) |
| Chapter | A chapter within a book |
| Verse | A single verse identifier |
| BibleIndex | Full book/chapter/verse structure for a version, as returned by `GET /v1/bibles/{id}/index` — the authoritative source for real, per-version counts |
| IndexBook | A book's structural position within a `BibleIndex` (title, canon, chapters, optional intro section) |
| IndexChapter | A chapter's structural position within an `IndexBook` |
| IndexVerse | A verse's structural position within an `IndexChapter` (no scripture text — use `Passage` for content) |
| IndexSection | A named, non-chapter section of a book (e.g. an introduction) |
| BookCanon | Enum identifying which scriptural canon a book belongs to (Old Testament, New Testament, Deuterocanon) |
| Passage | Scripture content returned by a passage fetch |
| PassageRequestOptions | Options controlling format, headings, and footnotes |
| PassageFormat | Text or Html enum |
| Highlight | A user Bible verse highlight — identified by (BibleId, PassageId), Color is a hex string |
| App | App metadata (id, name, description, website, status) |
| Language | Language metadata, including display names and script/writing-population data |
| Font, FontVariant, FontSource | Font family metadata and its variant/source files |
| License | A Bible content license, including agreed/held state when applicable |
| Organization, OrganizationAddress, AddressComponent | Publisher/organization metadata and address |
| OrganizationBible | A Bible version published by an organization |
| VerseImage | Verse-art image metadata (URL, passage, dimensions, attribution) |
| VerseOfTheDay | A single day's verse-of-the-day entry (passage, optional image) |
| AppPermissions | Permissions a signed-in user has granted to an app |
| SearchQuerySuggestion | An autocomplete or trending search query suggestion |
| SearchVerse, SearchTopic, SearchImage | Individual items returned by search results |
| UnifiedSearchResult, TopicSearchResult, VerseSearchResult, ImageSearchResult | Combined/per-category search result envelopes |
| PagedResult(T) | Generic paged envelope with Data, NextPageToken, and optional TotalSize |

## Target framework

net10.0

## Installation

Not published as a package — usually referenced transitively via `Platform.API`. To reference it
directly, clone this repo and add a `ProjectReference`: see
[Referencing this repo locally](../README.md#referencing-this-repo-locally) in the solution
README.

## Attribution

Always display the version Copyright field alongside Passage.Reference when showing Bible text.

## Related packages

This package has no dependencies on other packages in this SDK. It's consumed by:

- [`BiblePlatform.API`](../Platform.API/README.md) — typed HTTP clients that return these model types.
- [`BiblePlatform.SDK.Services`](../Platform.SDK.Services/README.md) — business-logic services built on top of `Platform.API`.
- [`BiblePlatform.SDK.Components`](../Platform.SDK.Components/README.md) — Blazor components that render these types.

See the [solution README](../README.md) for the full package list and architecture overview.
