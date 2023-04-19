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
    // Currently active spawns
    private readonly List<CreatedSpawner> chunkSpawners = new();
    private readonly List<CreatedSpawner> cloudSpawners = new();
    private readonly List<CreatedSpawner> microbeSpawners = new();

    private SpawnSystem spawnSystem;
    private ProcessSystem processSystem;
    private CompoundCloudSystem compoundCloudSystem;
    private TimedLifeSystem timedLife;
    private DirectionalLight worldLight;

    [JsonProperty]
    private Patch? previousPatch;

    [JsonProperty]
    private float compoundCloudBrightness = 1.0f;

    /// <summary>
    ///   Used to detect when an old save is loaded and we can't rely on the new logic for despawning things
    /// </summary>
    private bool skipDespawn;

    public PatchManager(SpawnSystem spawnSystem, ProcessSystem processSystem,
        CompoundCloudSystem compoundCloudSystem, TimedLifeSystem timedLife, DirectionalLight worldLight,
        GameProperties? currentGame)
    {
        this.spawnSystem = spawnSystem;
        this.processSystem = processSystem;
        this.compoundCloudSystem = compoundCloudSystem;
        this.timedLife = timedLife;
        this.worldLight = worldLight;
        CurrentGame = currentGame;
    }

    public GameProperties? CurrentGame { get; set; }

    public void OnNoPropertiesLoaded()
    {
        skipDespawn = true;
    }

    public void OnPropertiesLoaded()
    {
    }

    /// <summary>
    ///   Applies all patch related settings that are needed to be set. Like different spawners, despawning old
    ///   entities if the patch changed etc.
    /// </summary>
    /// <param name="currentPatch">The patch to apply settings from</param>
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
                GD.Print("Previous patch (", previousPatch.Name.ToString(),
                    ") different to " + "current patch (", currentPatch.Name.ToString(), ") despawning all entities.");
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

        GD.Print($"Applying patch ({currentPatch.Name}) settings");

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
        compoundCloudBrightness = currentPatch.BiomeTemplate.CompoundCloudBrightness;

        UpdateAllPatchLightLevels();

        return patchIsChanged;
    }

    public void UpdatePatchBiome(Patch currentPatch)
    {
        // Update environment for the process system
        processSystem.SetBiome(currentPatch.Biome);
    }

    public void UpdateAllPatchLightLevels()
    {
        if (CurrentGame == null)
            throw new InvalidOperationException($"{nameof(PatchManager)} doesn't have {nameof(CurrentGame)} set");

        var gameWorld = CurrentGame.GameWorld;

        if (!gameWorld.WorldSettings.DayNightCycleEnabled)
            return;

        var multiplier = gameWorld.LightCycle.DayLightFraction;
        compoundCloudSystem.SetBrightnessModifier(multiplier * (compoundCloudBrightness - 1.0f) + 1.0f);
        gameWorld.UpdateGlobalLightLevels();
    }

    private void HandleChunkSpawns(BiomeConditions biome)
    {
        if (CurrentGame == null)
            throw new InvalidOperationException($"{nameof(PatchManager)} doesn't have {nameof(CurrentGame)} set");

        GD.Print("Number of chunks in this patch = ", biome.Chunks.Count);

        foreach (var entry in biome.Chunks)
        {
            // Don't spawn Easter eggs if the player has chosen not to
            if (entry.Value.EasterEgg && !CurrentGame.GameWorld.WorldSettings.EasterEggs)
                continue;

            // Difficulty only scales the spawn rate for chunks containing compounds
            var density = entry.Value.Density * Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR;
            if (entry.Value.Compounds?.Count > 0 || entry.Value.VentAmount > 0)
                density *= CurrentGame.GameWorld.WorldSettings.CompoundDensity;

            HandleSpawnHelper(chunkSpawners, entry.Value.Name, density,
                () => new CreatedSpawner(entry.Value.Name, Spawners.MakeChunkSpawner(entry.Value),
                    Constants.MICROBE_SPAWN_RADIUS));
        }
    }

    private void HandleCloudSpawns(BiomeConditions biome)
    {
        if (CurrentGame == null)
            throw new InvalidOperationException($"{nameof(PatchManager)} doesn't have {nameof(CurrentGame)} set");

        GD.Print("Number of clouds in this patch = ", biome.Compounds.Count);

        foreach (var entry in biome.Compounds)
        {
            // Density value in difficulty settings scales overall compound amount quadratically
            var density = entry.Value.Density * CurrentGame.GameWorld.WorldSettings.CompoundDensity *
                Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR;
            var amount = entry.Value.Amount * CurrentGame.GameWorld.WorldSettings.CompoundDensity *
                Constants.CLOUD_SPAWN_AMOUNT_SCALE_FACTOR;

            HandleSpawnHelper(cloudSpawners, entry.Key.InternalName, density,
                () => new CreatedSpawner(entry.Key.InternalName,
                    Spawners.MakeCompoundSpawner(entry.Key, compoundCloudSystem, amount),
                    Constants.CLOUD_SPAWN_RADIUS));
        }
    }

    private void HandleCellSpawns(Patch patch)
    {
        if (CurrentGame == null)
            throw new InvalidOperationException($"{nameof(PatchManager)} doesn't have {nameof(CurrentGame)} set");

        GD.Print("Number of species in this patch = ", patch.SpeciesInPatch.Count);

        foreach (var entry in patch.SpeciesInPatch.OrderByDescending(entry => entry.Value))
        {
            var species = entry.Key;
            var population = entry.Value;

            if (population <= 0)
            {
                GD.Print(entry.Key.FormattedName, " population <= 0. Skipping Cell Spawn in patch.");
                continue;
            }

            if (species.Obsolete)
            {
                GD.PrintErr("Obsolete species is in a patch");
                continue;
            }

            var density = Mathf.Max(
                Mathf.Log(population / Constants.MICROBE_SPAWN_DENSITY_POPULATION_MULTIPLIER) *
                Constants.MICROBE_SPAWN_DENSITY_SCALE_FACTOR, 0.0f);

            var name = species.ID.ToString(CultureInfo.InvariantCulture);

            HandleSpawnHelper(microbeSpawners, name, density,
                () => new CreatedSpawner(name, Spawners.MakeMicrobeSpawner(species,
                    compoundCloudSystem, CurrentGame), Constants.MICROBE_SPAWN_RADIUS),
                new MicrobeSpawnerComparer());
        }
    }

    private void HandleSpawnHelper(List<CreatedSpawner> existingSpawners, string itemName,
        float density, Func<CreatedSpawner> createNew, IEqualityComparer<CreatedSpawner>? customEqualityComparer = null)
    {
        if (density <= 0)
        {
            GD.Print(itemName, " spawn density is 0. It won't spawn");
            return;
        }

        var existing = existingSpawners.Find(s => s.Name == itemName);

        CreatedSpawner? newSpawner = null;

        if (existing != null && customEqualityComparer != null)
        {
            // Need additional checking to make sure the existing is good
            newSpawner = createNew();

            if (!customEqualityComparer.Equals(existing, newSpawner))
            {
                GD.Print("Existing spawner of ", existing.Name, " didn't match equality check, creating new instead");
                existing = null;
            }
        }

        if (existing != null)
        {
            if (existing.Marked)
            {
                GD.PrintErr($"Multiple spawn items want to use the same spawner {existing.Name} ({existing})");
            }

            existing.Marked = true;

            if (existing.Spawner.Density != density)
            {
                GD.Print("Changing spawn density of ", existing.Name, " from ",
                    existing.Spawner.Density, " to ", density);
                existing.Spawner.Density = density;
            }
        }
        else
        {
            // New spawner needed
            GD.Print("Registering new spawner: Name: ", itemName, " density: ", density);

            newSpawner ??= createNew();

            // Register this here to not cause problems if the new spawner was created just to compare against the old
            // object (and the code execution never got here)
            spawnSystem.AddSpawnType(newSpawner.Spawner, density, newSpawner.WantedRadius);

            existingSpawners.Add(newSpawner);
        }
    }

    private void UpdateLight(Biome biome)
    {
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
    ///   Removes unmarked spawners from List.
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
        public readonly Spawner Spawner;
        public readonly string Name;

        /// <summary>
        ///   The wanted radius that is passed to <see cref="SpawnSystem.AddSpawnType"/> when this is initially
        ///   registered
        /// </summary>
        public readonly int WantedRadius;

        public bool Marked = true;

        public CreatedSpawner(string name, Spawner spawner, int wantedRadius)
        {
            Name = name;
            Spawner = spawner;
            WantedRadius = wantedRadius;
        }
    }

    private class MicrobeSpawnerComparer : EqualityComparer<CreatedSpawner>
    {
        public override bool Equals(CreatedSpawner x, CreatedSpawner y)
        {
            if (ReferenceEquals(x, y) || ReferenceEquals(x.Spawner, y.Spawner))
                return true;

            if (x.Spawner is MicrobeSpawner microbeSpawner1 && y.Spawner is MicrobeSpawner microbeSpawner2)
            {
                return Equals(microbeSpawner1.Species, microbeSpawner2.Species);
            }

            return false;
        }

        public override int GetHashCode(CreatedSpawner obj)
        {
            return (obj.Name.GetHashCode() * 439) ^ (obj.Spawner.GetHashCode() * 443);
        }
    }
}
