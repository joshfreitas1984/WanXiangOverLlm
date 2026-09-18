# WanXiangOverLlm documentation hub

# Latest release
Extract the [Latest Release](https://github.com/joshfreitas1984/WanXiangOverLlm/releases) into your `<Game Folder>` folder where WXQXZ.exe is.

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
