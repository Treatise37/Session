# Zona-14 changelog — maintainer workflow

Until the webhook bot is wired up, **a maintainer runs the changelog merger by hand after each PR merge**. This directory documents the procedure.

The runtime side (the in-game **Zona 14** tab) is already live: `Content.Client/Changelog/ChangelogManager.cs` auto-discovers every `.yml` under `Resources/Changelog/` and renders one tab per file, sorted by the `Order` field. Nothing there needs ongoing maintenance.

## Feeds

| YAML file | In-game tab | Category prefix in `:cl:` | Notes |
|---|---|---|---|
| `Resources/Changelog/Zona14.yml` | **Zona 14** | *(default — no prefix)* | This repo's own feed. `Order: 0` — first tab. |
| `Resources/Changelog/Admin.yml` | Admin | `ADMIN:` | Admin-only (hidden from non-admins). |
| `Resources/Changelog/Maps.yml` | Maps | `MAPS:` | |
| `Resources/Changelog/Rules.yml` | Rules | `RULES:` | |
| `Resources/Changelog/Changelog.yml` | Upstream | *(frozen — not written to)* | Historical Wizden/upstream entries. `Order: 5` — last tab. |

## Merge procedure

### 1. Grab the `:cl:` block from the merged PR body

Example PR body fragment:

```
:cl: Alice
- add: Added a new stalker artifact.
- fix: Fixed anomaly flicker at low light levels.

MAPS:
- tweak: On Delta, moved engineering locker closer to power.
```

### 2. Write part file(s) in `Resources/Changelog/Parts/`

One part file per category the PR touches. Name them `pr-<number>.yml` (and `pr-<number>-maps.yml` etc. for extra categories). Shape:

```yaml
author: Alice
time: '2026-04-21T18:30:00.0000000+00:00'
url: https://github.com/Zona-14/Zona-14/pull/1337
changes:
  - type: Add
    message: Added a new stalker artifact.
  - type: Fix
    message: Fixed anomaly flicker at low light levels.
```

For categorised entries, add a top-level `category:`:

```yaml
category: Maps
author: Alice
time: '2026-04-21T18:30:00.0000000+00:00'
url: https://github.com/Zona-14/Zona-14/pull/1337
changes:
  - type: Tweak
    message: On Delta, moved engineering locker closer to power.
```

Types accepted by the merger: `Add` / `Remove` / `Tweak` / `Fix` (capitalised — the runtime enum expects these).

### 3. Run the merger

From the repo root:

```bash
# Zona 14 (default category — grabs parts with no 'category:' field)
python3 Tools/update_changelog.py Resources/Changelog/Zona14.yml Resources/Changelog/Parts/

# Extra-category feeds (only needed if the PR included those prefixes)
python3 Tools/update_changelog.py Resources/Changelog/Admin.yml Resources/Changelog/Parts/ --category Admin
python3 Tools/update_changelog.py Resources/Changelog/Maps.yml  Resources/Changelog/Parts/ --category Maps
python3 Tools/update_changelog.py Resources/Changelog/Rules.yml Resources/Changelog/Parts/ --category Rules
```

Each run:
- Assigns the next sequential `id` (IDs are monotonic across all feeds — the merger reads the max from the target file).
- Appends new entries to `Entries:`.
- **Deletes the consumed part files.**
- Trims to 500 most recent entries (only affects large feeds).

### 4. Commit

```bash
git add Resources/Changelog/Zona14.yml Resources/Changelog/Parts/
git commit -m "chore: changelog for PR #1337"
```

If you merged several PRs in one session, you can batch the commits: drop all parts at once, run the mergers once, and commit with `chore: changelog update`.

## Verification

- `python3 Tools/update_changelog.py ... Resources/Changelog/Parts/` is idempotent — running it twice on an empty `Parts/` is a no-op.
- After running, `git status` should show `Resources/Changelog/<Feed>.yml` modified and the `Parts/pr-*.yml` files deleted.
- Boot the client (`dotnet run --project Content.Client`) and open the changelog (`Esc → Changelog` or `/changelog`) to verify the entry rendered correctly.

## Future automation

The upstream automation lives at [`space-wizards/SS14.Changelog`](https://github.com/space-wizards/SS14.Changelog) — an ASP.NET webhook receiver that does exactly the above, triggered by `pull_request.closed` GitHub webhooks. Wizden and Frontier both run it. To adopt:

1. Fork (or run verbatim) `space-wizards/SS14.Changelog`. No fork-specific logic in the bot itself.
2. Deploy on a Linux host with network access to GitHub (ASP.NET 8+, listens on `localhost:45896`, reverse-proxy with nginx/Caddy).
3. `appsettings.yml` template:
   ```yaml
   Changelog:
     GitHubSecret: "<webhook HMAC shared secret>"
     ChangelogBranchName: master
     ChangelogFilename: Zona14.yml
     ChangelogRepo: /var/lib/changelog/zona-14   # local clone of Zona-14/Zona-14
     SshKey: /var/lib/changelog/deploy_key       # deploy key with push to master
     DelaySeconds: 60
     CommitAuthorName: "Zona-14 Changelog Bot"
     CommitAuthorEmail: "changelog-bot@zona-14.invalid"
     ExtraCategories: [Admin, Maps, Rules]
   ```
4. Add a GitHub webhook on `Zona-14/Zona-14` → `https://<bot-host>/hook`, content type `application/json`, same `GitHubSecret`, subscribed to **Pull requests** and **Push**.
5. SSH deploy key with push access to `Zona-14/Zona-14` master.

Once the bot is live, the manual steps 1–4 above go away. This README stays as documentation of the pipeline's shape.

## Optional fanout (deferred)

Upstream's `Tools/actions_changelogs_since_last_run.py` and `Tools/actions_changelog_rss.py` are present in this repo (inherited). They post new entries to a Discord webhook and an RSS feed respectively when the `publish.yml` workflow runs. Wiring them up needs:

- `CHANGELOG_DISCORD_WEBHOOK` secret → Zona-14 Discord's #changelog webhook. Add a step in `.github/workflows/publish.yml` that runs `Tools/actions_changelogs_since_last_run.py`.
- `CHANGELOG_RSS_KEY` secret → Ed25519 key for an SSH target hosting the RSS XML. Requires the `FEED_*` constants in `actions_changelog_rss.py` to be retargeted to Zona-14 hosting. Skip unless there's a concrete hosting plan.

Not blocking — the in-game tab works without either.
