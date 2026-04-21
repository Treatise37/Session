# `Tools/_Zona14`

Scripts that enforce the Zona-14 coding conventions documented in the root-level [`CONTRIBUTING.md`](../../CONTRIBUTING.md).

## `check-conventions.sh`

PR-diff validator. Runs the six checks described in `CONTRIBUTING.md §10`:

1. Namespace–folder alignment (fatal)
2. Upstream-edit `// Zona14:` / `# Zona14:` marker enforcement (fatal; skipped on `[upstream-port]` PRs)
3. Misfiled `_Zona14` namespace guard (fatal)
4. Greenfield-outside-`_Zona14/` warning (non-fatal; skipped on `[upstream-port]` PRs)
5. Key-file delete guard (fatal)
6. Asset `meta.json` `license` / `copyright` enforcement (fatal; allowlist override via `[custom-license]`)

### Usage

```bash
bash Tools/_Zona14/check-conventions.sh <base-ref> <head-ref>
```

Typical local invocation (before pushing):

```bash
bash Tools/_Zona14/check-conventions.sh origin/master HEAD
```

The workflow [`.github/workflows/zona14-convention.yml`](../../.github/workflows/zona14-convention.yml) runs the same script on every PR against `master`.

### Environment variables

- `PR_TITLE` — optional. Set by the CI workflow from `github.event.pull_request.title`. Used to detect `[upstream-port]` and `[custom-license]` tags. When unset, only commit messages in `base..head` are inspected for tags.

### Dependencies

- `git`, `grep`, `sed`, `awk` — standard on any Linux/macOS shell.
- `jq` — for parsing `meta.json`. Install with `sudo apt install jq` or `brew install jq`.

### Exit codes

- `0` — pass (possibly with warnings on stderr).
- `1` — at least one fatal check failed.
- `2` — usage error or missing dependency (`jq`).
