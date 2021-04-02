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
    private GameProperties currentGame;

    private Patch previousPatch;

    public PatchManager(SpawnSystem spawnSystem, ProcessSystem processSystem,
        CompoundCloudSystem compoundCloudSystem, TimedLifeSystem timedLife, DirectionalLight worldLight,
        GameProperties currentGame)
    {
        this.spawnSystem = spawnSystem;
        this.processSystem = processSystem;
        this.compoundCloudSystem = compoundCloudSystem;
        this.timedLife = timedLife;
        this.worldLight = worldLight;
        this.currentGame = currentGame;
    }

    /// <summary>
    ///   Applies all patch related settings that are needed to be
    ///   set. Like different spawners, despawning old entities if the
    ///   patch changed etc.
    /// </summary>
    public void ApplyChangedPatchSettingsIfNeeded(Patch currentPatch, bool despawnAllowed,
        Vector3 playerPosition, bool isNewGame)
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

        HandleCloudSpawns(currentPatch.Biome, playerPosition);
        HandleChunkSpawns(currentPatch.Biome);
        HandleCellSpawns(currentPatch);

        spawnSystem.FillSpawnItemBag();

        //If we just loaded a save file, don't spawn new stuff.
        if(despawnAllowed && !isNewGame)
        {
            spawnSystem.NewPatchSpawn(playerPosition);
        }

        // Change the lighting
        UpdateLight(currentPatch.BiomeTemplate);
    }

    private void HandleChunkSpawns(BiomeConditions biome)
    {
        GD.Print("Number of chunks in this patch = ", biome.Chunks.Count);

        spawnSystem.ClearChunkSpawner();

        foreach (var chunk in biome.Chunks.Keys)
        {
            float chunkDensity = biome.Chunks[chunk].Density;

            if(chunkDensity > 0)
            {
                int numOfItems = (int)(Constants.SPAWN_DENSITY_MULTIPLIER * chunkDensity);
                GD.Print(biome.Chunks[chunk].Name + " has " + numOfItems + " items per bag.");
                spawnSystem.AddBiomeChunk(biome.Chunks[chunk], numOfItems);
            }
        }
    }

    private void HandleCloudSpawns(BiomeConditions biome, Vector3 playerPosition)
    {
        GD.Print("Number of clouds in this patch = ", biome.Compounds.Count);

        spawnSystem.ClearCloudSpawner();

        foreach (var compound in biome.Compounds.Keys)
        {
            float compoundDensity = biome.Compounds[compound].Density;
            float compoundAmount = biome.Compounds[compound].Amount;

            // if density = 0, then do not add to biomeCompounds
            if (compoundDensity > 0 && compoundAmount > 0)
            {
                int numOfItems = (int)(Constants.SPAWN_DENSITY_MULTIPLIER * compoundDensity);
                GD.Print(compound.Name + " has " + numOfItems + " items per bag.");
                spawnSystem.AddBiomeCompound(compound, numOfItems, compoundAmount);
            }
        }
    }

    private void HandleCellSpawns(Patch patch)
    {
        GD.Print("Number of species in this patch = ", patch.SpeciesInPatch.Count);

        spawnSystem.ClearMicrobeSpawner();

        foreach (var entry in patch.SpeciesInPatch)
        {
            var species = entry.Key;

            if (species.Population <= 0)
            {
                GD.Print(entry.Key.FormattedName, " population <= 0. Skipping Cell Spawn in patch.");
                continue;
            }

            var density = 1.0f / (Constants.STARTING_SPAWN_DENSITY -
                Math.Min(Constants.MAX_SPAWN_DENSITY,
                    species.Population * 5));

            var name = species.ID.ToString(CultureInfo.InvariantCulture);

            int numOfItems = (int)(Constants.SPAWN_DENSITY_MULTIPLIER * density);
            GD.Print(name + " has " + numOfItems + " items per bag.");
            spawnSystem.AddPatchSpecies(species, numOfItems);

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
}
