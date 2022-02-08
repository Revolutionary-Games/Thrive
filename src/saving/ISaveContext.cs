/// <summary>
///   Read only context access to converters
/// </summary>
public interface ISaveContext
{
    SimulationParameters Simulation { get; }

    GameWorld? World { get; }
}
