# WanXiang downstream project structure

The repository follows the downstream layout used by DragonHierOverLlm:

- `Translate/` contains reusable game-specific translation code.
- `Tests/` contains workflow/manual facts and regression tests.
- `SharedAssembly/` contains contracts shared with the runtime plugin.
- `EnglishPatch/` contains game-runtime patches.
- `Files/` contains working translation data.
- `docs/` is organized by feature, investigation, plan, and architecture purpose.

The solution includes the local projects and the sibling `FanslationStudio.LlmKit` project by relative project reference. Shared-library behavior is documented in the sibling repository rather than copied here.
