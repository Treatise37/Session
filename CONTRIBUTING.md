# Contributing to Zona-14

Welcome — thanks for helping build Zona-14. This guide is the contract between contributors and the project: if you follow it, your PR should sail through review. The automated `Zona14 convention check` workflow enforces most of what's below.

## 1. Project lineage

Zona-14 is an English-direction fork of Space Station 14. The chain:

- [space-wizards/space-station-14](https://github.com/space-wizards/space-station-14) — upstream SS14.
- [space-syndicate/space-station-14](https://github.com/space-syndicate/space-station-14) — Russian mainline SS14 fork.
- [stalker14-project/stalker14](https://github.com/stalker14-project/stalker14) — S.T.A.L.K.E.R.-themed derivative (our direct parent; Russian).
- **Zona-14** — this repo. English-direction.

We merge from `stalker14-project` regularly. Expect PRs to contain both upstream ports and Zona-14-specific work; the conventions below make the two kinds of changes easy to tell apart.

## 2. The `_Zona14/` rule

**New Zona-14 code lives under a `_Zona14/` folder.** This applies to every project tree where a `_Zona14/` folder exists:

- `Content.Server/_Zona14/`
- `Content.Client/_Zona14/`
- `Content.Shared/_Zona14/`
- `Content.IntegrationTests/Tests/_Zona14/`
- `Resources/Prototypes/_Zona14/`
- `Resources/Maps/_Zona14/`
- `Resources/Locale/en-US/_Zona14/`
- `Resources/Locale/ru-RU/_Zona14/`
- `Resources/Textures/_Zona14/`
- `Resources/Audio/_Zona14/`
- `Resources/ServerInfo/_Zona14/`
- `Resources/ConfigPresets/_Zona14/`

Inside a `_Zona14/` folder, mirror the upstream feature-driven layout (`_Zona14/Atmos/Components/…`, `_Zona14/Cargo/Systems/…`) rather than grouping by type.

### Namespace (C#)

A file at `Content.<project>/_Zona14/<Feature>/<Sub>/File.cs` declares:

```csharp
namespace Content.<project>._Zona14.<Feature>.<Sub>;
```

**Worked example.** A new anomaly component at `Content.Shared/_Zona14/Anomalies/Components/StalkerAnomalyComponent.cs`:

```csharp
using Robust.Shared.GameStates;

namespace Content.Shared._Zona14.Anomalies.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StalkerAnomalyComponent : Component
{
    [DataField]
    public float FlickerRate = 0.5f;
}
```

## 3. Upstream edits: the `// Zona14:` marker

When you edit a file **outside** a `_Zona14/` folder (an upstream SS14 or stalker14 file), mark every logical change inline. This makes Zona-14 modifications easy to spot during future upstream merges.

Forms:

- **Single line** — `// Zona14: short reason`:
  ```csharp
  public bool Inverted; // Zona14: if true, Species list is a blacklist
  ```
- **Value swap** — `// Zona14: OLD<NEW`:
  ```csharp
  public const int MaxPlayers = 100; // Zona14: 50<100
  ```
- **Multi-line block** — `// Zona14: reason` opens, `// End Zona14` closes:
  ```csharp
  // Zona14: custom stalker loadout validation
  if (profile.Species == "Stalker" && !StalkerLoadoutCheck(profile))
      return false;
  // End Zona14
  ```
- **Added `using`** — trailing `// Zona14`:
  ```csharp
  using Content.Shared._Zona14.Anomalies.Components; // Zona14
  ```

### YAML and Fluent (`.ftl`) edits

Same rule with `#` comments — `# Zona14:` / `# End Zona14`.

```yaml
- type: entity
  id: SomeUpstreamEntity
  components:
  - type: HealthAnalyzer
    scanDelay: 0.8 # Zona14: 1.2<0.8
```

### Upstream-port escape hatch

If the PR is a pure merge or port from `stalker14-project` (no new Zona-14 logic), include `[upstream-port]` in the PR title. The validator skips the marker check for that PR. Do not abuse this — use it only for genuine upstream syncs.

## 4. YAML / prototype convention

- **No mandatory entity-ID prefix.** Folder isolation (`Resources/Prototypes/_Zona14/…`) is the contract; the folder path tells you the entity is Zona-14.
- Optional `Zona14` prefix is fine when the entity's fork-provenance matters at a glance (e.g., `Zona14CargoConsole`).
- Keep file names feature-scoped, not type-scoped (`anomalies.yml`, not `entities.yml`).

## 5. Licensing (code)

The repo has layered licensing. Nothing conflicts — it stacks:

- **Upstream code** (Space Wizards, Corvax) is **MIT**. Preserved verbatim; nobody is relicensing that.
- **Stalker-team contributions** (the `stalker14-project` authors listed at the top of `LICENSE.TXT`) are marked **All rights reserved**. That clause binds their code wherever it lives; contact the Stalker14 team to reuse it.
- **Zona-14 team contributions** — everything under `_Zona14/` — is **MIT** © 2024-2026 Zona-14 Team. By opening a PR that adds files under `_Zona14/`, you agree your contribution is licensed under the Zona-14 MIT terms in `LICENSE.TXT`.

A broader legal review of the Stalker-team "All rights reserved" clause is **pending**. Flag questions to the team; don't try to resolve them in code.

## 6. Licensing (assets — sprites, audio, maps)

**Every sprite `.rsi` directory, and every standalone asset with a `meta.json`, requires non-empty `license` and `copyright` fields.** The CI validator fails any PR that adds or modifies a `meta.json` without them.

Allowed `license` values (SPDX identifiers):

- `CC-BY-SA-3.0` — SS14 default; use this unless you have a specific reason otherwise.
- `CC-BY-SA-4.0`
- `CC-BY-4.0`
- `CC0-1.0`
- `OFL-1.1`
- `Apache-2.0`
- `MIT`

Anything else requires `[custom-license]` in the PR title plus a justification in the PR body.

**Template** — `Resources/Textures/_Zona14/Anomalies/flicker.rsi/meta.json`:

```json
{
  "version": 1,
  "license": "CC-BY-SA-3.0",
  "copyright": "Made by <contributor handle> for Zona-14, 2026",
  "size": { "x": 32, "y": 32 },
  "states": [{ "name": "icon" }]
}
```

### Reusing a sprite from another fork

Copy its `license` and `copyright` values **verbatim**. Note the source in the PR description (e.g., "Ported from `space-wizards/space-station-14@<sha>` — `Resources/Textures/…/crowbar.rsi`.").

### Editing an existing sprite

**Never remove `license` or `copyright` fields.** Augment the attribution:

```json
"copyright": "Made by Alice for SS14, 2022 — modified by Bob for Zona-14, 2026"
```

The validator fails the PR if a `license` or `copyright` field was present on `base` and is removed or emptied on `head`.

### Audio

`.ogg` files that ship with a `meta.json` follow the same rule. `.ogg` files without a `meta.json` need their license declared in the PR description and recorded in an adjacent `README.md` or attribution file.

## 7. Branch / PR conventions

- **Target branch**: `master`.
- **PR title**: short imperative, one line. Include `[upstream-port]` for pure merges from `stalker14-project`. Include `[custom-license]` if any asset uses a license outside the allowlist.
- **PR body**: fill in the `.github/PULL_REQUEST_TEMPLATE.md` sections. Include media (screenshots / GIFs / video) for anything visible in-game; upload larger videos to the [Zona-14 Discord](https://discord.gg/57S48NzbZ9) and link them.
- **PR behavior** follows the [SS14 pull-request guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html): separate PRs for features / bug fixes / refactors; test your change in-game before opening the PR; don't use the GitHub web editor; don't force-push after a reviewer has left comments.

## 8. Commit style

- English is preferred for new Zona-14 work.
- Russian is fine for merges / direct ports from `stalker14-project`.
- No Conventional-Commits requirement in v1 — write descriptive messages.

## 9. Code style & upstream SS14 standards

Zona-14 follows the upstream Space Wizards' Den coding standards. Read and apply these as a prerequisite for any PR that touches C# or YAML — most review nits will trace back to one of them:

- [SS14 codebase info](https://docs.spacestation14.com/en/general-development/codebase-info.html) — landing page for the full conventions tree.
- [SS14 conventions](https://docs.spacestation14.com/en/general-development/codebase-info/conventions.html) — naming, comments, ECS rules (components hold *only* data; systems hold logic; events are struct `[ByRefEvent]`s named `…Event` with `OnXEvent` handlers), XAML/UI, performance, `TimeSpan` / field-deltas, YAML conventions, localization, in-/out-of-simulation split. This is the primary document.
- [SS14 codebase organization](https://docs.spacestation14.com/en/general-development/codebase-info/codebase-organization.html) — project split (Client / Shared / Server), file layout, prototype organization (`base.yml` + per-type files; no `misc/` folders).
- [SS14 pull-request guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html) — PR hygiene (separate PRs for features / bug fixes / refactors, test in-game, no web edits, don't force-push after reviews). See §7 below for how Zona-14 applies these.
- [SS14 style guide](https://docs.spacestation14.com/en/general-development/codebase-info/style-guide.html) — C# formatting.

Local rules on top of upstream:

- `.editorconfig` enforces 4-space indent, 120-char line limit, trim trailing whitespace, no CRLF (these match upstream).
- Zona-14 adds no new stylistic rules of its own in v1. Propose changes via Discord before adding rules.

**One documented exception to upstream.** SS14's `codebase-organization` says "game-code folders live directly under `Content.Client/Shared/Server`." Zona-14 overrides that for **new fork code only** — new Zona-14 code goes under `_Zona14/` per §2 above. Upstream files edited in place still follow upstream layout and carry `// Zona14:` markers per §3.

## 10. CI checks

The `Zona14 convention check` workflow runs on every PR. It enforces:

1. **Namespace–folder alignment** — files under `Content.<project>/_Zona14/…` must declare the matching namespace.
2. **Upstream-edit markers** — files edited outside `_Zona14/` must have `// Zona14` (or `# Zona14`) markers in the added hunks (skipped if the PR is tagged `[upstream-port]`).
3. **Misfiled namespace** — `.cs` files outside `_Zona14/` may not declare a `_Zona14.*` namespace.
4. **Greenfield warning** — newly added `.cs` or `.yml` files outside `_Zona14/` produce a warning (non-fatal); reviewers decide.
5. **Key-file delete guard** — protects `README.md`, `README.ru.md`, `LICENSE.TXT`, `CONTRIBUTING.md`, `.github/PULL_REQUEST_TEMPLATE.md`.
6. **Asset `meta.json` license/copyright** — every `meta.json` under `Resources/` (added or modified) must have populated `license` (SPDX identifier on the allowlist) and `copyright` fields; license removals on edits also fail.

### Running the check locally

```bash
bash Tools/_Zona14/check-conventions.sh origin/master HEAD
```

Requirements: `git`, `grep`, `awk`, `jq`. Install `jq` with `sudo apt install jq` (Ubuntu/Debian) or `brew install jq` (macOS).

## 12. Changelog

Zona-14 ships its own in-game changelog tab — **Zona 14** — alongside the inherited upstream, rules, maps, and admin tabs. It's populated from `:cl:` blocks in PR bodies, merged into `Resources/Changelog/Zona14.yml` by a maintainer after the PR lands.

### PR body syntax

```
:cl: <optional author override>
- add: Added a new stalker artifact.
- fix: Fixed anomaly flicker at low light levels.
- tweak: Reduced bandage application time.
- remove: Removed the broken handheld scanner.
```

- Types: `add` / `remove` / `tweak` / `fix`. `bug` and `bugfix` are aliases for `fix`.
- Empty entries (`- add:` with no message) are silently dropped by the merger.
- Author defaults to your GitHub username. Put a display name after `:cl:` on the same line to override.

### Category prefixes

By default entries land in the **Zona 14** tab. Prefix later lines with a category to route them elsewhere:

```
:cl:
- add: Added a new stalker artifact.

ADMIN:
- add: Added an admin verb to force-ghost a player.

MAPS:
- tweak: On Delta, moved engineering locker closer to power.

RULES:
- tweak: Clarified rule 4 around IC/OOC boundaries.
```

Recognised categories: `ADMIN:` → `Admin.yml`, `MAPS:` → `Maps.yml`, `RULES:` → `Rules.yml`. Unknown categories are silently ignored (entries fall back to the previous category), so a typo just sends the entries to Zona 14.

### When to skip the `:cl:` block

Omit it (or leave all entries empty) for:
- Docs / comment changes.
- CI / tooling.
- Pure refactors with no gameplay impact.
- Upstream ports (the `[upstream-port]` tag in the PR title already tells the validator this is a merge; no changelog needed).

Gameplay-visible changes (new items, balance tweaks, bug fixes that affect play) should have a `:cl:` entry. It's optional but strongly encouraged.

### Writing effective entries

Defer to SS14's [effective-changelog rules](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html#writing-an-effective-changelog):

1. Complete, grammatically correct sentences. Start with a capital, end with a period.
2. Log only changes with significant in-game impact.
3. Present, active voice.
4. Be concise. Avoid IC flavor / RP jargon.
5. Set the appropriate tone.

### Maintainer merge workflow

After merging a PR, a maintainer runs the manual merger documented in [`Tools/_Zona14/changelog/README.md`](Tools/_Zona14/changelog/README.md). Automation (a dedicated webhook bot) is planned; the manual flow covers the gap until then.

## 11. Where to discuss

- **Bug reports, player feedback, feature requests**: the public [Zona-14-Feedback](https://github.com/Zona-14/Zona-14-Feedback) repo. Anyone can open an issue there — it's the canonical channel for community-facing reports. Please don't file these on Discord.
- **Community, news, updates, playtests, media uploads for large PR videos**: [Zona-14 Discord](https://discord.gg/57S48NzbZ9).
- **Code changes**: GitHub Pull Requests on this repo.
- **Internal dev-tracking issues**: this repo's Issues tab is reserved for maintainer-tracked work items (refactor todos, ported-PR tracking, CI tasks). User-facing reports still belong in Zona-14-Feedback.
