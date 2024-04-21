namespace AutoEvo;

using System;

/// <summary>
///   Contains the parameters for an auto-evo run
/// </summary>
public class RunParameters
{
    public readonly GameWorld World;

    public RunParameters(GameWorld world)
    {
        World = world ?? throw new ArgumentException("GameWorld is null");
    }
}
