using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Manages applying patch data and setting up spawns
/// </summary>
public class PatchManager : IChildPropertiesLoadCallback
{
    private SpawnSystem spawnSystem;
    private ProcessSystem processSystem;
    private CompoundCloudSystem compoundCloudSystem;
    private TimedLifeSystem timedLife;
    private DirectionalLight worldLight;

    [JsonProperty]
    private Patch previousPatch;

    /// <summary>
    ///   Used to detect when an old save is loaded and we can't rely on the new logic for despawning things
    /// </summary>
    private bool skipDespawn;

    // Currently active spawns
    private List<CreatedSpawner> chunkSpawners = new List<CreatedSpawner>();
    private List<CreatedSpawner> cloudSpawners = new List<CreatedSpawner>();
    private List<CreatedSpawner> microbeSpawners = new List<CreatedSpawner>();

    public PatchManager(SpawnSystem spawnSystem, ProcessSystem processSystem,
        CompoundCloudSystem compoundCloudSystem, TimedLifeSystem timedLife, DirectionalLight worldLight,
        GameProperties currentGame)
    {
        this.spawnSystem = spawnSystem;
        this.processSystem = processSystem;
        this.compoundCloudSystem = compoundCloudSystem;
        this.timedLife = timedLife;
        this.worldLight = worldLight;
        CurrentGame = currentGame;
    }

    public GameProperties CurrentGame { get; set; }

    public void OnNoPropertiesLoaded()
    {
        skipDespawn = true;
    }

    /// <summary>
    ///   Applies all patch related settings that are needed to be
    ///   set. Like different spawners, despawning old entities if the
    ///   patch changed etc.
    /// </summary>
    /// <returns>
    ///   True if the patch is changed from the previous one. False if the patch is not changed.
    /// </returns>
    public bool ApplyChangedPatchSettingsIfNeeded(Patch currentPatch)
    {
        var patchIsChanged = false;

        if (previousPatch != currentPatch && !skipDespawn)
        {
            if (previousPatch != null)
            {
                GD.Print("Previous patch (", TranslationServer.Translate(previousPatch.Name), ") different to " +
                    "current patch (", TranslationServer.Translate(currentPatch.Name), ") despawning all entities.");
            }
            else
            {
                GD.Print("Previous patch doesn't exist, despawning all entities.");
            }

            // Despawn old entities
            spawnSystem.DespawnAll();

            // And also all timed entities
            timedLife.DespawnAll();

            // Clear compounds
            compoundCloudSystem.EmptyAllClouds();

            patchIsChanged = true;
        }

        previousPatch = currentPatch;
        skipDespawn = false;

        GD.Print("Applying patch (", TranslationServer.Translate(currentPatch.Name), ") settings");

        // Update environment for process system
        processSystem.SetBiome(currentPatch.Biome);

        // Apply spawn system settings
        UnmarkAllSpawners();

        // Cloud spawners should be added first due to the way the
        // total entity count is limited
        HandleCloudSpawns(currentPatch.Biome);
        HandleChunkSpawns(currentPatch.Biome);
        HandleCellSpawns(currentPatch);

        RemoveNonMarkedSpawners();

        // Change the lighting
        UpdateLight(currentPatch.BiomeTemplate);

        return patchIsChanged;
    }

    private void HandleChunkSpawns(BiomeConditions biome)
    {
        GD.Print("Number of chunks in this patch = ", biome.Chunks.Count);

        foreach (var entry in biome.Chunks)
        {
            HandleSpawnHelper(chunkSpawners, entry.Value.Name, () =>
            {
                var spawner = new CreatedSpawner(entry.Value.Name);
                spawner.Spawner = Spawners.MakeChunkSpawner(entry.Value, compoundCloudSystem);
                spawnSystem.AddSpawnType(spawner.Spawner);
                return spawner;
            });
        }
    }

    private void HandleCloudSpawns(BiomeConditions biome)
    {
        GD.Print("Number of clouds in this patch = ", biome.Compounds.Count);

        foreach (var entry in biome.Compounds.Where(c => c.Key.IsCloud))
        {
            HandleSpawnHelper(cloudSpawners, entry.Key.InternalName, () =>
            {
                var spawner = new CreatedSpawner(entry.Key.InternalName);
                spawner.Spawner = Spawners.MakeCompoundSpawner(entry.Key, compoundCloudSystem, entry.Value.Amount);
                spawnSystem.AddSpawnType(spawner.Spawner);
                return spawner;
            });
        }
    }

    private void HandleCellSpawns(Patch patch)
    {
        GD.Print("Number of species in this patch = ", patch.SpeciesInPatch.Count);

        foreach (var entry in patch.SpeciesInPatch)
        {
            var species = entry.Key;
            var populationHere = entry.Value;

            if (populationHere <= 0)
            {
                GD.Print(entry.Key.FormattedName, " population <= 0. Skipping Cell Spawn in patch.");
                continue;
            }

            var name = species.ID.ToString(CultureInfo.InvariantCulture);

            HandleSpawnHelper(microbeSpawners, name, () =>
            {
                var spawner = new CreatedSpawner(name);
                spawner.Spawner =
                    Spawners.MakeMicrobeSpawner(species, compoundCloudSystem, CurrentGame, populationHere);
                spawnSystem.AddSpawnType(spawner.Spawner);
                return spawner;
            });
        }
    }

    private void HandleSpawnHelper(List<CreatedSpawner> existingSpawners, string itemName,
        Func<CreatedSpawner> createNew)
    {
        var existing = existingSpawners.Find(s => s.Name == itemName);

        if (existing != null)
        {
            existing.Marked = true;
        }
        else
        {
            // New spawner needed
            GD.Print("Registering new spawner: Name: ", itemName);

            existingSpawners.Add(createNew());
        }
    }

    private void UpdateLight(Biome biome)
    {
        if (biome.Sunlight == null)
        {
            GD.PrintErr("biome has no sunlight parameters");
            return;
        }

        worldLight.Translation = new Vector3(0, 0, 0);
        worldLight.LookAt(biome.Sunlight.Direction, new Vector3(0, 1, 0));

        worldLight.ShadowEnabled = biome.Sunlight.Shadows;

        worldLight.LightColor = biome.Sunlight.Colour;
        worldLight.LightEnergy = biome.Sunlight.Energy;
        worldLight.LightSpecular = biome.Sunlight.Specular;
    }

    private void UnmarkAllSpawners()
    {
        UnmarkSingle(chunkSpawners);
        UnmarkSingle(cloudSpawners);
        UnmarkSingle(microbeSpawners);
    }

    private void UnmarkSingle(List<CreatedSpawner> spawners)
    {
        foreach (var spawner in spawners)
            spawner.Marked = false;
    }

    private void RemoveNonMarkedSpawners()
    {
        ClearUnmarkedSingle(chunkSpawners);
        ClearUnmarkedSingle(cloudSpawners);
        ClearUnmarkedSingle(microbeSpawners);
    }

    /// <summary>
    /// Removes unmarked spawners from List.
    /// </summary>
    /// <param name="spawners">Spawner list to act upon</param>
    private void ClearUnmarkedSingle(List<CreatedSpawner> spawners)
    {
        spawners.RemoveAll(item =>
        {
            if (!item.Marked)
            {
                GD.Print("Removed ", item.Name, " spawner.");
                item.Spawner.DestroyQueued = true;
                return true;
            }

            return false;
        });
    }

    private class CreatedSpawner
    {
        public Spawner Spawner;
        public string Name;
        public bool Marked = true;

        public CreatedSpawner(string name)
        {
            Name = name;
        }
    }
}
