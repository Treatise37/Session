# Contributing to Zona-14

Welcome — thanks for helping build Zona-14. This guide is the contract between contributors and the project: if you follow it, your PR should sail through review. The automated `Zone14 convention check` workflow enforces most of what's below.

## 1. Project lineage

Zona-14 is an English-direction fork of Space Station 14. The chain:

- [space-wizards/space-station-14](https://github.com/space-wizards/space-station-14) — upstream SS14.
- [space-syndicate/space-station-14](https://github.com/space-syndicate/space-station-14) — Russian mainline SS14 fork.
- [stalker14-project/stalker14](https://github.com/stalker14-project/stalker14) — S.T.A.L.K.E.R.-themed derivative (our direct parent; Russian).
- **Zona-14** — this repo. English-direction.

We merge from `stalker14-project` regularly. Expect PRs to contain both upstream ports and Zona-14-specific work; the conventions below make the two kinds of changes easy to tell apart.

## 2. The `_Zone14/` rule

**New Zona-14 code lives under a `_Zone14/` folder.** This applies to every project tree where a `_Zone14/` folder exists:

- `Content.Server/_Zone14/`
- `Content.Client/_Zone14/`
- `Content.Shared/_Zone14/`
- `Content.IntegrationTests/Tests/_Zone14/`
- `Resources/Prototypes/_Zone14/`
- `Resources/Maps/_Zone14/`
- `Resources/Locale/en-US/_Zone14/`
- `Resources/Locale/ru-RU/_Zone14/`
- `Resources/Textures/_Zone14/`
- `Resources/Audio/_Zone14/`
- `Resources/ServerInfo/_Zone14/`
- `Resources/ConfigPresets/_Zone14/`

Inside a `_Zone14/` folder, mirror the upstream feature-driven layout (`_Zone14/Atmos/Components/…`, `_Zone14/Cargo/Systems/…`) rather than grouping by type.

### Namespace (C#)

A file at `Content.<project>/_Zone14/<Feature>/<Sub>/File.cs` declares:

```csharp
namespace Content.<project>._Zone14.<Feature>.<Sub>;
```

**Worked example.** A new anomaly component at `Content.Shared/_Zone14/Anomalies/Components/StalkerAnomalyComponent.cs`:

```csharp
using Robust.Shared.GameStates;

namespace Content.Shared._Zone14.Anomalies.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StalkerAnomalyComponent : Component
{
    [DataField]
    public float FlickerRate = 0.5f;
}
```

## 3. Upstream edits: the `// Zone14:` marker

When you edit a file **outside** a `_Zone14/` folder (an upstream SS14 or stalker14 file), mark every logical change inline. This makes Zona-14 modifications easy to spot during future upstream merges.

Forms:

- **Single line** — `// Zone14: short reason`:
  ```csharp
  public bool Inverted; // Zone14: if true, Species list is a blacklist
  ```
- **Value swap** — `// Zone14: OLD<NEW`:
  ```csharp
  public const int MaxPlayers = 100; // Zone14: 50<100
  ```
- **Multi-line block** — `// Zone14: reason` opens, `// End Zone14` closes:
  ```csharp
  // Zone14: custom stalker loadout validation
  if (profile.Species == "Stalker" && !StalkerLoadoutCheck(profile))
      return false;
  // End Zone14
  ```
- **Added `using`** — trailing `// Zone14`:
  ```csharp
  using Content.Shared._Zone14.Anomalies.Components; // Zone14
  ```

### YAML and Fluent (`.ftl`) edits

Same rule with `#` comments — `# Zone14:` / `# End Zone14`.

```yaml
- type: entity
  id: SomeUpstreamEntity
  components:
  - type: HealthAnalyzer
    scanDelay: 0.8 # Zone14: 1.2<0.8
```

### Upstream-port escape hatch

If the PR is a pure merge or port from `stalker14-project` (no new Zona-14 logic), include `[upstream-port]` in the PR title. The validator skips the marker check for that PR. Do not abuse this — use it only for genuine upstream syncs.

## 4. YAML / prototype convention

- **No mandatory entity-ID prefix.** Folder isolation (`Resources/Prototypes/_Zone14/…`) is the contract; the folder path tells you the entity is Zona-14.
- Optional `Zone14` prefix is fine when the entity's fork-provenance matters at a glance (e.g., `Zone14CargoConsole`).
- Keep file names feature-scoped, not type-scoped (`anomalies.yml`, not `entities.yml`).

## 5. Licensing (code)

The repo has layered licensing. Nothing conflicts — it stacks:

- **Upstream code** (Space Wizards, Corvax) is **MIT**. Preserved verbatim; nobody is relicensing that.
- **Stalker-team contributions** (the `stalker14-project` authors listed at the top of `LICENSE.TXT`) are marked **All rights reserved**. That clause binds their code wherever it lives; contact the Stalker14 team to reuse it.
- **Zona-14 team contributions** — everything under `_Zone14/` — is **MIT** © 2024-2026 Zona-14 Team. By opening a PR that adds files under `_Zone14/`, you agree your contribution is licensed under the Zona-14 MIT terms in `LICENSE.TXT`.

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

**Template** — `Resources/Textures/_Zone14/Anomalies/flicker.rsi/meta.json`:

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
- **PR body**: fill in the `.github/PULL_REQUEST_TEMPLATE.md` checklist. Include media (screenshots / GIFs / video) for anything visible in-game; upload larger videos to the [Zona-14 Discord](https://discord.gg/CFVWFfVpJg) and link them.

## 8. Commit style

- English is preferred for new Zona-14 work.
- Russian is fine for merges / direct ports from `stalker14-project`.
- No Conventional-Commits requirement in v1 — write descriptive messages.

## 9. Code style

- Inherits from `.editorconfig`: 4-space indent, 120-char line limit, trim trailing whitespace, no CRLF.
- Beyond that, defer to the [upstream SS14 style guide](https://docs.spacestation14.com/en/general-development/codebase-info/style-guide.html).
- Zona-14 doesn't add new stylistic rules in v1. Propose changes via Discord before adding rules.

## 10. CI checks

The `Zone14 convention check` workflow runs on every PR. It enforces:

1. **Namespace–folder alignment** — files under `Content.<project>/_Zone14/…` must declare the matching namespace.
2. **Upstream-edit markers** — files edited outside `_Zone14/` must have `// Zone14` (or `# Zone14`) markers in the added hunks (skipped if the PR is tagged `[upstream-port]`).
3. **Misfiled namespace** — `.cs` files outside `_Zone14/` may not declare a `_Zone14.*` namespace.
4. **Greenfield warning** — newly added `.cs` or `.yml` files outside `_Zone14/` produce a warning (non-fatal); reviewers decide.
5. **Key-file delete guard** — protects `README.md`, `README.ru.md`, `LICENSE.TXT`, `CONTRIBUTING.md`, `.github/PULL_REQUEST_TEMPLATE.md`.
6. **Asset `meta.json` license/copyright** — every `meta.json` under `Resources/` (added or modified) must have populated `license` (SPDX identifier on the allowlist) and `copyright` fields; license removals on edits also fail.

### Running the check locally

```bash
bash Tools/_Zone14/check-conventions.sh origin/master HEAD
```

Requirements: `git`, `grep`, `awk`, `jq`. Install `jq` with `sudo apt install jq` (Ubuntu/Debian) or `brew install jq` (macOS).

## 11. Where to discuss

- **Development discussion, playtests, media uploads for large PR videos, day-to-day coordination**: [Zona-14 Discord](https://discord.gg/CFVWFfVpJg).
- **Bug reports, tracked work items**: GitHub Issues on this repo.
- **Code changes**: GitHub Pull Requests on this repo.
