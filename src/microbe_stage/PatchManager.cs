using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

/// <summary>
///   Manages applying patch data and setting up spawns
/// </summary>
public class PatchManager
{
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
        CompoundCloudSystem compoundCloudSystem, TimedLifeSystem timedLife, DirectionalLight worldLight)
    {
        this.spawnSystem = spawnSystem;
        this.processSystem = processSystem;
        this.compoundCloudSystem = compoundCloudSystem;
        this.timedLife = timedLife;
        this.worldLight = worldLight;
    }

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

            // Clear SpawnBags
            spawnSystem.ClearSpawnSystem();

            // And also all timed entities
            timedLife.DespawnAll();

            // Clear compounds
            compoundCloudSystem.EmptyAllClouds();
        }

        previousPatch = currentPatch;

        GD.Print("Applying patch (", TranslationServer.Translate(currentPatch.Name), ") settings");

        // Update environment for process system
        processSystem.SetBiome(currentPatch.Biome);

        HandleCloudSpawns(currentPatch.Biome);
        HandleChunkSpawns(currentPatch.Biome);
        HandleCellSpawns(currentPatch);

        SetFullSpawnBags();

        // Change the lighting
        UpdateLight(currentPatch.BiomeTemplate);
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
                // Cheaty divide because there are too many chunks
                int numOfItems = Math.Min((int)chunkDensity, 100);
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

            // if density = 0, then do not add to biomeCompounds
            if (compoundDensity > 0 && compoundAmount > 0)
            {
                int numOfItems = (int)compoundDensity;
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

            float density = 400000f / (Constants.STARTING_SPAWN_DENSITY -
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
        foreach (Compound compound in compoundCloudCounts.Keys)
        {
            for (int i = 0; i < compoundCloudCounts[compound]; i++)
            {
                spawnSystem.AddSpawnItem(new CloudItem(compound, compoundAmounts[compound]));
            }
        }

        foreach (ChunkConfiguration chunk in chunkCounts.Keys)
        {
            foreach (var mesh in chunk.Meshes)
            {
                if (mesh.LoadedScene == null)
                    throw new ArgumentException("configured chunk spawner has a mesh that has no scene loaded");
            }

            for (int i = 0; i < chunkCounts[chunk]; i++)
            {
                spawnSystem.AddSpawnItem(new ChunkItem(chunk));
            }
        }

        foreach (Species key in speciesCounts.Keys)
        {
            if (!(key is MicrobeSpecies))
                continue;

            MicrobeSpecies species = (MicrobeSpecies)key;

            MicrobeItem microbeItem = new MicrobeItem(species);
            microbeItem.IsWanderer = false;
            spawnSystem.AddSpawnItem(microbeItem);

            MicrobeItem wanderMicrobeItem = new MicrobeItem(species);
            wanderMicrobeItem.IsWanderer = true;
            spawnSystem.AddMicrobeItem(wanderMicrobeItem);
        }

        spawnSystem.SetMicrobeBagSize();
    }
}
