# WanXiangOverLlm — Copilot Instructions

This repository contains game-specific translation tooling and the runtime patch for WanXiang. Read [`AGENTS.md`](../AGENTS.md) and the scoped instruction file matching the path being edited.

- `Translate/` owns reusable extraction, translation, packaging, and game configuration code.
- `Tests/` owns manually-run workflow facts and pure regression tests. Do not run workflow facts, live LLM calls, export, merge, or packaging steps unless explicitly requested.
- `EnglishPatch/` owns runtime BepInEx/Harmony behavior; preserve game-specific hooks and deployed-assembly assumptions.
- `Files/` is working data. Treat `Raw/Export` as regenerable, `Converted` as accumulated translation state, and `Mod` as generated output.
- Shared translation mechanics belong in the sibling `FanslationStudio.LlmKit` repository. Keep the project reference relative to `../FanslationStudio.LlmKit` and do not duplicate its data-model or workflow logic here.
- Keep current rules in `.github/instructions/`; put feature references under `docs/features/`, durable architecture under `docs/architecture/`, investigations under `docs/investigations/`, and active plans under `docs/plans/`.
- [`docs/KNOWN_ISSUES.md`](../docs/KNOWN_ISSUES.md) is an index only; put detailed narratives in linked topic files.

For translation-pipeline changes, preserve `TranslationLine`/`TranslationSplit`/`FieldTemplate` compatibility and route shared CSV/compound-field behavior through LlmKit utilities. Add focused static validation where practical; do not add throwaway verification projects.
