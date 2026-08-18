# Thrive PR Review Guide

## Purpose

- Thrive is an open-source evolution-simulation game built with Godot 4. It combines gameplay simulation, GUI, assets, and a small native-performance module.

## Architecture

- Godot scene tree for presentation, audio, and UI; gameplay simulation uses an ECS architecture.
- ECS components are data-only structs; behaviour belongs in systems or same-file extension helpers.
- Systems declare ordering and component read/write access for generated parallel execution; structural entity changes use command buffers.
- Stage-oriented code: place features in the earliest game stage that needs them, not necessarily the newest consumer.

## Key directories

- `src/` — game C#, Godot scenes/resources, organised by stage; `engine/` provides shared lower-level systems.
- `simulation_parameters/` — registry and gameplay tuning data (JSON/YAML).
- `test/` — xUnit code tests, Godot/gdUnit tests, benchmarks, and experimental scenes.
- `ThriveScriptsShared/`, `Scripts/` — shared build metadata/types and developer tooling.
- `native_libs/`, `src/native/`, `third_party/` — C++ native module, interop, and vendored dependencies.
- `assets/`, `shaders/`, `locale/` — game media, shaders, and translations.

## Stack & testing

- C#/.NET 10 with Godot 4, Arch ECS, Newtonsoft.Json/YamlDotNet; C++/CMake for native performance and Jolt physics interop.
- Tests use xUnit under `test/code_tests`; Godot-facing tests use gdUnit4. Require coverage for changed logic where practical, especially save/load, simulation, and utilities.

## Conventions

- Follow `doc/style_guide.md`; CI already enforces C# formatting and compilation.
- C# uses PascalCase types/files, British English (except `meter`), 120-column lines, `//`/XML documentation, and normally no namespaces.
- Avoid `async`; use `TaskExecutor`. Avoid unnecessary LINQ, interfaces in hot loops, and per-frame allocations; reuse containers.
- Use `ref` for ECS components and `this ref` extension helpers. Update the relevant dirty flags through established helpers.
- For Godot, use containers rather than Control offsets; use `Connect`/`nameof` for signals; never dispose Nodes or `GD.Load` resources.

## Review Focus

When reviewing code of a PR pay most attention to the following: logic flow of the code to check for logical errors, typos in code identifiers or comments, and code efficiency (Thrive is a game so avoiding temporary memory allocations as much as possible is important; persistent, reused data containers should be preferred).

Automated CI checks ensure code compiles and is formatted correctly (except for shader files, for those check they match the style of existing shader files in the shaders folder). Translation files in the locale folder don't need to be checked, other than `en.po` to see if the English text is grammatically fine and has no typos, as they also have automated checks to ensure no missing text. So don't bother reading other `.po` files than the `en.po` as they are not required to be reviewed.

## PR rules

- PRs should be focused on one issue/change; do not mix unrelated refactors or formatting. Issue branches use `<issue>_<short_lowercase_name>`.
- Changed code must preserve and update nearby documentation/comments.
- Save-format changes must retain compatibility or include an upgrader and required version/subversion change.
- New C# files must include the Godot-generated matching `.cs.uid` file.
- Do not include locale `.po` changes that only update source-reference line numbers.

## Flag common pitfalls

- Logic errors, identifier/comment typos, and avoidable allocations—especially in update systems.
- ECS systems with missing/incorrect access or ordering metadata, direct world construction instead of `ThriveWorld.Create()`, structural component changes during updates, or unreleased command-buffer recorders.
- Component copies instead of `ref`, missing dirty-flag updates, or saved runtime-only visual state.
- Translated strings passed to `string.Format`; use `LocalizedString`, `LocalizedStringBuilder`, or `FormatSafe`.
- `.tscn` changes that make an intentionally hidden node visible by changing/removing `visible = false` without a clear reason.
- Godot signal subscriptions using `+=`, static parent references, Node disposal, or UI that only works with mouse.
- Shader style inconsistencies: shaders are not covered by the normal formatter.

## Out of scope

- Do not review non-English `.po` files; review `locale/en.po` only for English grammar and typos.
- Do not duplicate CI feedback for C# compilation/formatting or translation completeness unless it reveals a substantive problem.
