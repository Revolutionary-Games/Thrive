using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class SimulationParameters
{
    private static readonly SimulationParameters INSTANCE = new SimulationParameters();

    private Dictionary<string, MembraneType> membranes;
    private Dictionary<string, Background> backgrounds;
    private Dictionary<string, Biome> biomes;
    private Dictionary<string, BioProcess> bioProcesses;

    static SimulationParameters()
    {
    }

    /// <summary>
    ///   Loads the simulation configuration parameters from JSON files
    /// </summary>
    private SimulationParameters()
    {
        membranes = LoadRegistry<MembraneType>(
            "res://scripts/simulation_parameters/microbe_stage/membranes.json");
        backgrounds = LoadRegistry<Background>(
            "res://scripts/simulation_parameters/microbe_stage/backgrounds.json");
        biomes = LoadRegistry<Biome>(
            "res://scripts/simulation_parameters/microbe_stage/biomes.json");
        bioProcesses = LoadRegistry<BioProcess>(
            "res://scripts/simulation_parameters/microbe_stage/bio_processes.json");

        GD.Print("SimulationParameters loading ended");
        CheckForInvalidValues();
        GD.Print("SimulationParameters are good");
    }

    public static SimulationParameters Instance
    {
        get
        {
            return INSTANCE;
        }
    }

    public MembraneType GetMembrane(string name)
    {
        return membranes[name];
    }

    public Background GetBackground(string name)
    {
        return backgrounds[name];
    }

    public Biome GetBiome(string name)
    {
        return biomes[name];
    }

    private Dictionary<string, T> LoadRegistry<T>(string path)
    {
        using (var file = new File())
        {
            file.Open(path, File.ModeFlags.Read);
            var result = JsonConvert.DeserializeObject<Dictionary<string, T>>(
                file.GetAsText());

            // file.Close();

            GD.Print($"Loaded registry for {typeof(T)} with {result.Count} items");
            return result;
        }
    }

    private void CheckForInvalidValues()
    {
        CheckRegistryType(membranes);
        CheckRegistryType(backgrounds);
        CheckRegistryType(biomes);
        CheckRegistryType(bioProcesses);
    }

    private void CheckRegistryType<T>(Dictionary<string, T> registry)
        where T : IRegistryType
    {
        foreach (var entry in registry)
        {
            entry.Value.Check(entry.Key);
        }
    }
}
