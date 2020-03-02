using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class SimulationParameters
{
    private static readonly SimulationParameters INSTANCE = new SimulationParameters();

    private Dictionary<string, MembraneType> membranes;
    private Dictionary<string, Background> backgrounds;

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

        GD.Print("SimulationParameters loading ended");
        CheckForInvalidValues();
        GD.Print("SimulationParameters are good");
    }

    public MembraneType GetMembrane(string name)
    {
        return membranes[name];
    }

    public Background GetBackground(string name)
    {
        return backgrounds[name];
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
        foreach (var entry in membranes)
        {
            entry.Value.Check(entry.Key);
        }

        foreach (var entry in backgrounds)
        {
            entry.Value.Check(entry.Key);
        }
    }

    public static SimulationParameters Instance
    {
        get
        {
            return INSTANCE;
        }
    }
}
