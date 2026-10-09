# Thrive repository guide

Thrive is an evolution simulation game built with Godot 4. The main game is written in C# 
and uses Godot scenes and nodes for presentation, audio, and UI. Gameplay simulation is 
primarily implemented with an Entity Component System (ECS). C++ code provides native 
performance features and the Thrive Godot GDExtension.

## Repository layout

- `src/` contains game code and resources. Most gameplay is grouped by stage
  (`microbe_stage/`, `multicellular_stage/`, `awakening_stage/`, and later stages); shared
  systems live in `engine/`, `general/`, and `gui_common/`. `src/native/` contains native
  C++, and `src/extension/` contains the Godot-facing C++ GDExtension.
- `assets/`, `shaders/`, and `locale/` contain game assets, shaders, and translations. Large binary assets use Git LFS.
- `simulation_parameters/` contains data used to tune and define game systems.
- `test/` contains code tests, Godot-facing tests, and benchmarks. `Scripts/` contains
  developer and build tools; `ThriveScriptsShared/` contains shared types and metadata.
- `third_party/` contains vendored dependencies and submodules.

## Reading requirements

Apply the shared requirements and every task-specific route that fits the change; combine routes
when work spans areas. Read each named section in full, including its subsections, but skip
unrelated sections in the same document.

### Shared requirements

- Any repository edit: style guide [Git section](doc/style_guide.md#git). Source code: contribution
  guide [Pull requests section](CONTRIBUTING.md#pull-requests); preserve relevant comments and
  update any made inaccurate by the change.
- C# changes: style guide introduction through the text before
  [Code style rules](doc/style_guide.md#code-style-rules), and [Other recommended
  approaches](doc/style_guide.md#other-recommended-approaches); follow nearby code for local
  patterns.
- Non-code files (including Markdown, JSON/configuration, Godot resources, and locale files):
  style guide [Other files](doc/style_guide.md#other-files). C++ follows the native code 
  route (and uses `clang-format`); use project conventions for imported assets and generated files.

### Task-specific requirements

- Frequent code (per-frame systems, callbacks, or native hot paths): style guide
  [Memory allocation](doc/style_guide.md#memory-allocation).
- Godot nodes/scenes/resources: style guide [Godot usage](doc/style_guide.md#godot-usage);
  check existing project patterns because Thrive extends Godot defaults. For unfamiliar workflows
  or editor behavior, read [Learning Godot](doc/learning_godot.md).
- GUI behavior: [Making Graphical User Interfaces](doc/making_guis.md), plus the Godot route for
  scene/node changes. Input handling:
  architecture [Input](doc/architecture.md#input) and [the input system guide](doc/input_system.md).
- Gameplay, simulation, or stage behavior: style guide
  [Gameplay changes](doc/style_guide.md#gameplay-changes) and architecture
  [Folder Structure](doc/architecture.md#folder-structure).
- ECS components, systems, world creation, or threaded simulation: architecture
  [Entity Component System](doc/architecture.md#entity-component-system-ecs). Keep components
  data-only; put behavior in systems or same-file helpers, create worlds with `ThriveWorld.Create()`,
  declare system access/order metadata, respect thread safety, and use command buffers for
  structural changes during updates.
- Serialized data, save/load, or save compatibility: [Saving System](doc/saving_system.md); ECS
  persistence also needs architecture [Saving and Loading](doc/architecture.md#saving-and-loading).
  Keep older saves compatible, adding a version change and upgrader when required.
- Translations: [How the translations work](doc/working_with_translations.md#how-the-translations-work),
  plus the relevant `Working in C# files`, `Working in scene files`, `Updating the localizations`,
  `Translate the text`, or `Translating the game into a new language` subsection. For localization
  tools, read only the relevant `Gettext tools`, `Poedit`, or `Custom merge driver` subsection under
  setup [Localization tools](doc/setup_instructions.md#localization-tools). Use project localization
  APIs, extract keys, and update the English translation as described in the guide.
- Auto-Evo algorithms or tuning: [Auto-Evo](doc/auto_evo.md), plus applicable gameplay and C# routes.
- Native C++ or the C#/C++ boundary: architecture [Native Code](doc/architecture.md#native-code),
  including `C# Interop (P/Invoke)`; follow `.clang-format` and nearby code.
- Debug console commands: [The Debug Console](doc/debug_console.md). Profiling workflows:
  [Profiling](doc/profiling.md).
- Godot version changes: [Updating Godot Version](doc/updating_godot_version.md) and setup
  [Godot .NET version](doc/setup_instructions.md#godot-net-version).
- C# builds: setup `Godot .NET version`, `Compiling C# Code`, and `C# packages`; code-only tests
  need `C# packages`; Godot tests/runs also need `Native Libraries` and Godot .NET on `PATH`.
  Formatting checks: `Running the Format Checks`; troubleshooting: only the relevant
  `Additional Tips` subsection.
- Binary game assets: README `Modellers, texture and GUI artists, and Sound Engineers` and setup
  `Git with LFS`. Releases: [Making Releases](doc/making_releases.md) and setup
  [Exporting the game](doc/setup_instructions.md#exporting-the-game).

Keep changes focused. Avoid unrelated formatting or refactoring, especially in generated files,
imported Godot resources, and vendored dependencies.

## Build and checks

Common repository checks include:

- `dotnet build` to compile the solution.
- `dotnet run --project Scripts -- test --godot false` to run the code-only xUnit tests.
- `dotnet run --project Scripts -- test --godot true` to run the unit tests that require the 
  Godot runtime. This also generates any missing .uid files.
- `dotnet run --project Scripts -- check` to run the repository formatting and code checks. 
  This takes a while so a lighter variant, which is recommended most of the time, is: 
 `dotnet run --project Scripts -- check compile files localization rewrite` 

If the Godot-using tests do not work correctly, follow the build/test setup route above. The
Godot .NET executable must be available on `PATH` as `godot-mono` or `godot` (the test runner
prefers `godot-mono`).

Run checks relevant to the change when the environment permits, and report any checks that could not be run.

## Code review

When reviewing a change or PR, read all of
[`.github/copilot-instructions.md`](.github/copilot-instructions.md). Also apply the task-specific
reading routes above for every changed area; the review guide's link does not replace reading
applicable source sections.

Automatic CI will run tests and ensure the code compiles.

## Further documentation

The `doc/` directory is the primary technical reference. Use the task-specific routes above to
select the complete sections needed for a change.
