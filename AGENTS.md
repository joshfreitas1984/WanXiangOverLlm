# AGENTS.md

Universal rules for any AI coding agent working in this repository, regardless of vendor. This
file exists so no repository rule lives in only one vendor-specific format
(`.github/copilot-instructions.md`, `CLAUDE.md`, etc.) — mirrors the same pattern used by
`FanslationStudio.LlmKit` and `DragonHierOverLlm`.

## Start here

- [`docs/README.md`](docs/README.md) is the canonical documentation hub — project overview,
  documentation taxonomy, and a "where should I look?" task table.

## Repository-wide rules

- This repository contains independent sub-projects (`Translate/`, `EnglishPatch/`,
  `SharedAssembly/`, `Tests/`, `Files/`). Do not assume conventions from one apply to another
  without checking its own code/docs first.
- `Translate/` consumes `FanslationStudio.LlmKit` (sibling repo `../FanslationStudio.LlmKit`) via a
  project reference, not a NuGet package — changes there take effect immediately here without a
  version bump. LlmKit's `TranslationLine`/`TranslationSplit`/`FieldTemplate` shape is a
  golden-rule-protected contract shared across every consuming repo: never change its shape from
  this repo, only propose additive changes upstream in LlmKit itself.
- `SharedAssembly/` is shared between `Translate/` and `EnglishPatch/` only — it is a different
  project to `FanslationStudio.LlmKit` and is not migrated/covered by the LlmKit work (runtime
  patching/resizing concerns are out of scope for LlmKit).
- Do not update this file, `docs/`, or other instructions/documentation as a side effect of a fix or
  feature. Only write documentation when explicitly asked to.
- Keep this file short and operational. Long rationale, design proposals, and migration plans
  belong in [`docs/plans/`](docs/plans/), not here.

## Where to look for more detail

See [`docs/README.md`](docs/README.md)'s "Where should I look?" table for task-specific starting
points (translation pipeline work, the in-progress `FanslationStudio.LlmKit` migration, etc.).
