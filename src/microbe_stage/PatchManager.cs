﻿using System;
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
    private CompoundCloudSystem compoundCloudSystem;
    private TimedLifeSystem timedLife;
    private GameWorld world;

    private Patch previousPatch;

    // Currently active spawns
    private List<CreatedSpawner> chunkSpawners = new List<CreatedSpawner>();
    private List<CreatedSpawner> cloudSpawners = new List<CreatedSpawner>();
    private List<CreatedSpawner> microbeSpawners = new List<CreatedSpawner>();

    public PatchManager(SpawnSystem spawnSystem, ProcessSystem processSystem,
        CompoundCloudSystem compoundCloudSystem, TimedLifeSystem timedLife, GameWorld world)
    {
        this.spawnSystem = spawnSystem;
        this.processSystem = processSystem;
        this.compoundCloudSystem = compoundCloudSystem;
        this.timedLife = timedLife;
        this.world = world;
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
            spawnSystem.DespawnAll();

            // And also all timed entities
            timedLife.DespawnAll();

            // Clear compounds
            compoundCloudSystem.EmptyAllClouds();
        }

        previousPatch = currentPatch;

        GD.Print("Applying patch (", currentPatch.Name, ") settings");

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
        UpdateLight(currentPatch.Biome);

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
            HandleSpawnHelper(chunkSpawners, entry.Value.Name, entry.Value.Density,
                () =>
                    {
                        var spawner = new CreatedSpawner(entry.Value.Name);
                        spawner.Spawner = Spawners.MakeChunkSpawner(entry.Value,
                            compoundCloudSystem);

                        spawnSystem.AddSpawnType(spawner.Spawner, (int)entry.Value.Density,
                            Constants.MICROBE_SPAWN_RADIUS);
                        return spawner;
                    });
        }
    }

    private void HandleCloudSpawns(Biome biome)
    {
        GD.Print("Number of clouds in this patch = ", biome.Compounds.Count);

        foreach (var entry in biome.Compounds)
        {
            HandleSpawnHelper(chunkSpawners, entry.Key, entry.Value.Density,
                () =>
                    {
                        var spawner = new CreatedSpawner(entry.Key);
                        spawner.Spawner = Spawners.MakeCompoundSpawner(
                            SimulationParameters.Instance.GetCompound(entry.Key),
                            compoundCloudSystem, entry.Value.Amount);

                        spawnSystem.AddSpawnType(spawner.Spawner, entry.Value.Density,
                            Constants.CLOUD_SPAWN_RADIUS);
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

            if (species.Population <= 0)
                continue;

            var density = 1.0f / (Constants.STARTING_SPAWN_DENSITY -
                                Math.Min(Constants.MAX_SPAWN_DENSITY,
                                    species.Population * 5));

            var name = species.ID.ToString();

            HandleSpawnHelper(chunkSpawners, name, density,
                () =>
                    {
                        var spawner = new CreatedSpawner(name);
                        spawner.Spawner = Spawners.MakeMicrobeSpawner(species,
                            compoundCloudSystem, world);

                        spawnSystem.AddSpawnType(spawner.Spawner, density,
                            Constants.MICROBE_SPAWN_RADIUS);
                        return spawner;
                    });
        }
    }

    private void HandleSpawnHelper(List<CreatedSpawner> existingSpawners, string itemName,
        float density, Func<CreatedSpawner> createNew)
    {
        if (density <= 0)
        {
            GD.Print(itemName, " spawn density is 0. It won't spawn");
            return;
        }

        var existing = existingSpawners.Find((s) => s.Name == itemName);

        if (existing != null)
        {
            existing.Marked = true;

            existing.Spawner.SetFrequencyFromDensity(density);
        }
        else
        {
            // New spawner needed
            GD.Print("Registering new spawner: Name: ", itemName, " density: ", density);

            chunkSpawners.Add(createNew());
        }
    }

    private void UpdateLight(Biome biome)
    {
        // // Sunlight
        // InWorld.SetLightProperties(biome.sunlightColor, biome.sunlightIntensity,
        //     biome.sunlightDirection, biome.sunlightSourceRadius);

        // // Skybox with indirect light
        // ThriveGame::get()->setSkybox(biome.skybox, biome.skyboxLightIntensity);

        // // Eye adaptation settings
        // LOG_INFO("Setting eye adaptation: min: " +
        //          std::to_string(biome.minEyeAdaptation) +
        //          " max: " + std::to_string(biome.maxEyeAdaptation));
        // InWorld.SetAutoExposure(biome.minEyeAdaptation, biome.maxEyeAdaptation);
    }

    private void UpdateBiomeStatsForGUI(Biome biome)
    {
        // vars->Add(std::make_shared<NamedVariableList>("oxygenPercent",
        //     new Leviathan::IntBlock(
        //         cellWorld.GetProcessSystem().getDissolved(oxyId) * 100)));

        // vars->Add(std::make_shared<NamedVariableList>("co2Percent",
        //     new Leviathan::IntBlock(
        //         cellWorld.GetProcessSystem().getDissolved(c02Id) * 100)));

        // vars->Add(std::make_shared<NamedVariableList>("n2Percent",
        //     new Leviathan::IntBlock(
        //         cellWorld.GetProcessSystem().getDissolved(n2Id) * 100)));

        // vars->Add(std::make_shared<NamedVariableList>("sunlightPercent",
        //     new Leviathan::IntBlock(
        //         cellWorld.GetProcessSystem().getDissolved(lightId) * 100)));
    }

    private void UpdateCurrentPatchInfoForGUI(Patch patch)
    {
        // vars->Add(std::make_shared<NamedVariableList>(
        //     "patchName", new Leviathan::StringBlock(patch.getName())));
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

    private void ClearUnmarkedSingle(List<CreatedSpawner> spawners)
    {
        spawners.RemoveAll((item) => item.Marked == false);
    }

    private class CreatedSpawner
    {
        public ISpawner Spawner;
        public string Name;
        public bool Marked = true;

        public CreatedSpawner(string name)
        {
            Name = name;
        }
    }
}
