# LlmKit migration — Piece 2: JSON game-data tables

Status: design, not yet implemented. Branch: `feature/llmkit-migration`. Depends on Piece 1 landing
first (see `docs/plans/llmkit-migration-piece1-prefab-dynamicstrings.md`) so the model swap happens
once, not twice.

## Why this is a different problem to Piece 1

`Files/Raw/Dumped/*.json` (~35 files — `Hero.json`, `Skill.json`, `Event.json`, etc.) are JSON
arrays of objects, one object per row, keyed by a `Key` property, with:

- Variable/loose schema — not every object has every property (this repo's own
  `InputFileHandling.ExportTextAssetsToCustomFormat` just enumerates whatever properties exist per
  object; there's no fixed column list).
- Parallel `...Tw` (Traditional Chinese) sibling properties for many translatable fields, which the
  existing exporter explicitly skips (`property.Name.ToLower().Replace("list","").EndsWith("tw")`),
  as well as a `...Final` suffix convention, also skipped.
- String array properties (e.g. `SomeArray`) where individual elements, not the whole array, are
  the translatable unit — addressed as `SomeArray[2]`.
- Fields addressed by **JSON property name** (`Desc`, `SomeArray[2]`), not by position/column index.

LlmKit's model is entirely CSV/column-index shaped:

- `TranslationSplit.Split`/`SubIndex` are `int`s — a CSV column index and a compound-fragment index
  within that column. There is no concept of "property named X".
- `TranslationLine` has no per-line identity separate from `Raw` (the whole raw row/line text).
  `GameFileHandlingBase.MergeFilesIntoTranslatedAsync` matches an old exported line to a newly
  re-exported one by `Raw` string equality (falling back to a `Split`+`SubIndex`+`Text` scan). For a
  JSON object, matching on whole-object equality means **any** field changing between game updates
  — including untranslatable ones like numeric stats — invalidates the match and loses previously
  translated fields' `Translated` values for that object, forcing full re-translation of the file on
  every game patch. WanXiang's own pipeline already avoids this by keeping the object's `Key`
  (`RawIndex`) as a separate, stable identity from `Raw`, and matching on that instead
  (`InputFileHandling.MergeFilesIntoTranslatedAsync`'s `RegularDb` branch, and
  `FileOutputHandling.PackageFinalTranslationAsync`'s `int.TryParse(line.RawIndex, ...)` → `Key`
  round-trip).
- `CsvGameDataWorkflow`/`PrefabTextWorkflow`/`DynamicStringWorkflow` all reconstruct output by
  splitting/rebuilding a single delimited row (`CompoundFieldSplitter.ParseCsvRow`/`RebuildCsvRow`).
  There's no equivalent for "parse this line as a JSON array, look up object by `Key`, set property
  by path, possibly into an array element, re-serialize."

None of this can be forced through `CsvGameDataWorkflow` without pretending a JSON object is a CSV
row, which it isn't — it needs its own type and workflow, per your original scoping. The design
below stays deliberately close to `CsvGameDataWorkflow`'s shape and API conventions (single-file
`Export…`/`PackageAsync` pair, `TextFileToSplit`-driven, same `Raw/Export`→`Converted`→`Mod`
directory flow) so it reads as "the JSON sibling of CsvGameDataWorkflow," not a bespoke one-off.

## Golden-rule constraint

LlmKit's `AGENTS.md`: *"the `Line → Splits → (Templates)` data model shape is the contract every
downstream project depends on. Extend it with new optional fields (safe defaults), never change its
shape — old serialized YAML in a downstream repo's `Files/Converted/*.yaml` must keep
deserializing correctly."* Every change below is additive: a new nullable/default-valued field, or a
new enum value. Nothing existing is renamed, retyped, or removed, and every new field defaults to a
value that reproduces current behavior for every non-JSON `TextFileType`.

## Proposed model additions

### `TranslationLine` (`FanslationStudio.LlmKit/Support/TranslationLine.cs`)

```csharp
/// <summary>
/// Stable per-line identity independent of <see cref="Raw"/> — e.g. a JSON object's "Key" property
/// (see Workflow.JsonGameDataWorkflow). Empty for every file type that identifies a line by its
/// whole Raw text instead (CSV row, PrefabText/DynamicStrings line) — those keep using Raw-equality
/// matching in GameFileHandlingBase.MergeFilesIntoTranslatedAsync unchanged. When non-empty, a
/// consuming workflow's merge step should match on this instead of Raw equality, since Raw can
/// legitimately change (untranslated fields updated) without invalidating existing translations.
/// </summary>
public string RawIndex { get; set; } = string.Empty;
```

This is the one field WanXiang's own `TranslationLine` already has today (`Translate/Support/
TranslationLine.cs`) that LlmKit's does not — porting it forward, not inventing something new.

### `TranslationSplit` (`FanslationStudio.LlmKit/Support/TranslationSplit.cs`)

```csharp
/// <summary>
/// JSON property path this split was extracted from (e.g. "Desc", "SomeArray[2]"), for file types
/// addressed by path rather than column index (see Workflow.JsonGameDataWorkflow). Empty for every
/// other TextFileType, which keep using Split/SubIndex for addressing.
/// </summary>
public string SplitPath { get; set; } = string.Empty;
```

Also ported forward verbatim from WanXiang's existing `TranslationSplit.SplitPath` — same name, same
purpose, so this repo's own YAML that already has a `SplitPath` key continues to deserialize
unchanged once it points at LlmKit's type.

No changes needed to `FieldTemplate` — a JSON leaf value that itself packs multiple Chinese
fragments (e.g. a `Desc` string with several sentences) can still use
`CompoundFieldSplitter.Decompose`/`FieldTemplate.Split` exactly as `CsvGameDataWorkflow` does, with
`FieldTemplate.Split` repurposed to mean "which `SplitPath`-addressed field this template
reconstructs" (see below) rather than "which column index" — this needs one clarifying doc-comment
update on `FieldTemplate.Split`, not a shape change, and a lookup-by-`SplitPath`-instead-of-`int`
convention in the new workflow's packaging code (matching is done by scanning `line.Templates`
for the corresponding `SplitPath`-tagged entry rather than an int index — see below for exactly how).

**Open design question to resolve before implementation**: `FieldTemplate.Split` is `int`. Two
options:
1. Leave `FieldTemplate.Split` as `int` and always `0` for JSON-addressed templates (since a JSON
   line only ever has one `SplitPath` needing decomposition tracked per `FieldTemplate` entry
   anyway, disambiguated by pairing each `FieldTemplate` with a matching `TranslationSplit` set via
   `SplitPath` instead of `Split`) — i.e. add `FieldTemplate.SplitPath` as a second additive field,
   mirroring `TranslationSplit`, and have the JSON workflow ignore `Split`/set it to `0` throughout.
   **Recommended** — keeps `FieldTemplate`'s existing `int Split` meaning ("CSV column index")
   completely untouched for every other file type, and mirrors exactly how `TranslationSplit` itself
   grew a parallel `SplitPath` alongside its existing `Split`/`SubIndex` rather than overloading them.
2. Overload `Split` to mean "index into the line's ordered list of `SplitPath`s needing templates"
   for JSON lines. Rejected — reintroduces exactly the "meaning depends on file type" ambiguity the
   explicit `SplitPath` field is meant to avoid, and every reader of `FieldTemplate.Split` elsewhere
   would need to know which convention applies.

So: **add `FieldTemplate.SplitPath` (string, default `""`)** alongside its existing `int Split`,
following option 1.

### New `TextFileType.RawJson`

```csharp
/// <summary>
/// JSON array-of-objects game data, each object keyed by a "Key" property, with loose/variable
/// per-object schema and translatable fields addressed by JSON property path (TranslationSplit/
/// FieldTemplate.SplitPath) rather than column index — handled by Workflow.JsonGameDataWorkflow.
/// Distinct from RawCsv because there is no row/column structure to parse via
/// CompoundFieldSplitter.ParseCsvRow/RebuildCsvRow; a "row" here is one JSON object, addressed by
/// its Key (TranslationLine.RawIndex), not by position.
/// </summary>
RawJson,
```

Added to `FanslationStudio.LlmKit/Support/TextFileToSplit.cs`'s `TextFileType` enum — a pure
addition, existing enum values/ordinal positions unchanged (this enum is not `[Flags]` and isn't
documented as serialized by ordinal anywhere Piece 1's investigation turned up, but the new value
should still be appended at the end rather than inserted, to avoid relying on that).

### New `Workflow.JsonGameDataWorkflow`

New file `FanslationStudio.LlmKit/Workflow/JsonGameDataWorkflow.cs`, same shape as
`CsvGameDataWorkflow`:

```csharp
public static class JsonGameDataWorkflow
{
    public static void ExportToCustomFormat(
        string workingDirectory, TextFileToSplit textFile, CompoundFieldSplitterOptions? options = null,
        string rawSubfolder = "Raw/Dumped")
    { ... }

    public static async Task<(int Passed, int Failed)> PackageAsync(
        string workingDirectory, TextFileToSplit textFile)
    { ... }
}
```

**Export**: parse `{rawSubfolder}/{textFile.Path}` as a JSON array via `System.Text.Json.JsonDocument`
(matching this repo's existing `InputFileHandling.ExportTextAssetsToCustomFormat` exactly for the
extraction rules, since that logic is already correct and tested against this game's data):

- Skip if root isn't a JSON array.
- Per object: require a `Key` property; skip the object entirely if absent (matches current
  behavior).
- `TranslationLine.Raw = entry.GetRawText()`, `TranslationLine.RawIndex = key`.
- Per property (except `Key` itself, and any property whose name — after lowercasing and stripping
  a trailing `List`/`list` — ends with `tw` or `final`): if the value is a string containing Chinese
  text, run it through `CompoundFieldSplitter.Decompose` (this is new relative to WanXiang's current
  exporter, which records the whole string as one split with no decomposition — bringing in
  `Decompose` here is a deliberate improvement, giving JSON fields the same compound-fragment
  splitting CSV/PrefabText/DynamicStringsIL2CPP already get, not a behavior change requiring a flag).
  Trivial templates still record as one plain `TranslationSplit(0, 0, text) { SplitPath = property.Name }`
  (no `FieldTemplate`); non-trivial ones add a `FieldTemplate { SplitPath = property.Name, Template = template }`
  plus one `TranslationSplit` per fragment (`SplitPath = property.Name`, `SubIndex = fragment index`).
- If the value is a string array: same `Decompose` treatment per element, with
  `SplitPath = $"{property.Name}[{index}]"`.
- Only add the line to the output if it has at least one split (matches current behavior).
- Serialize/write `Raw/Export/{path}.yaml` and seed `Converted/{path}.yaml` if missing — identical
  to `CsvGameDataWorkflow`.

**Package**: for each `TranslationLine`, re-parse `line.Raw` as the original JSON object (needed to
recover the exact original array-length/structure for any `SplitPath` ending in `[n]`, exactly as
WanXiang's current `FileOutputHandling.PackageFinalTranslationAsync` already does via
`JsonDocument.Parse(line.Raw)`), then:

- Group `line.Splits` by `SplitPath` (stripped of any trailing `[n]`) to find which properties need
  reconstruction; for each, look up a matching `FieldTemplate` by `SplitPath` (not `Split`/index —
  see the `FieldTemplate.SplitPath` addition above). If found, reconstruct via
  `CompoundFieldSplitter.Reconstruct` from the ordered (`SubIndex`) fragments' effective translated
  text, same fallback/failure rules as `CsvGameDataWorkflow.PackageAsync` (untranslated/flagged/unsafe
  fragment ⇒ whole property falls back and counts as failed).
- If no `FieldTemplate` matches a `SplitPath`, treat it as a single-fragment property/array-element
  exactly as WanXiang's current packager does.
- Rebuild the JSON object: `Key` from `RawIndex` (int-parse with string fallback, matching current
  behavior exactly), each translated/reconstructed property or array element set at its path,
  everything else in the object copied through from the original parse untouched (properties with
  no splits, `...Tw`/`...Final` siblings, non-string/non-array properties) — this mirrors
  WanXiang's current `FileOutputHandling.PackageFinalTranslationAsync` JSON-rebuild loop closely
  enough that it can likely be lifted near-verbatim into the new workflow.
- Serialize the whole array back to `Mod/{path}` with `WriteIndented = true` and
  `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` (matching current output exactly, since this is what
  the shipped mod file format already is and downstream tooling/`EnglishPatch` presumably expects it
  unchanged).

### `GameFileHandlingBase.MergeFilesIntoTranslatedAsync` — matching by `RawIndex`

This is the one existing LlmKit method that needs a behavior branch, not just a new field. Currently
it matches by `Raw` string equality unconditionally. Proposed change (additive/backward compatible —
every existing `TextFileType` has `RawIndex == ""` on every line, so this only changes behavior for
`RawJson`):

```csharp
var found = !string.IsNullOrEmpty(line.RawIndex)
    ? fileLines.FirstOrDefault(x => x.RawIndex == line.RawIndex)
    : fileLines.FirstOrDefault(x => x.Raw == line.Raw);
```

with the existing fallback-scan-by-split-text logic unchanged below it. This exactly restores
WanXiang's current `RegularDb`-branch merge behavior (match by `RawIndex`) for the new `RawJson`
type, while leaving every other type's merge behavior byte-for-byte identical (empty `RawIndex`
never matches another empty `RawIndex` accidentally in practice here because the fallback path is
only reached when the primary match fails, and CSV/PrefabText/DynamicStrings lines already rely on
`Raw` equality succeeding in the normal case).

## What stays out of scope

- The `...Tw`/`...Final` skip-suffix convention and the specific `Key`-is-usually-an-int convention
  are WanXiang-specific data conventions, not LlmKit-generic ones — implement them as default
  behavior in `JsonGameDataWorkflow` (since every current consumer of a `RawJson` file would be this
  game, until a second one exists), but flag in the LlmKit PR description that a future second JSON
  game may need these pulled out into `TextFileToSplit`-level options (similar to how
  `SkipColumns`/`CompoundFieldSplitterOptions` already exist for per-game CSV tuning) rather than
  hardcoded — don't build that generality speculatively now.
- Quality-review-pass (`Qc*` fields on `TranslationSplit`) interaction with `SplitPath`-addressed
  templates is structurally identical to the existing `Split`-addressed case (the QC pass already
  operates on whatever the anchor/`SubIndex == 0` fragment is) and needs no new design — just verify
  `QualityReviewHelpers.IsQcReviewFresh` and `QualityReviewWorkflow` don't assume `Split`-based
  grouping anywhere they'd need a `SplitPath`-based equivalent (a quick check during implementation,
  not a design decision).

## Sequencing

1. Land Piece 1 first (`FanslationStudio.LlmKit` PR + this repo's model swap for
   PrefabText/DynamicStrings), so this repo is already on LlmKit's `TranslationLine`/
   `TranslationSplit`/`TextFileToSplit` types before Piece 2 adds more fields to them.
2. `FanslationStudio.LlmKit` PR: `TranslationLine.RawIndex`, `TranslationSplit.SplitPath`,
   `FieldTemplate.SplitPath`, `TextFileType.RawJson`, `Workflow.JsonGameDataWorkflow`, the
   `MergeFilesIntoTranslatedAsync` `RawIndex`-match branch — with unit tests covering: loose/missing
   properties, `...Tw`/`...Final` skipping, array-element `SplitPath`s, and a round-trip
   export→package test against a small fixture matching this game's actual JSON shape.
3. This repo: migrate the ~35 `RegularDb` entries in `GameTextFiles.cs` to `TextFileType.RawJson`,
   replace `FileInputHandling`/`FileOutputHandling`'s `RegularDb` branches with calls into
   `JsonGameDataWorkflow`, delete the now-fully-dead local JSON handling code.
4. Do **not** attempt this against live/uncommitted `Files/Raw/Dumped/*.json` changes already present
   in the working tree at the start of this session — confirm with the user whether those are
   in-progress game-update data that should land separately first.
