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

    private ShaderMaterial materialToEdit;

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
        materialToEdit = (ShaderMaterial)MaterialOverride;
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
        ApplyWiggly();
        ApplyHealth();
        ApplyTint();
        ApplyTextures();
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
}
