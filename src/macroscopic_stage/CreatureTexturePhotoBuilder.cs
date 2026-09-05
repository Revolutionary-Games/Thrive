using Godot;

/// <summary>
///   Photographs an unwrapped creature model to create a texture for it.
/// </summary>
public partial class CreatureTexturePhotoBuilder : Node3D
{
#pragma warning disable CA2213
    [Export]
    private MeshInstance3D meshInstance3D = null!;
#pragma warning restore CA2213

    private StringName projectionMatricesName = new("projectionMatrices");
    private StringName projectionMatrixSizeName = new("projectionMatrixCount");
    private StringName mainTextureName = new("mainTexture");
    private StringName projectedTextureName = new("projected");

    public void SetMesh(Mesh mesh)
    {
        meshInstance3D.Mesh = mesh;
    }

    public void SetProjectionMatrices(Godot.Collections.Array matrices)
    {
        ((ShaderMaterial)meshInstance3D.MaterialOverride).SetShaderParameter(projectionMatricesName, matrices);
        ((ShaderMaterial)meshInstance3D.MaterialOverride).SetShaderParameter(projectionMatrixSizeName, matrices.Count);
    }

    public void SetTextures(Texture2D? mainTexture, Texture2D? projectedTexture)
    {
        ((ShaderMaterial)meshInstance3D.MaterialOverride).SetShaderParameter(mainTextureName,
            mainTexture ?? default(Variant));

        ((ShaderMaterial)meshInstance3D.MaterialOverride).SetShaderParameter(projectedTextureName,
            projectedTexture ?? default(Variant));
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
