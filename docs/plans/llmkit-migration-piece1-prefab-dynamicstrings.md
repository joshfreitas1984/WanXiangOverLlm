# LlmKit migration — Piece 1: PrefabText + DynamicStrings

Status: design, not yet implemented. Branch: `feature/llmkit-migration`.

## Goal

Move `dumpedPrefabText.txt` and `dynamicStrings.txt` off this repo's own
`Translate/FileInputHandling.cs`/`FileOutputHandling.cs` logic and onto
`FanslationStudio.LlmKit` (referenced via project reference, sibling repo
`../FanslationStudio.LlmKit`), reusing its existing `TranslationLine`/`TranslationSplit`/
`FieldTemplate` model and `GameFileHandlingBase` merge/package plumbing. This is the smaller,
lower-risk half of the migration — no changes to LlmKit's golden-rule-protected model shape are
required for `dumpedPrefabText.txt`, and only one small new workflow class is needed for
`dynamicStrings.txt`.

## dumpedPrefabText.txt → `PrefabTextWorkflow`

Current state in this repo: **dormant**. `Translate/GameTextFiles.cs` has the entry commented out
(`//new() {Path = "dumpedPrefabText.txt", ...}`), and `Files/Raw/ExportedText/dumpedPrefabText.txt`
does not currently exist in the working tree. `Translate/FileInputHandling.cs`'s
`ExportDumpedPrefabToCustomFormat` still exists and is a straight "one distinct string per line" →
whole-line `TranslationSplit` converter, functionally identical to what LlmKit's
`Workflow.PrefabTextWorkflow.ExportPrefabTextToCustomFormat` already does (plus it also runs
`CompoundFieldSplitter.Decompose` per line, which this repo's version does not).

Because this is not a wired-in, live feature today, migrating it is a re-enable, not a conversion
of production behavior:

1. Add `TextFileToSplit { Path = "dumpedPrefabText.txt", TextFileType = TextFileType.PrefabText }`
   to whatever replaces `GameTextFiles.TextFilesToSplit` once it's rebuilt against
   `FanslationStudio.LlmKit.Support.TextFileToSplit`/`TextFileType` (see "Model swap" below).
2. Call `LlmKit.Workflow.PrefabTextWorkflow.ExportPrefabTextToCustomFormat(workingDirectory, textFile)`
   from wherever this repo's export entry point lives, reading from
   `Files/Raw/Dumped/PrefabText/dumpedPrefabText.txt` — note the LlmKit convention is
   `Raw/Dumped/PrefabText/{path}`, not this repo's current `Raw/ExportedText/{path}`. Either move the
   dump step to write there, or check whether `PrefabTextWorkflow` needs a `rawSubfolder` parameter
   added upstream (it currently hardcodes the subfolder, unlike `CsvGameDataWorkflow.ExportToCustomFormat`,
   which takes `rawSubfolder` as a parameter). **Open question for the LlmKit PR**: add an optional
   `rawSubfolder` parameter to `PrefabTextWorkflow.ExportPrefabTextToCustomFormat` for parity with
   `CsvGameDataWorkflow`, defaulting to the existing `"Raw/Dumped/PrefabText"` — purely additive,
   doesn't change any existing caller.
3. Call `LlmKit.Workflow.PrefabTextWorkflow.PackagePrefabTextAsync(workingDirectory, textFile)` from
   the packaging entry point. Output shape (`- raw: ... / result: ...` YAML) is unchanged from what
   this repo already does not currently produce (dormant), so there is no existing consumer to keep
   in sync — but confirm whether `EnglishPatch` expects this file/shape before re-enabling, since a
   runtime plugin consuming it may not exist yet either.

No LlmKit model or workflow-behavior changes needed for this part.

## dynamicStrings.txt → new LlmKit workflow

This is **not** a drop-in onto the existing `LlmKit.Workflow.DynamicStringWorkflow`. That workflow's
own doc comment states it is for `TextFileType.DynamicStringsIL2CPP` — the newer Harmony-postfix,
IL2CPP-safe approach, where the packaged output is a flat `raw → result` substring-replacement
dictionary (`DynamicStringResult`, no method/IL-offset addressing). WanXiang's
`dynamicStrings.txt` is the **older** Mono/Cecil-transpiler dump+patch format:

```
GameTools,Save,88,备份失败: ,[System.Object，System.String，System.String，System.Boolean]
Type,   Method,ILOffset,RawText,        ParamTypesList
```

— 5 positional fields, naive (non-quote-aware) comma-split, with the dumper substituting full-width
`，` for any literal comma that would otherwise appear inside `RawText`/`ParamTypesList` (see
`SharedAssembly/DynamicStrings/DynamicStringSupport.cs`'s `PrepareMethodParameters`, which reverses
this at parse time, and `Translate/FileOutputHandling.cs`'s `.Replace("，", ",")` on the translated
text at package time). It packages into `DynamicStringContract` (`Type`/`Method`/`ILOffset`/`Raw`/
`Translation`/`Parameters`), keyed by IL offset for the runtime Cecil-transpiler patch to find —
nothing like `DynamicStringsIL2CPP`'s flat substring dictionary.

**Notable finding**: LlmKit already contains almost everything needed for this except the workflow
class itself:

- `FanslationStudio.LlmKit/Contracts/DynamicStringContract.cs` — identical field-for-field to this
  repo's own `SharedAssembly/DynamicStrings/DynamicStringContract.cs` (`Type`/`Method`/`ILOffset`/
  `Raw`/`Translation`/`Parameters`), plus `GroupedDynamicStringContracts`.
- `FanslationStudio.LlmKit/SharedAssembly/DynamicStrings/DynamicStringSupport.cs` — an apparently
  verbatim copy of this repo's own `IsSafeContract`/`PrepareMethodParameters`/
  `ReplaceCommasInBrackets`, including the same game-specific `skipTypes`/`skipMethods`/
  `skipCombinations` lists (`"SweetPotato"`, `"GameTools"`, etc.) — this is this game's data, already
  staged in LlmKit for reuse.
- `FanslationStudio.LlmKit/Support/TextFileToSplit.cs`'s `TextFileType` enum already has a
  `DynamicStrings` value reserved for exactly this (its XML doc explicitly contrasts it with
  `DynamicStringsIL2CPP`).
- `Workflow/TranslationWorkflow.cs` already branches on `TextFileType.DynamicStrings` in
  `TryHandleDynamicStringExclusion` (translation-time exclusion rules for sprite/UI-looking text).

What's missing is the actual `Export…`/`Package…` pair. Proposed new file
`FanslationStudio.LlmKit/Workflow/DynamicStringsCecilWorkflow.cs` (name open for discussion — avoid
colliding with the existing `DynamicStringWorkflow`, which stays IL2CPP-only), mirroring this
repo's `FileInputHandling.ExportDynamicStringsToCustomFormat`/`FileOutputHandling`'s
`TextFileType.DynamicStrings` branch:

- `ExportDynamicStringsToCustomFormat(workingDirectory, textFile, ...)` — reads
  `Raw/Dumped/dynamicStrings.txt` (or a `rawSubfolder`-parameterized path, for consistency with
  `CsvGameDataWorkflow`), naive-splits each line on `,`, records each of the 5 fields as its own
  `TranslationSplit` when it matches the Chinese-char pattern (this repo's current behavior — only
  `RawText`, field index 3, realistically ever matches), writes the standard
  `TranslationLine` YAML to `Raw/Export`/`Converted`.
- `PackageDynamicStringsCecilAsync(workingDirectory, textFile)` — re-splits `line.Raw` on `,`,
  applies `DynamicStringSupport.PrepareMethodParameters` to field 4, builds a `DynamicStringContract`
  per line, filters via `DynamicStringSupport.IsSafeContract`, writes the resulting
  `List<DynamicStringContract>` as YAML to `Mod/dynamicStrings.txt` (matching current output
  location/shape exactly) via `LlmKit.Utility.YamlHelper`.

No `TranslationLine`/`TranslationSplit`/`FieldTemplate` model changes needed — this reuses the
existing `Raw`/`Splits`/`Split` shape exactly as `CsvGameDataWorkflow` does for a single-column file
(field index as `Split`, no `Templates`).

## Sequencing

1. Land the `DynamicStringsCecilWorkflow` (+ optional `rawSubfolder` param on
   `PrefabTextWorkflow.ExportPrefabTextToCustomFormat`) in `FanslationStudio.LlmKit` first, as its
   own PR against that repo, with unit tests against the existing `DynamicStringSupport`/new
   workflow (pure functions, no live LLM/game dependency — matches LlmKit's own
   `AGENTS.md` testing guidance).
2. Then, in this repo, swap `Translate/Support/TranslationLine.cs`/`TranslationSplit.cs`/
   `TextFileToSplit.cs`/`GameTextFiles.cs`'s hand-rolled types for LlmKit's, replace the
   `Export…`/`Package…`/`Merge…` call sites in `FileInputHandling.cs`/`FileOutputHandling.cs` with
   calls into `LlmKit.Workflow.*`/`LlmKit.GameFileHandlingBase`, and delete the now-dead local copies.
   The JSON-table (`RegularDb`) branch stays on this repo's own logic until Piece 2 lands — see
   `docs/plans/llmkit-migration-piece2-json-workflow.md`.
3. Re-enable `dumpedPrefabText.txt` in `GameTextFiles.cs` only once someone confirms `EnglishPatch`
   is ready to consume its packaged output — otherwise leave it commented out/unwired in the new
   shape, matching current (dormant) behavior.
