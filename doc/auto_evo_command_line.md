# Running Auto-Evo from the command line

Use a prepared Debug checkout with the required Godot .NET version, compiled C# code,
installed native libraries and imported assets (see [setup instructions](setup_instructions.md)):

```sh
godot --headless --path /path/to/Thrive -- --auto-evo --generations=2 --seed=12345
```

Everything after `--` is a game option. `--generations=N` must be a positive 32-bit integer
and defaults to one. `--seed=N` accepts a signed 64-bit integer; omission uses
`WorldGenerationSettings`' existing random default. The actual seed is reported.
Duplicate, unknown and malformed options fail before constructing the world.
A seed identifies the initial world settings; it does not promise identical evolution.

The command creates one microbe world and advances it sequentially. Each generation computes
Auto-Evo, finalises external effects, advances world time, applies results, increments the
player species generation, and commits domain history. The player species is retained on
extinction, as in the exploring tool. No main menu or menu background simulation is started.

The log contains one line beginning with `AUTO_EVO_RESULT `, followed by JSON:

- `Success`, `RequestedGenerations`, `CompletedGenerations`, and the actual `Seed`.
- `Generations`: only fully completed generations, each with `Input` and `Output`
  population records, player species generation, world time and history count.
- `ComputeSeconds`: the completed run's fixed `RunDuration`, from worker start to
  computation completion. It excludes queueing and late observation.
- `WallSeconds`: from starting that generation's run to completing its domain history.
  Includes queueing, observation waiting and application; excludes world creation and reporting.
- `Error`: failure details, or null on success. Computation errors also appear in the log.

The initial player generation is one; two completed generations produce player generation three.
Input/output records are reports, not world saves.

Success exits with code zero. Invalid arguments, failed or cancelled computation, and failed
application exit nonzero and retain completed generation reports. Ctrl+C (when attached to a
console) and a window close request cancel the command; it waits for accepted Auto-Evo work
to finish before reporting. Force-killing the process cannot produce a final report.
A failed application may already have changed the world. That world is discarded, never
continued or exported as a successful input. GUI callers retain their existing error policy.

This entry point does not build targets, launch child processes, restore mature inputs or run
benchmarks. Windows is the currently validated platform; Linux and macOS have not been run.
