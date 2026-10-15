# Thrive repository guide

Thrive is an evolution simulation game using Godot 4 and C#: Godot handles presentation,
audio, and UI; an Entity Component System (ECS) handles most gameplay simulation;
native C++ provides performance features and the Thrive Godot GDExtension.

## Repository layout

- `src/` contains game code and resources grouped primarily by stage. Shared systems live
  in `engine/`, `general/`, and `gui_common/`; native C++ lives in `src/native/`, and the
  Godot-facing C++ GDExtension lives in `src/extension/`.
- `assets/`, `shaders/`, and `locale/` contain game media and translations.
  Large binary assets use Git LFS.
- `simulation_parameters/` contains game definitions and balance data.
- `test/` contains code tests, Godot-facing tests, and benchmarks.
  `Scripts/` contains developer tools; `ThriveScriptsShared/` contains shared types and metadata.
- `third_party/` contains vendored dependencies and submodules.

## General requirements

The `doc/` directory is the primary technical reference.

For implementation and review, apply all matching reading routes below.
Read each referenced section in full, including subsections; skip unrelated sections
and sections already read for the current task.

Keep changes focused. Avoid unrelated formatting or refactoring, especially in generated files,
imported Godot resources, and vendored dependencies.

## Reading routes

- **Any repository edit** — [Git](doc/style_guide.md#git).
- **Source code** — [Pull requests](CONTRIBUTING.md#pull-requests).
  Read relevant existing comments before changing code, preserve them, and update any made
  inaccurate by the change.
- **C#** — Read the style guide introduction,
  [Code style rules](doc/style_guide.md#code-style-rules), and
  [Other recommended approaches](doc/style_guide.md#other-recommended-approaches).
  Follow nearby code for local patterns.
- **Non-code files**, including Markdown, JSON/configuration, Godot resources, and locale files —
  [Other files](doc/style_guide.md#other-files).
  Use project conventions for imported assets and generated files.
- **Frequent code** — Per-frame systems, callbacks, or native hot paths:
  [Memory allocation](doc/style_guide.md#memory-allocation).
- **Godot nodes, scenes, or resources** —
  [Godot usage](doc/style_guide.md#godot-usage).
  Check existing project patterns because Thrive extends Godot defaults.
  For unfamiliar workflows or editor behavior, read [Learning Godot](doc/learning_godot.md).
- **GUI behavior** — [Making Graphical User Interfaces](doc/making_guis.md).
- **Input handling** — Architecture [Input](doc/architecture.md#input) and
  [the input system guide](doc/input_system.md).
- **Gameplay, simulation, or stage behavior** —
  [Gameplay changes](doc/style_guide.md#gameplay-changes) and architecture
  [Folder Structure](doc/architecture.md#folder-structure).
- **ECS components, systems, world creation, or threaded simulation** —
  [Entity Component System](doc/architecture.md#entity-component-system-ecs).
  Keep components data-only and behavior in systems or same-file helpers.
  Create worlds with `ThriveWorld.Create()`, declare system access/order metadata,
  respect thread safety, and use command buffers for structural changes during updates.
- **Serialized data, save/load, or save compatibility** —
  [Saving System](doc/saving_system.md).
  ECS persistence also requires [Saving and Loading](doc/architecture.md#saving-and-loading).
  Preserve compatibility with older saves, adding a version change and upgrader when required.
- **Translations** —
  [How the translations work](doc/working_with_translations.md#how-the-translations-work),
  plus the relevant `Working in C# files`, `Working in scene files`,
  `Updating the localizations`, `Translate the text`, or
  `Translating the game into a new language` subsection.
  For localization tools, read the relevant `Gettext tools`, `Poedit`, or
  `Custom merge driver` subsection under
  [Localization tools](doc/setup_instructions.md#localization-tools).
  Use project localization APIs, extract keys, and update the English translation
  as described in the guide.
- **Auto-Evo algorithms or tuning** — [Auto-Evo](doc/auto_evo.md).
- **Native C++ or the C#/C++ boundary** —
  [Native Code](doc/architecture.md#native-code), including `C# Interop (P/Invoke)`.
  Follow `.clang-format` and nearby code.
- **Debug console commands** — [The Debug Console](doc/debug_console.md).
- **Profiling workflows** — [Profiling](doc/profiling.md).
- **Godot version changes** — [Updating Godot Version](doc/updating_godot_version.md)
  and [Godot .NET version](doc/setup_instructions.md#godot-net-version).
- **Builds, tests, formatting checks, or related setup and troubleshooting** —
  Read the matching [setup instructions](doc/setup_instructions.md) sections:
  C# builds require `Godot .NET version`, `Compiling C# Code`, and `C# packages`;
  code-only tests require `C# packages`;
  Godot tests/runs require the C# build setup plus `Native Libraries`;
  formatting checks require `Running the Format Checks`.
  For troubleshooting, read the relevant `Additional Tips` subsection.
- **Changes to existing Godot import settings or additions of C# dependencies** —
  [Continuous integration checks](CONTRIBUTING.md#continuous-integration-checks).
  Apply its cache-version update requirements, including the `writeTo` cache.
- **Binary game assets** — Read the README's
  `Modellers, texture and GUI artists, and Sound Engineers` section and
  setup instructions' `Git with LFS` section.
- **Releases** — [Making Releases](doc/making_releases.md) and
  [Exporting the game](doc/setup_instructions.md#exporting-the-game).

## Build and checks

Run checks relevant to the change when the environment permits.
Report which checks ran and which could not be run.

| Purpose | Command |
| --- | --- |
| Compile the solution | `dotnet build` |
| Code-only xUnit tests | `dotnet run --project Scripts -- test --godot false` |
| Godot tests | `dotnet run --project Scripts -- test --godot true` |
| Full repository checks | `dotnet run --project Scripts -- check` |
| Quick checks, recommended most of the time | `dotnet run --project Scripts -- check compile files localization rewrite` |

Quick checks cover a subset of the full checks and do not replace relevant tests.

Checks and tests can modify files: `rewrite` can change C# source, Godot test setup can
update generated files, and Godot startup can generate missing `.uid` files.
Review resulting differences before cleanup, preserve pre-existing work, and retain only
task-related generated changes.

Godot tests/runs require the Godot .NET executable on `PATH` as `godot-mono` or `godot`;
the test runner prefers `godot-mono`.

## Code review

When reviewing a change or PR, read all of
[`.github/copilot-instructions.md`](.github/copilot-instructions.md).
