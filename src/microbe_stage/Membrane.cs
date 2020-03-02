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

    /// <summary>
    ///   How healthy the cell is, mixes in a damaged texture. Range 0.0f - 1.0f
    /// </summary>
    public float healthFraction
    {
        get
        {
            return _healthFraction;
        }
        set
        {
            _healthFraction = MathUtils.Clamp(value, 0.0f, 1.0f);
            if (materialToEdit != null)
                ApplyHealth();
        }
    }

    /// <summary>
    ///   How much the membrane wiggles. Used values are 0 and 1
    /// </summary>
    public float wigglyNess
    {
        get
        {
            return _wigglyNess;
        }
        set
        {
            _wigglyNess = value;
            if (materialToEdit != null)
                ApplyWiggly();
        }
    }

    public Color tint
    {
        get
        {
            return _tint;
        }
        set
        {
            _tint = value;
            if (materialToEdit != null)
                ApplyTint();
        }
    }

    private float _healthFraction = 1.0f;
    private float _wigglyNess = 1.0f;
    private Color _tint = new Color(1, 1, 1, 1);

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
        materialToEdit.SetShaderParam("wigglyNess", wigglyNess);
    }

    private void ApplyHealth()
    {
        materialToEdit.SetShaderParam("healthFraction", healthFraction);
    }

    private void ApplyTint()
    {
        materialToEdit.SetShaderParam("tint", tint);
    }

    private void ApplyTextures()
    {
        materialToEdit.SetShaderParam("albedoTexture", wigglyNess);
        materialToEdit.SetShaderParam("damagedTexture", wigglyNess);
    }
}
