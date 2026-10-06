using Godot;
using Godot.Collections;

/// <summary>
///   Photographs an unwrapped creature model to create a texture for it.
/// </summary>
public partial class CreatureTexturePhotoBuilder : Node3D
{
#pragma warning disable CA2213
    [Export]
    private MeshInstance3D meshInstance3D = null!;

    private ShaderMaterial material = null!;
#pragma warning restore CA2213

    private StringName projectionMatricesName = new("projectionMatrices");
    private StringName projectionMatrixSizeName = new("projectionMatrixCount");
    private StringName mainTextureName = new("mainTexture");
    private StringName projectedTextureName = new("projected");

    public void SetMesh(Mesh mesh)
    {
        meshInstance3D.Mesh = mesh;
        material = (ShaderMaterial)meshInstance3D.MaterialOverride;
    }

    public void SetProjectionMatrices(Array matrices)
    {
        material.SetShaderParameter(projectionMatricesName, matrices);
        material.SetShaderParameter(projectionMatrixSizeName, matrices.Count);
    }

    public void SetTextures(Texture2D? mainTexture, Texture2D? projectedTexture)
    {
        material.SetShaderParameter(mainTextureName, mainTexture ?? default(Variant));
        material.SetShaderParameter(projectedTextureName, projectedTexture ?? default(Variant));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            projectionMatricesName.Dispose();
            projectionMatrixSizeName.Dispose();
            mainTextureName.Dispose();
            projectedTextureName.Dispose();
        }
    }
}
