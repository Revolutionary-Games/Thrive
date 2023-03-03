using Godot;

/// <summary>
///   A simple defined world resource
/// </summary>
/// <remarks>
///   <para>
///     TODO: switch to loading these from JSON (should be in
///     simulation_parameters/multicellular_stage/world_resources.json)
///   </para>
/// </remarks>
public class SimpleWorldResource : IWorldResource
{
    public SimpleWorldResource(PackedScene worldRepresentation)
    {
        WorldRepresentation = worldRepresentation;
    }

    public PackedScene WorldRepresentation { get; }
}
