using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Visuals of engulfing something and encasing it in a "membrane" bubble
/// </summary>
public class Endosome : Spatial, IEntity
{
    [JsonProperty]
    private Color tint = Colors.White;

    [JsonProperty]
    private int renderPriority;

    [JsonIgnore]
    public MeshInstance? Mesh { get; private set; }

    [JsonIgnore]
    public Color Tint
    {
        get => tint;
        set
        {
            // EngulfingSystem always updates the property values so we skip applying this to the shader if the value
            // didn't change
            if (tint == value)
                return;

            tint = value;
            ApplyTint();
        }
    }

    [JsonIgnore]
    public int RenderPriority
    {
        get => renderPriority;
        set
        {
            renderPriority = value;
            ApplyRenderPriority();
        }
    }

    [JsonIgnore]
    public Spatial EntityNode => this;

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    public override void _Ready()
    {
        Mesh = GetNode<MeshInstance>("EngulfedObjectHolder") ?? throw new Exception("mesh node not found");

        var material = Mesh!.MaterialOverride as ShaderMaterial;

        if (material == null)
            GD.PrintErr("Material is not found from the EngulfedObjectHolder mesh for Endosome");

        // This has to be done here because setting this in Godot editor truncates
        // the number to only 3 decimal places.
        material?.SetShaderParam("jiggleAmount", 0.0001f);

        ApplyTint();
        ApplyRenderPriority();
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    private void ApplyTint()
    {
        var material = Mesh?.MaterialOverride as ShaderMaterial;
        material?.SetShaderParam("tint", tint);
    }

    private void ApplyRenderPriority()
    {
        if (Mesh == null)
            return;

        var material = Mesh.MaterialOverride;
        material.RenderPriority = renderPriority;
    }
}
