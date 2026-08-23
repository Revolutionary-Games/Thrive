using Godot;
using System;

/// <summary>
///   Photographs an unwrapped creature model to create a texture for it.
/// </summary>
public partial class CreatureTexturePhotoBuilder : Node3D
{
#pragma warning disable CA2213
    [Export]
    private MeshInstance3D meshInstance3D = null!;
#pragma warning restore CA2213

    private StringName projectionMatrices = new("matrices");

    public void SetMesh(Mesh mesh)
    {
        meshInstance3D.Mesh = mesh;
    }

    public void SetProjectionMatrices(Godot.Collections.Array matrices)
    {
        ((ShaderMaterial)meshInstance3D.MaterialOverride).SetShaderParameter(projectionMatrices, matrices);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            projectionMatrices.Dispose();
        }
    }
}
