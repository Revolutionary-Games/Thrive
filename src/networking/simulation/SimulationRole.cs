/// <summary>
///   What a world simulation is responsible for in a network session
/// </summary>
public enum SimulationRole
{
    /// <summary>
    ///   Not networked. Single-player.
    /// </summary>
    Standalone = 0,

    /// <summary>
    ///   Runs the real simulation and decides the outcome of everything. The host or a dedicated server.
    /// </summary>
    Authoritative = 1,

    /// <summary>
    ///   A client.
    /// </summary>
    Replica = 2,
}
