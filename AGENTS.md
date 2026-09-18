# AGENTS.md

Universal rules for AI coding agents working in this repository. The canonical documentation hub
is [`docs/README.md`](docs/README.md); read the scoped instruction file matching the project being
edited before making changes.

## Repository structure

- `Translate/` contains reusable game-specific extraction, translation, packaging, and configuration code.
- `Tests/` contains manually-run workflow facts and pure regression tests.
- `SharedAssembly/` contains contracts shared by `Translate/` and `EnglishPatch/`.
- `EnglishPatch/` contains the BepInEx/Harmony runtime plugin.
- `Files/` contains raw, converted, and packaged translation data.

## Documentation rules

- Keep current operational rules in `.github/instructions/` and the root Copilot instructions.
- Keep [`docs/KNOWN_ISSUES.md`](docs/KNOWN_ISSUES.md) as an index only.
- Put current behavior in `docs/features/`, investigations in `docs/investigations/`, active plans in
  `docs/plans/`, and durable structure in `docs/architecture/`.
- Shared-library mechanics belong in the sibling `FanslationStudio.LlmKit/docs/` tree; link to them
  instead of duplicating implementation details here.
- Do not update documentation as a side effect of an ordinary fix unless the user explicitly asks.

## Engineering rules

- `Translate/` references the sibling `FanslationStudio.LlmKit` by project reference, never NuGet.
- Preserve LlmKit's `TranslationLine`/`TranslationSplit`/`FieldTemplate` contract; propose shared model
  changes upstream in LlmKit.
- Keep reusable pipeline code in `Translate/` and workflow/manual facts in `Tests/`.
- Do not run workflow facts, live LLM calls, export, merge, or packaging steps unless requested.
- Do not create throwaway verification projects.

See [`docs/README.md`](docs/README.md)'s task table for the detailed feature and workflow references.
