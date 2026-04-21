<!--
Thanks for contributing to Zona-14. Before you submit:
- Skim CONTRIBUTING.md (especially §2 on _Zona14/ and §3 on // Zona14: markers).
- Follow the SS14 PR guidelines:
  https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html
- For general discussion, playtests, or uploading large media files, join the
  Zona-14 Discord: https://discord.gg/57S48NzbZ9
-->

## About the PR
<!-- What does this PR change? -->

## Why / Balance
<!-- Why are you making this change? Link discussions, issues, or design notes.
Justifications that only restate *what* the PR does are not enough — explain
the problem it solves or the effect on gameplay. -->

## Technical details
<!-- High-level summary of the code changes, for reviewers. Makes review faster. -->

## Media
<!--
Attach before/after screenshots, GIFs, or short videos of the change in-game.
Small files (≤10 MB) can be dropped directly into this text area.

For larger videos, upload them to the Zona-14 Discord (https://discord.gg/57S48NzbZ9)
and paste the message link here.

Tips:
- A side-by-side "before / after" GIF is worth a hundred words of testing notes.
- For UI changes, include both themes / zoom levels you tested.
- For audio changes, a short clip (or an uploaded .ogg link) is fine.
-->

## Upstream source (if this is a port)
<!-- Link to the upstream PR / commit. Omit if this is greenfield Zona-14 work. -->

## Testing
<!-- How did you verify this works in-game? Servers joined, rounds played,
edge cases exercised. -->

## Requirements
<!-- Place an X in the brackets (no spaces, like [X]) to confirm each item. -->

- [ ] New files live under a `_Zona14/` folder (or this PR is a pure upstream port/merge — explain in *Upstream source* and tag the PR title `[upstream-port]`).
- [ ] New C# namespaces match the `Content.<project>._Zona14.<Feature>.<Sub>` pattern.
- [ ] Edits to files **outside** `_Zona14/` carry `// Zona14:` (or `# Zona14:`) markers — see `CONTRIBUTING.md` §3.
- [ ] New or modified sprites / assets have a `meta.json` with populated `license` (SPDX) and `copyright` fields (see `CONTRIBUTING.md` §6); no existing `license` / `copyright` fields were removed.
- [ ] I have read and am following the [SS14 pull-request guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html).
- [ ] Media is attached — or marked **N/A** with one line explaining why (e.g., "server-only refactor, no visual change").
- [ ] CI is green — `Zona14 convention check`, `Build and Test (Debug)`, linters, validators.

## Breaking changes
<!--
List any breaking changes and provide migration notes. Breaking changes include:
- Public API changes (fields, methods, class renames, namespace moves).
- YAML data-field renames or removals.
- Prototype ID renames or deletions (even if migrated).
- Changes to component access restrictions or friend groups.

Omit this section if there are no breaking changes.
-->

## Changelog

<!--
Add a changelog entry for gameplay-visible changes. After the PR is merged, a
maintainer runs the changelog merger and your entry appears in the in-game
"Zona 14" tab. Skip this section (delete the :cl: block below) for docs-only,
CI, or pure-refactor PRs.

Default category is Zona 14 (this repo's own tab). Prefix subsequent lines with
ADMIN:, MAPS:, or RULES: to route those entries to the respective tabs.

Types: add | remove | tweak | fix | bug (alias for fix). Author defaults to
your GitHub username — override by putting a name after :cl: on the same line.

Writing tips: complete sentences, start with a capital, end with a period,
present active voice, no jargon. See
https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html#writing-an-effective-changelog
-->

:cl:
- add: 
- fix: 
- tweak: 
- remove: 
