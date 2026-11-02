# Platform wire-contract specs

Short records of the Bible Platform API's *actual observed wire behavior* — exact endpoint, exact
query/body parameter names (including bracket/casing conventions), which hop in a multi-step flow
carries which field, and example request/response shapes pulled from real traffic. These sit
alongside [`docs/adr/`](../docs/adr/README.md) but record a different kind of thing: an ADR
captures a *decision this repo made*; a spec here captures *what the platform does*, independent of
any decision — the thing that was wrong twice in the same OAuth flow before these specs existed
(see spec 0001's History section).

New specs are numbered sequentially and never renumbered or deleted, even if superseded — a
superseded spec should say so and link to the one that replaces it.

Status values:

- **Verified** — confirmed against live traffic (a real browser round trip, a `curl`/`WebFetch`
  against the live docs or API, or equivalent), not just the published documentation's prose.
- **Assumed** — based on documentation only; not yet confirmed against live traffic. Treat as
  lower-confidence than `Verified` and re-check before relying on it for anything load-bearing.
- **Superseded** — no longer current; the record says why and links to its replacement.

A spec is only as trustworthy as its last verification. If you observe the platform behaving
differently from what a `Verified` spec claims, that's platform drift — update the spec (bump its
status back to `Assumed` until re-confirmed, or correct it outright) in the same change that fixes
the code, not after.

| # | Title | Status |
|---|---|---|
| [0001](0001-oauth-signin-and-data-exchange.md) | OAuth sign-in and Data Exchange permission flow | Verified |
