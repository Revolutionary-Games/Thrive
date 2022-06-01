using Godot;
using Newtonsoft.Json;

/// <summary>
///   This does nothing (for now) and only exist so saving could work.
/// </summary>
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/microbe_stage/Endosome.tscn", UsesEarlyResolve = false)]
public class Endosome : Spatial
{
    [JsonIgnore]
    public MeshInstance? Mesh { get; private set; }

    public override void _Ready()
    {
        Mesh = GetNode<MeshInstance>("Vacuole");
    }

    public void UpdateTint(Color colour)
    {
        var material = Mesh?.MaterialOverride as ShaderMaterial;
        material?.SetShaderParam("tint", colour);
    }
}
