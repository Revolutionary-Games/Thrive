using System;
using Godot;

/// <summary>
///   Displays fluid currents in the microbe stage
/// </summary>
public partial class WaterCurrentDisplay : GpuParticles3D
{
#pragma warning disable CA2213
    private ShaderMaterial material = null!;
#pragma warning restore CA2213

    private float time = 0.0f;

    public override void _Ready()
    {
        base._Ready();

        material = (ShaderMaterial)ProcessMaterial;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        GlobalPosition = new Vector3(GlobalPosition.X, 1.0f, GlobalPosition.Z);

        if (!PauseManager.Instance.Paused)
        {
            time += (float)delta;

            // TODO: make those into StringNames
            material.SetShaderParameter("gameTime", time);
        }
    }

    public void ApplyBiome(Biome biome)
    {
        material.SetShaderParameter("speed", biome.WaterCurrentSpeed);
        material.SetShaderParameter("chaoticness", biome.WaterCurrentChaoticness);
        material.SetShaderParameter("scale", biome.WaterCurrentScale);
    }
}
