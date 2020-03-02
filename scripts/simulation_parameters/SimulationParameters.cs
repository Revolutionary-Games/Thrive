using System;

public class SimulationParameters
{
    private static readonly SimulationParameters INSTANCE = new SimulationParameters();

    static SimulationParameters()
    {
    }

    /// <summary>
    ///   Loads the simulation configurations
    /// </summary>
    private SimulationParameters()
    {
    }

    public static SimulationParameters Instance
    {
        get
        {
            return INSTANCE;
        }
    }
}
