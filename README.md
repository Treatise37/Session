# Zona-14

> 🇷🇺 [Русская версия](README.ru.md)

Zona-14 is an English-direction **S.T.A.L.K.E.R.-themed fork of Space Station 14**, built on the [Robust Toolbox](https://github.com/space-wizards/RobustToolbox) engine (C#).

## Lineage

This fork descends from:

- [space-wizards/space-station-14](https://github.com/space-wizards/space-station-14) — upstream SS14.
- [space-syndicate/space-station-14](https://github.com/space-syndicate/space-station-14) — Russian mainline SS14 fork.
- [stalker14-project/stalker14](https://github.com/stalker14-project/stalker14) — S.T.A.L.K.E.R.-themed derivative (our direct parent).

We merge from `stalker14-project` regularly; Zona-14-specific work is isolated under `_Zona14/` folders so upstream syncs stay manageable.

## Quickstart

See the [Space Wizards setup guide](https://docs.spacestation14.com/en/general-development/setup.html) for prerequisites (recent .NET SDK, `git` with LFS, etc.), then:

```bash
git clone --recursive https://github.com/Zona-14/Zona-14.git
cd Zona-14
dotnet run --project Content.Server
# In a second terminal:
dotnet run --project Content.Client
```

## Contributing

All new Zona-14 code lives under `_Zona14/` folders — see [`CONTRIBUTING.md`](CONTRIBUTING.md) for the full rules (namespace conventions, `// Zona14:` markers for upstream edits, sprite `meta.json` license requirements, PR template, CI checks).

Before pushing, run the local validator:

```bash
bash Tools/_Zona14/check-conventions.sh origin/master HEAD
```

## Bug reports & feedback

Bug reports, player feedback, and feature requests go to the public [**Zona-14-Feedback**](https://github.com/Zona-14/Zona-14-Feedback) repo — not to Discord. Anyone can open an issue there; it's the canonical channel for community-facing reports.

## Community

Join the [Zona-14 Discord](https://discord.gg/57S48NzbZ9) for news, updates, playtests, development discussion, and coordinating contributions. It's also where you upload larger gameplay videos for PR reviews. For bug reports, use [Zona-14-Feedback](https://github.com/Zona-14/Zona-14-Feedback) instead.

## License

The repository has layered licensing. In plain English:

- **Upstream code** (Space Wizards, Corvax) is [MIT-licensed](LICENSE.TXT).
- **Stalker-team contributions** (authors named in `LICENSE.TXT`) are marked **All rights reserved** — contact the [Stalker14 team](https://discord.gg/GXzurVkWYX) for reuse permission.
- **Zona-14 team contributions** (everything under `_Zona14/`) are [MIT-licensed](LICENSE.TXT) © 2024-2026 Zona-14 Team.
- **Assets** (sprites, audio, maps) inherit [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) by default; each asset's `meta.json` records its actual license and attribution.

Full legal text in [`LICENSE.TXT`](LICENSE.TXT).

---

🇷🇺 [Русская версия — README.ru.md](README.ru.md)
