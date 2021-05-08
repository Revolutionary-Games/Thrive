using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

/// <summary>
///   Manages applying patch data and setting up spawns
/// </summary>
public class PatchManager
{
    private int spawnBagSize;

    private SpawnSystem spawnSystem;
    private ProcessSystem processSystem;
    private CompoundCloudSystem compoundCloudSystem;
    private TimedLifeSystem timedLife;
    private DirectionalLight worldLight;

    private Patch previousPatch;

    // Counts of each Species/Chunk/Compound
    private Dictionary<Species, int> speciesCounts = new Dictionary<Species, int>();
    private Dictionary<ChunkConfiguration, int> chunkCounts = new Dictionary<ChunkConfiguration, int>();
    private Dictionary<Compound, int> compoundCloudCounts = new Dictionary<Compound, int>();

    // Cloud Compound Amounts
    private Dictionary<Compound, float> compoundAmounts = new Dictionary<Compound, float>();

    public PatchManager(SpawnSystem spawnSystem, ProcessSystem processSystem,
        CompoundCloudSystem compoundCloudSystem, TimedLifeSystem timedLife,
        DirectionalLight worldLight, GameProperties currentGame)
    {
        this.spawnSystem = spawnSystem;
        this.processSystem = processSystem;
        this.compoundCloudSystem = compoundCloudSystem;
        this.timedLife = timedLife;
        this.worldLight = worldLight;

        CloudSpawner = new CompoundCloudSpawner(compoundCloudSystem);
        ChunkSpawner = new ChunkSpawner(compoundCloudSystem);
        MicrobeSpawner = new MicrobeSpawner(compoundCloudSystem, currentGame);
    }

    public CompoundCloudSpawner CloudSpawner { get; private set; }
    public ChunkSpawner ChunkSpawner { get; private set; }
    public MicrobeSpawner MicrobeSpawner { get; private set; }

    /// <summary>
    ///   Applies all patch related settings that are needed to be
    ///   set. Like different spawners, despawning old entities if the
    ///   patch changed etc.
    /// </summary>
    public void ApplyChangedPatchSettingsIfNeeded(Patch currentPatch, bool despawnAllowed)
    {
        if (previousPatch != currentPatch && despawnAllowed)
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
        }

        previousPatch = currentPatch;

        GD.Print("Applying patch (", TranslationServer.Translate(currentPatch.Name), ") settings");

        // Update environment for process system
        processSystem.SetBiome(currentPatch.Biome);

        // Apply spawn system settings
        HandleCloudSpawns(currentPatch.Biome);
        HandleChunkSpawns(currentPatch.Biome);
        HandleCellSpawns(currentPatch);

        SetFullSpawnBags();
        SetSpawnGridSize(currentPatch.Biome);

        // Change the lighting
        UpdateLight(currentPatch.BiomeTemplate);
    }

    private void SetSpawnGridSize(BiomeConditions biome)
    {
        // SpawnChunkiness of 0 means one item per spawn event,
        // SpawnChunkiness of 1 means one spawnBagSize per spawn event,
        int spawnEventCount = (int)(1 + (biome.SpawnChunkiness * (spawnBagSize - 1)));

        // Assuming constant SpawnRateMultiplier, then SpawnChunkiness will not change overall density
        // Increase in SpawnRateMultiplier lowers the gridSize, which increases density
        // SpawnRateMultiplier of 2.0 will double the spawn density.
        int spawnGridSize = (int)(Constants.SPAWN_GRID_SIZE * Math.Sqrt(spawnEventCount)
            / Math.Sqrt(biome.SpawnRateMultiplier));

        spawnSystem.SetSpawnData(spawnEventCount, spawnGridSize, biome.SpawnRateMultiplier);
    }

    private void HandleChunkSpawns(BiomeConditions biome)
    {
        GD.Print("Number of chunks in this patch = ", biome.Chunks.Count);

        chunkCounts.Clear();

        foreach (var chunk in biome.Chunks.Keys)
        {
            float chunkDensity = biome.Chunks[chunk].Density;

            if (chunkDensity > 0)
            {
                int numOfItems = Math.Min(
                    (int)(chunkDensity * Constants.SPAWN_DENSITY_MULTIPLIER), 100);
                GD.Print(biome.Chunks[chunk].Name + " has " + numOfItems + " items per bag.");
                chunkCounts.Add(biome.Chunks[chunk], numOfItems);
            }
        }
    }

    private void HandleCloudSpawns(BiomeConditions biome)
    {
        GD.Print("Number of clouds in this patch = ", biome.Compounds.Count);

        compoundCloudCounts.Clear();
        compoundAmounts.Clear();

        foreach (var compound in biome.Compounds.Keys)
        {
            float compoundDensity = biome.Compounds[compound].Density;
            float compoundAmount = biome.Compounds[compound].Amount;

            if (compoundDensity > 0 && compoundAmount > 0)
            {
                int numOfItems = Math.Min(
                    (int)(compoundDensity * Constants.SPAWN_DENSITY_MULTIPLIER), 100);
                GD.Print(compound.Name + " has " + numOfItems + " items per bag.");
                compoundCloudCounts.Add(compound, numOfItems);
                compoundAmounts.Add(compound, compoundAmount);
            }
        }
    }

    private void HandleCellSpawns(Patch patch)
    {
        GD.Print("Number of species in this patch = ", patch.SpeciesInPatch.Count);

        speciesCounts.Clear();

        foreach (var entry in patch.SpeciesInPatch)
        {
            var species = entry.Key;

            if (species.Population <= 0)
            {
                GD.Print(entry.Key.FormattedName, " population <= 0. Skipping Cell Spawn in patch.");
                continue;
            }

            float density = Constants.SPAWN_DENSITY_MULTIPLIER / (Constants.STARTING_SPAWN_DENSITY -
                Math.Min(Constants.MAX_SPAWN_DENSITY,
                    species.Population * 3));

            var name = species.FormattedName.ToString(CultureInfo.InvariantCulture);

            int numOfItems = (int)density;
            GD.Print(name + " has " + numOfItems + " items per bag.");
            speciesCounts.Add(species, numOfItems);
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

    private void SetFullSpawnBags()
    {
        spawnBagSize = 0;

        foreach (Compound compound in compoundCloudCounts.Keys)
        {
            spawnBagSize += compoundCloudCounts[compound];
            for (int i = 0; i < compoundCloudCounts[compound]; i++)
            {
                spawnSystem.AddSpawnItem(new CloudItem(compound, compoundAmounts[compound], CloudSpawner));
            }
        }

        foreach (ChunkConfiguration chunk in chunkCounts.Keys)
        {
            spawnBagSize += chunkCounts[chunk];
            foreach (var mesh in chunk.Meshes)
            {
                if (mesh.LoadedScene == null)
                    throw new ArgumentException("configured chunk spawner has a mesh that has no scene loaded");
            }

            for (int i = 0; i < chunkCounts[chunk]; i++)
            {
                spawnSystem.AddSpawnItem(new ChunkItem(chunk, ChunkSpawner));
            }
        }

        foreach (Species key in speciesCounts.Keys)
        {
            if (!(key is MicrobeSpecies))
                continue;

            MicrobeSpecies species = (MicrobeSpecies)key;
            spawnBagSize += speciesCounts[key];

            for (int i = 0; i < speciesCounts[key]; i++)
            {
                MicrobeItem microbeItem = new MicrobeItem(species, MicrobeSpawner);
                microbeItem.IsWanderer = false;
                spawnSystem.AddSpawnItem(microbeItem);

                MicrobeItem wanderMicrobeItem = new MicrobeItem(species, MicrobeSpawner);
                wanderMicrobeItem.IsWanderer = true;
                spawnSystem.AddMicrobeItem(wanderMicrobeItem);
            }
        }

        spawnSystem.SetMicrobeBagSize();
    }
}
