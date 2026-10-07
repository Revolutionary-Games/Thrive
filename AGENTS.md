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

Read the shared requirements and then every task-specific route that applies. Tasks spanning
several areas combine their routes. For each named section, locate it in the document and read
the complete section, including all subsections; a link or heading name is only a navigation
aid. Do not read unrelated sections just because they share a document.

### Shared requirements

- Before editing any repository file, read the complete [Git section](doc/style_guide.md#git) of the style guide.
- Before changing source code, read the complete [Pull requests section](CONTRIBUTING.md#pull-requests)
  of the contribution guide. Preserve relevant comments around changed code and update comments
  made inaccurate by the change.
- Before changing C# code, also read the style guide's introduction (from the title through the
  text immediately before "Code style rules"), the complete [Code style rules](doc/style_guide.md#code-style-rules)
  section, and the complete [Other recommended approaches](doc/style_guide.md#other-recommended-approaches)
  section. Use nearby code for local patterns.
- Before adding or editing non-code files such as Markdown documentation, JSON/configuration data,
  Godot scenes/resources, or locale files, read the complete [Other files](doc/style_guide.md#other-files)
  section. C++ source files follow the native code route below. For imported assets and generated
  files, follow existing project conventions and avoid unrelated changes.

### Task-specific requirements

- For code that runs every frame or otherwise runs frequently, additionally read the complete
  [Memory allocation](doc/style_guide.md#memory-allocation) section. This applies to frequent
  ECS systems, Godot callbacks, and hot paths in native code.
- When changing Godot nodes, scenes, or resources, additionally read the complete
  [Godot usage](doc/style_guide.md#godot-usage) section. For a new Godot workflow or when Godot
  editor behavior is unfamiliar, read all of [Learning Godot](doc/learning_godot.md) as well.
- When adding or changing GUI behavior, also read all of
  [Making Graphical User Interfaces](doc/making_guis.md), including its focus, navigation, and
  tabs subsections. Combine this with the Godot usage route for scene or node changes.
- When changing input handling, read the complete [Input section](doc/architecture.md#input) of
  the architecture guide and all of [the input system guide](doc/input_system.md), in addition to
  the C# and Godot routes that apply.
- When changing gameplay rules, simulation behavior, or stage behavior, read the complete
  [Gameplay changes](doc/style_guide.md#gameplay-changes) and [Folder Structure](doc/architecture.md#folder-structure)
  sections.
- When changing ECS components, systems, world creation, or threaded simulation, read the complete
  [Entity Component System (ECS)](doc/architecture.md#entity-component-system-ecs) section, even
  when the change does not alter gameplay rules. Follow its rules for data-only components,
  `ThriveWorld.Create()`, system access/order metadata, thread safety, and command buffers.
- When changing serialized data, save/load behavior, or compatibility with existing saves, read
  all of [the Saving System guide](doc/saving_system.md). If changing ECS component persistence,
  also read the architecture guide's complete [Saving and Loading](doc/architecture.md#saving-and-loading)
  subsection. Keep older saves compatible or add the required version change and upgrader.
- For new or changed translatable strings in C# code, read the complete `Working in C# files`
  subsection in [the translation guide](doc/working_with_translations.md). For text in Godot
  scenes, read its complete `Working in scene files` subsection. If adding or changing translation
  keys, also read the complete `Updating the localizations` subsection. In all these cases, read
  the complete `How the translations work` section of the translation guide. When editing `.po`
  text, also read the complete `Translate the text` subsection; when adding a language, read the
  complete `Translating the game into a new language` section, including all its subsections. For
  localization tool setup, read only the matching subsection of `Localization tools` in
  `doc/setup_instructions.md` (`Gettext tools`, `Poedit`, or `Custom merge driver`).
- When changing Auto-Evo algorithms, selection pressures, mutations, or their tuning, read all of
  [Auto-Evo](doc/auto_evo.md), in addition to the gameplay and C# routes that apply.
- When changing native C++ code or the C#/C++ boundary, read the architecture guide's complete
  [Native Code](doc/architecture.md#native-code) section, including its `C# Interop (P/Invoke)`
  subsection. Follow the repository `.clang-format` and nearby native code.
- When adding or changing debug console commands, read all of [The Debug Console](doc/debug_console.md).
- When investigating or changing profiling workflows, read all of [Profiling](doc/profiling.md).
- When changing Godot versions, read all of [Updating Godot Version](doc/updating_godot_version.md) and
  the [Godot .NET version](doc/setup_instructions.md#godot-net-version) section of setup instructions.
- When building the C# solution, read `Godot .NET version`, `Compiling C# Code`, and `C# packages`
  in [the setup instructions](doc/setup_instructions.md). For code-only tests, read `C# packages`;
  for game runs or Godot-using tests, also read `Native Libraries` and make the required Godot .NET
  executable available on `PATH`. For formatting checks, read the complete `Running the Format Checks`
  section. When troubleshooting, read only the subsection under `Additional Tips` that matches the
  failure. Do not read the entire setup guide by default.
- When adding or updating binary game assets, read the complete `Modellers, texture and GUI artists,
  and Sound Engineers` section of [README.md](README.md) and the `Git with LFS` section of
  `doc/setup_instructions.md`.
- When packaging a release, read all of [Making Releases](doc/making_releases.md) and the
  [Exporting the game](doc/setup_instructions.md#exporting-the-game) section of the setup instructions.

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
