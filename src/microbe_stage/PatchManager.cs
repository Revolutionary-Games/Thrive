using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Manages applying patch data and setting up spawns
/// </summary>
public class PatchManager
{
    private SpawnSystem spawnSystem;
    private ProcessSystem processSystem;

    private Patch previousPatch;

    // Currently active spawns
    private List<CreatedSpawner> chunkSpawners = new List<CreatedSpawner>();
    private List<CreatedSpawner> cloudSpawners = new List<CreatedSpawner>();
    private List<CreatedSpawner> microbeSpawners = new List<CreatedSpawner>();

    public PatchManager(SpawnSystem spawnSystem, ProcessSystem processSystem)
    {
        this.spawnSystem = spawnSystem;
        this.processSystem = processSystem;
    }

    /// <summary>
    ///   Applies all patch related settings that are needed to be
    ///   set. Like different spawners, despawning old entities if the
    ///   patch changed etc.
    /// </summary>
    public void ApplyChangedPatchSettingsIfNeeded(Patch currentPatch)
    {
        if (previousPatch != currentPatch)
        {
            // Despawn old entities
            throw new NotImplementedException();
        }

        previousPatch = currentPatch;

        GD.Print("Applying patch (", currentPatch.Name, ") settings");

        // Update environment for process system
        processSystem.SetBiome(currentPatch.Biome);

        // Apply spawn system settings
        UnmarkAllSpawners();

        HandleChunkSpawns(currentPatch.Biome);
        HandleCloudSpawns(currentPatch.Biome);
        HandleCellSpawns(currentPatch);

        RemoveNonMarkedSpawners();

        // // Change the lighting
        // updateLight(biome);

        // // Changing the background.
        // ThriveGame::get()->setBackgroundMaterial(biome.background);

        // updateBiomeStatsForGUI(biome);
        // updateCurrentPatchInfoForGUI(*patch);
    }

    private void HandleChunkSpawns(Biome biome)
    {
        GD.Print("Number of chunks in this patch = ", biome.Chunks.Count);

        foreach (var entry in biome.Chunks)
        {
            if (entry.Value.Density <= 0)
            {
                GD.Print("chunk spawn density is 0. It won't spawn");
                continue;
            }

            var existing = chunkSpawners.Find((c) => c.Name == entry.Value.Name);

            if (existing != null)
            {
                existing.Marked = true;

                // TODO: change this in json to be int to not have this cast here
                existing.Spawner.SetFrequencyFromDensity((int)entry.Value.Density);
            }
            else
            {
                // New spawner needed
                GD.Print("Registering chunk: Name: ", entry.Value.Name,
                    " density: ", entry.Value.Density);

                var spawner = new CreatedSpawner();
                spawner.Marked = true;
                spawner.Name = entry.Value.Name;
                spawner.Spawner = new ChunkSpawner(entry.Value);

                spawnSystem.AddSpawnType(spawner.Spawner, (int)entry.Value.Density,
                    Constants.MICROBE_SPAWN_RADIUS);

                chunkSpawners.Add(spawner);
            }
        }
    }

    private void HandleCloudSpawns(Biome biome) { }

    private void HandleCellSpawns(Patch patch) { }

    private void UpdateLight(Biome biome) { }

    private void UpdateBiomeStatsForGUI(Biome biome) { }

    private void UpdateCurrentPatchInfoForGUI(Patch patch) { }

    private void UnmarkAllSpawners() { }

    private void UnmarkSingle(List<ISpawner> spawners) { }

    private void RemoveNonMarkedSpawners() { }

    private void ClearUnmarkedSingle(List<ISpawner> spawners) { }

    private class CreatedSpawner
    {
        public ISpawner Spawner;
        public string Name;
        public bool Marked;
    }
}
