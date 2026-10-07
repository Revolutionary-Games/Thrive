# Thrive repository guide

Thrive is an evolution simulation game built with Godot 4. The main game is written in C# 
and uses Godot scenes and nodes for presentation, audio, and UI. Gameplay simulation is 
primarily implemented with an Entity Component System (ECS). C++ code provides native 
performance features and the Thrive Godot GDExtension.

## Repository layout

- `src/` contains game code and resources. Most gameplay is grouped by stage (`microbe_stage/`, `multicellular_stage/`, `awakening_stage/`, and later stages); shared systems live in `engine/`, `general/`, and `gui_common/`. `src/native/` contains native C++, and `src/extension/` contains the Godot-facing C++ GDExtension.
- `assets/`, `shaders/`, and `locale/` contain game assets, shaders, and translations. Large binary assets use Git LFS.
- `simulation_parameters/` contains data used to tune and define game systems.
- `test/` contains code tests, Godot-facing tests, and benchmarks. `Scripts/` contains developer and build tools; `ThriveScriptsShared/` contains shared types and metadata.
- `third_party/` contains vendored dependencies and submodules.

## Before changing code

- Read [the contribution guide](CONTRIBUTING.md) and follow [the style guide](doc/style_guide.md). Use nearby code as the guide for local patterns, preserve useful comments, and update comments made inaccurate by a change.
- Read [the architecture guide](doc/architecture.md) before changing gameplay simulation or ECS code. Components are data containers; put behavior in systems or the component's same-file helper. Create worlds with `ThriveWorld.Create()`. Declare accurate system ordering and component read/write access, and use command buffers for structural entity changes during updates.
- Thrive extends some built-in Godot patterns. Check the relevant docs in `doc/` and existing code before using a default Godot approach. In particular, read [the input system guide](doc/input_system.md) for input changes and [the GUI guide](doc/making_guis.md) for UI changes.
- Treat save compatibility as part of the feature when changing serialized data. Follow 
  [the saving system guide](doc/saving_system.md), including archive versioning and defaults for older saves; add a version change when required.
- For user-visible text, follow [the translation guide](doc/working_with_translations.md). Use the project's localization APIs and update extracted keys and the English translation as described there.
- Keep changes focused. Avoid unrelated formatting or refactoring, especially in generated files, imported Godot resources, or vendored dependencies.

## Build and checks

Common repository checks include:

- `dotnet build` to compile the solution.
- `dotnet run --project Scripts -- test --godot false` to run the code-only xUnit tests.
- `dotnet run --project Scripts -- test --godot true` to run the unit tests that require the 
  Godot runtime. This also generates any missing .uid files.
- `dotnet run --project Scripts -- check` to run the repository formatting and code checks. 
  This takes a while so a lighter variant, which is recommended most of the time, is: 
 `dotnet run --project Scripts -- check compile files localization rewrite` 

If the Godot-using tests do not work correctly, 
check [the setup instructions](doc/setup_instructions.md) for info on Godot installation 
and other dependencies. `godot-mono` or `godot` must be available in `PATH` for the tool to 
run correctly (the test runner prefers `godot-mono`). 

Run checks relevant to the change when the environment permits, and report any checks that could not be run.

## Code review

When reviewing changes, read and follow [`.github/copilot-instructions.md`](.github/copilot-instructions.md). It contains Thrive's dedicated PR review guide, including architecture-specific pitfalls and review scope for translations, shaders, and CI-covered checks.

## Further documentation

The `doc/` directory is the primary technical reference. In addition to the guides linked above, consult [learning Godot](doc/learning_godot.md), [Auto-Evo](doc/auto_evo.md), [profiling](doc/profiling.md), and the other topic-specific documents when working in those areas.
