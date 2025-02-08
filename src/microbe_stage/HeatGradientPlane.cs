using System;
using Godot;

/// <summary>
///   Handles updating the heat gradient rendering parameters
/// </summary>
public partial class HeatGradientPlane : MeshInstance3D
{
    private readonly StringName uvParameterName = new("uvOffset");

#pragma warning disable CA2213
    private ShaderMaterial material = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        material = (ShaderMaterial)MaterialOverride;

        if (material == null)
            throw new InvalidOperationException("Material not set on HeatGradientPlane");

#if DEBUG
        var repeat = Constants.MICROBE_HEAT_AREA_REPEAT_EVERY_WORLD_COORDINATE;
        if (((PlaneMesh)Mesh).Size != new Vector2(repeat, repeat))
        {
            throw new InvalidOperationException("Heat gradient plane mesh size is not set to " + repeat);
        }
#endif
    }

    public override void _Process(double delta)
    {
        var pos = GlobalPosition;

        // Ensure this always stays at Y=0
        if (pos.Y != 0)
        {
            SetGlobalPosition(new Vector3(pos.X, 0, pos.Z));
        }

        var positionX = pos.X;
        var positionZ = pos.Z;

        // Scale position based on how many world units there are per unit of UV
        positionX *= Constants.MICROBE_HEAT_NOISE_TO_WORLD_RATIO;
        positionZ *= Constants.MICROBE_HEAT_NOISE_TO_WORLD_RATIO;

        // Send position to UV handling
        material.SetShaderParameter(uvParameterName, new Vector2(positionX, positionZ));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            uvParameterName.Dispose();
        }

        base.Dispose(disposing);
    }
}
