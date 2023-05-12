/// <summary>
///   Read only context access to converters
/// </summary>
public interface ISaveContext
{
    public SimulationParameters Simulation { get; }

    public GameWorld? World { get; }
}
