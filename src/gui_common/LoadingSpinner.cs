using System;
using Godot;

/// <summary>
///   A simple spinner that spins when visible.
/// </summary>
public partial class LoadingSpinner : Control
{
    /// <summary>
    ///   How fast the loading indicator spins
    /// </summary>
    [Export]
    public double SpinnerSpeed = Math.PI;

    private readonly StringName rotationName = new("rotation");

#pragma warning disable CA2213
    [Export]
    private TextureRect spinner = null!;

    private ShaderMaterial spinnerMaterial = null!;
#pragma warning restore CA2213

    private float currentSpinnerRotation;

    public override void _Ready()
    {
        spinnerMaterial = (ShaderMaterial)spinner.Material;

        if (spinnerMaterial == null)
        {
            GD.PrintErr("LoadingSpinner: No material found");
        }
    }

    public override void _Process(double delta)
    {
        // It's probably more expensive to update the shader parameters every frame than checking the tree for
        // visibility
        if (!IsVisibleInTree())
            return;

        currentSpinnerRotation += (float)(delta * SpinnerSpeed);
        currentSpinnerRotation %= MathF.Tau;
        spinnerMaterial.SetShaderParameter(rotationName, currentSpinnerRotation);
    }

    public void ResetSpin()
    {
        currentSpinnerRotation = 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            rotationName.Dispose();
        }

        base.Dispose(disposing);
    }
}
