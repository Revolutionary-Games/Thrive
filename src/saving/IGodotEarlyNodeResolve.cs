using Newtonsoft.Json;

/// <summary>
///   Types that implement this interface can have their Godot child nodes (from NodePaths) evaluated before entering
///   the scene tree. This is needed for scene loaded objects to properly have their child Nodes when deserializing
///   their data.
/// </summary>
public interface IGodotEarlyNodeResolve
{
    [JsonIgnore]
    public bool NodeReferencesResolved { get; }

    public void ResolveNodeReferences();
}
