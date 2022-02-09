/// <summary>
///   Object providing access for converters as well as loading finalizing for the current game context
/// </summary>
public class SaveContext : ISaveContext
{
    public SaveContext(SimulationParameters simulation)
    {
        Simulation = simulation;
    }

    public SaveContext() : this(SimulationParameters.Instance)
    {
    }

    public SimulationParameters Simulation { get; }

    public GameWorld? World { get; set; }
}
