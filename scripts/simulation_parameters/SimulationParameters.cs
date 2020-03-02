using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class SimulationParameters
{
    private static readonly SimulationParameters INSTANCE = new SimulationParameters();

    private Dictionary<string, MembraneType> membranes;

    static SimulationParameters()
    {
    }

    /// <summary>
    ///   Loads the simulation configuration parameters from JSON files
    /// </summary>
    private SimulationParameters()
    {
        using (var file = new File())
        {
            file.Open("res://scripts/simulation_parameters/microbe_stage/membranes.json",
                File.ModeFlags.Read);
            membranes = JsonConvert.DeserializeObject<Dictionary<string, MembraneType>>(
                file.GetAsText());

            // file.Close();
        }

        GD.Print("SimulationParameters loading ended");
        GD.Print("Number of loaded membrane types: ", membranes.Count);
    }

    public static SimulationParameters Instance
    {
        get
        {
            return INSTANCE;
        }
    }
}
