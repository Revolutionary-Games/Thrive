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
    public SimpleWorldResource(PackedScene worldRepresentation, string internalName)
    {
        WorldRepresentation = worldRepresentation;
        InternalName = internalName;
    }

    public PackedScene WorldRepresentation { get; }
    public string ReadableName => TranslationServer.Translate(InternalName);
    public string InternalName { get; }
}
