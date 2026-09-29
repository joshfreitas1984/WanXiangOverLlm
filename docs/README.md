# WanXiangOverLlm documentation hub

# Latest release
## Install with the installer (recommended)

1. Download the installer: [Windows](https://github.com/joshfreitas1984/WanXiangOverLlm/releases/download/installer/Installer-win-x64.exe) or [Linux](https://github.com/joshfreitas1984/WanXiangOverLlm/releases/download/installer/Installer-linux-x64). These links always give you the newest installer.
2. Run it. It finds the game through Steam (use **Browse** if it can't), installs BepInEx, and installs the [latest patch release](https://github.com/joshfreitas1984/WanXiangOverLlm/releases/latest). Press **Install / Update**.
3. Start the game once and let it reach the main menu, so BepInEx can generate its files.

Windows may warn "Windows protected your PC" because the installer isn't code-signed. Choose **More info**, then **Run anyway**.

**Linux (Steam/Proton):** the game and BepInEx are the Windows builds. Paste this into the game's Steam **Properties > Launch Options** (the installer shows it with a Copy button):

```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

To remove the patch, run the installer and press **Uninstall patch**. It removes the patch files and leaves BepInEx in place.

## Updates

When the game starts it checks for a newer patch. If there is one, an **Update available** window appears about 15 seconds later. **Update now** downloads it, closes the game, applies the update and restarts the game through Steam (on Linux you may need to start the game yourself afterwards). **Later** hides it until the next start. If a check or download fails, nothing changes and the reason is written to the BepInEx log.

Your settings files under `BepInEx/config` are never overwritten by an update. To turn the check off, set `Enabled = false` under `[Updates]` in `BepInEx/config/FanslationStudio.Plugins.UIEditor.cfg`. Updates only work for a patch installed with the installer.

## Manual install

Install [BepInEx 5.4.23.5 for Windows x64](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5) (`BepInEx_win_x64_5.4.23.5.zip`) into the folder that contains `WXQXZ.exe`, then extract the [latest release zip](https://github.com/joshfreitas1984/WanXiangOverLlm/releases/latest) over it. Release zips no longer contain BepInEx itself. A manual install won't get the in-game update prompt.

# Contacting us
You can join us here: [Discord](https://discord.gg/sqXd5ceBWT)

This is the canonical navigation entry point for the Wan Xiang Qi Xi Zhi translation tooling and
runtime patch.

## Projects

| Project | Purpose |
| --- | --- |
| [`Translate/`](../Translate/) | Reusable game-specific extraction, translation, packaging, and configuration code. |
| [`Tests/`](../Tests/) | Workflow facts and pure regression tests for the translation pipeline. |
| [`SharedAssembly/`](../SharedAssembly/) | Contracts shared by translation tooling and the runtime plugin. |
| [`EnglishPatch/`](../EnglishPatch/) | BepInEx/Harmony runtime patch and translation injection. |
| [`Files/`](../Files/) | Raw, converted, glossary, test-result, and packaged translation data. |

The tooling references the sibling `FanslationStudio.LlmKit` repository by project reference. Its
docs are the source of truth for shared line/split/template, parsing, translation, QC, and packaging
mechanics.

## Documentation taxonomy

- Scoped instructions under `.github/instructions/` contain concise operational rules.
- [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md) is one repository-wide index only.
- `docs/features/` contains current behavior and workflow references.
- `docs/investigations/` contains incidents, bug investigations, and postmortems.
- `docs/plans/` contains active plans only.
- `docs/architecture/` contains durable structure and design decisions.

## Where should I look?

| Task | Start here |
| --- | --- |
| Work on extraction, translation, or packaging | [Translate and Tests instructions](../.github/instructions/tests-translation-workflow.instructions.md), then [workflow reference](features/translation-pipeline/workflow-execution-reference.md) |
| Investigate a known issue | [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md), then the linked investigation |
| Understand repository structure | [downstream project structure](architecture/downstream-project-structure.md) |
| Work on runtime patching | [`EnglishPatch/`](../EnglishPatch/) and [`SharedAssembly/`](../SharedAssembly/) |
| Install or play the released patch | [`readme.md`](../readme.md) |
