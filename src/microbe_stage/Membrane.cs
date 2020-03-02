using System;
using Godot;

/// <summary>
///   Membrane for microbes
/// </summary>
public class Membrane : MeshInstance
{
    /// <summary>
    ///   When true the mesh needs to be regenerated
    /// </summary>
    private bool dirty = true;
    private ArrayMesh generatedMesh;

    [Export] public ShaderMaterial materialToEdit;

    private float healthFraction = 1.0f;
    private float wigglyNess = 1.0f;
    private Color tint = new Color(1, 1, 1, 1);

    /// <summary>
    ///   How healthy the cell is, mixes in a damaged texture. Range 0.0f - 1.0f
    /// </summary>
    public float HealthFraction
    {
        get
        {
            return healthFraction;
        }
        set
        {
            healthFraction = value.Clamp(0.0f, 1.0f);
            if (materialToEdit != null)
                ApplyHealth();
        }
    }

    /// <summary>
    ///   How much the membrane wiggles. Used values are 0 and 1
    /// </summary>
    public float WigglyNess
    {
        get
        {
            return wigglyNess;
        }
        set
        {
            wigglyNess = value;
            if (materialToEdit != null)
                ApplyWiggly();
        }
    }

    public Color Tint
    {
        get
        {
            return tint;
        }
        set
        {
            tint = value;
            if (materialToEdit != null)
                ApplyTint();
        }
    }

    public override void _Ready()
    {
        generatedMesh = new ArrayMesh();
        Mesh = generatedMesh;
        dirty = true;
    }

    public override void _Process(float delta)
    {
        if (!dirty)
            return;

        dirty = false;
        ApplyAllMaterialParameters();
    }

    private void ApplyAllMaterialParameters()
    {
        BuildMesh(); // Only here for testing purposes, should be moved to after adding organelles.
        ApplyWiggly();
        ApplyHealth();
        ApplyTint();
        //ApplyTextures();
    }

    private void ApplyWiggly()
    {
        materialToEdit.SetShaderParam("wigglyNess", WigglyNess);
    }

    private void ApplyHealth()
    {
        materialToEdit.SetShaderParam("healthFraction", HealthFraction);
    }

    private void ApplyTint()
    {
        materialToEdit.SetShaderParam("tint", Tint);
    }

    private void ApplyTextures()
    {
        materialToEdit.SetShaderParam("albedoTexture", WigglyNess);
        materialToEdit.SetShaderParam("damagedTexture", WigglyNess);
    }

    public void BuildMesh() {
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        var vectors = new Vector3[Constants.Instance.MEMBRANE_RESOLUTION + 2];
        var uvs = new Vector2[Constants.Instance.MEMBRANE_RESOLUTION + 2];

        vectors[0] = new Vector3(0.0f, 0.0f, 0.0f);
        uvs[0] = new Vector2(0.5f, 0.5f);

        for(int i = 1; i < Constants.Instance.MEMBRANE_RESOLUTION + 2; i++)
        {
            var t = i * 2 * Math.PI / Constants.Instance.MEMBRANE_RESOLUTION;
            var r = 50; // TODO: find the membrane border

            vectors[i] = new Vector3(
                (float)Math.Cos(t),
                0,
                (float)Math.Sin(t)
            ) * r;

            uvs[i] = new Vector2(
                (float)Math.Cos(t) / 2.0f + 0.5f,
                (float)Math.Sin(t) / 2.0f + 0.5f
            );
        }

        arrays[(int)Mesh.ArrayType.Vertex] = vectors;
        arrays[(int)Mesh.ArrayType.TexUv] = uvs;

        // TODO: Check if triangles + indices is better than triangle fan.
        var surfaceIndex = generatedMesh.GetSurfaceCount();
        generatedMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.TriangleFan, arrays);
        SetSurfaceMaterial(surfaceIndex, materialToEdit);
    }
}
