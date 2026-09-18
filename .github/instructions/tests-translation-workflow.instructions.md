---
applyTo: "{Translate,Tests}/**"
---

# Translate and Tests Instructions

`Translate/` contains reusable game-specific extraction, translation, packaging, and configuration code. `Tests/` contains workflow facts and regression tests. Shared translation mechanics belong in the sibling `FanslationStudio.LlmKit` repository.

## Workflow safety

- Do not run numbered workflow facts, live LLM calls, export, merge, or packaging steps unless explicitly requested.
- Workflow facts that mutate `Files/` stay in `Tests/`; reusable pipeline code stays in `Translate/`.
- Prefer pure xUnit regression tests that do not touch working data. Do not create throwaway verification projects.
- Treat `Files/Raw/Export` as regenerable, `Files/Converted` as accumulated translation state, and `Files/Mod` as generated output.

## Translation invariants

- Preserve the shared `TranslationLine`/`TranslationSplit`/`FieldTemplate` data shape and use LlmKit workflows/utilities for generic parsing and packaging.
- Preserve JSON property identity, dynamic-string contracts, placeholders, tags, and raw-fallback behavior when changing game-specific adapters.
- Keep game-specific extraction, configuration, and repair hooks in `Translate/`; keep runtime injection in `EnglishPatch/`.

See [`docs/README.md`](../../docs/README.md) and [`docs/features/translation-pipeline/workflow-execution-reference.md`](../../docs/features/translation-pipeline/workflow-execution-reference.md) for current references.
