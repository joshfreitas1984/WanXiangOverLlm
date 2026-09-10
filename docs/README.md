# WanXiangOverLlm — Documentation Hub

This is the canonical navigation entry point for the repository. It exists so that a human or an
AI agent (Copilot, Claude Code, or otherwise) can find the right source of truth without relying on
vendor-specific memory.

## Repository overview

WanXiangOverLlm builds an English fan-translation patch for *Wan Xiang Qi Xi Zhi* (WXQXZ), plus the
tooling used to translate the game's dumped data via `FanslationStudio.LlmKit`. The sub-projects are
independent and are documented separately:

| Project | Purpose |
| --- | --- |
| [`Translate/`](../Translate/) | Extracts game JSON/text data, drives the LLM translation workflow (built on `FanslationStudio.LlmKit`, referenced via project reference from sibling repo `../FanslationStudio.LlmKit`), and repackages translated data for `EnglishPatch`. |
| [`EnglishPatch/`](../EnglishPatch/) | Runtime plugin that injects translated text into the running game. |
| [`SharedAssembly/`](../SharedAssembly/) | Contracts/services shared between `Translate/` and `EnglishPatch/` (`DynamicStringContract`, `SpriteReplacerContract`, `TextResizerContract`, `WildcardMatchingService`) — distinct from `FanslationStudio.LlmKit`, and out of scope for the LlmKit migration (LlmKit doesn't cover runtime patching/resizing concerns). |
| [`Tests/`](../Tests/) | Test suite for the translation pipeline. |
| [`Files/`](../Files/) | Working-directory data: raw dumped game text (`Raw/Dumped/*.json`, `Raw/ExportedText/`), converted/translated output, and the mod drop-in folder consumed by `EnglishPatch`. |

## Documentation taxonomy

- **`docs/plans/*.md`** — design docs for in-progress or upcoming work (e.g. the `FanslationStudio.LlmKit`
  migration). Read only the file relevant to the current task.
- **`AGENTS.md`** (repo root) — short, auto-loaded, current-state rules. `CLAUDE.md` is a thin
  pointer to it.
- Project-level `readme.md` files describe how to install/run, separate from design docs.

This repo does not yet have per-project scoped instructions files or `KNOWN_ISSUES.md` indexes —
add them if/when a project accumulates enough investigation history to need one (see
`FanslationStudio.LlmKit`'s or `DragonHierOverLlm`'s `AGENTS.md` for the pattern to follow).

## Where should I look?

| Task | Start here |
| --- | --- |
| Understand the `FanslationStudio.LlmKit` migration in progress | [`docs/plans/llmkit-migration-piece1-prefab-dynamicstrings.md`](plans/llmkit-migration-piece1-prefab-dynamicstrings.md), [`docs/plans/llmkit-migration-piece2-json-workflow.md`](plans/llmkit-migration-piece2-json-workflow.md) |
| Work on the JSON/text extraction → LLM translation → repackaging pipeline | [`Translate/FileInputHandling.cs`](../Translate/FileInputHandling.cs), [`Translate/FileOutputHandling.cs`](../Translate/FileOutputHandling.cs), [`Translate/GameTextFiles.cs`](../Translate/GameTextFiles.cs) |
| Understand the `Files/` raw/converted/mod data layout | [`Translate/FileIteration.cs`](../Translate/FileIteration.cs) and the plan docs above |
| Runtime patching (dynamic strings, sprites, text resizing) | [`SharedAssembly/`](../SharedAssembly/) — out of scope for the LlmKit migration |
| Install/play the released patch | [`readme.md`](../readme.md) |
