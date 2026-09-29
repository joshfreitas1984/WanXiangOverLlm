# Translation workflow execution reference

`Translate/` is the reusable game-specific adapter around the sibling `FanslationStudio.LlmKit` library. It declares the WanXiang text files in `GameTextFiles`, applies game hooks in `GameFileHandling`, and delegates shared extraction, translation, quality review, and packaging mechanics to LlmKit workflows.

`Tests/` contains the numbered workflow facts and regression tests. These files may read or mutate the `Files/` working directory and may call a live LLM; do not run them as a normal build check.

## Working-directory boundaries

- `Files/Raw/` contains dumped source data and regenerable exported input.
- `Files/Converted/` contains translation state that must be preserved across runs.
- `Files/Mod/` contains generated package output for `EnglishPatch` or the game.
- `Files/TestResults/` contains workflow evidence and diagnostic output.

## Project boundaries

- `Translate/` owns WanXiang file declarations, game-specific repair/validation hooks, and adapters for LlmKit.
- `Tests/` owns workflow orchestration facts and pure regression coverage.
- `SharedAssembly/` owns contracts shared with runtime code.
- `EnglishPatch/` owns runtime injection and Harmony patches.
- Generic line/split/template behavior and LlmKit workflow implementations remain in the sibling repository.

The stable project reference is `..\\..\\FanslationStudio.LlmKit\\FanslationStudio.LlmKit\\FanslationStudio.LlmKit.csproj` from both `Translate/` and `Tests/` where needed. Do not replace it with a package reference.
