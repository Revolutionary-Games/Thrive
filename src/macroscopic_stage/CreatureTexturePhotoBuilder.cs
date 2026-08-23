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

    public void SetMesh(Mesh mesh)
    {
        meshInstance3D.Mesh = mesh;
    }
}
