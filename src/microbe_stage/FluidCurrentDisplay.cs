using Godot;

/// <summary>
///   Displays fluid currents in the microbe stage
/// </summary>
public partial class FluidCurrentDisplay : GpuParticles3D
{
#pragma warning disable CA2213
    private ShaderMaterial material = null!;
#pragma warning restore CA2213

    private StringName gameTimeParameterName = new("gameTime");
    private StringName speedParameterName = new("speed");
    private StringName chaoticnessParameterName = new("chaoticness");
    private StringName scaleParameterName = new("scale");

    private float time;

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

            material.SetShaderParameter(gameTimeParameterName, time);
        }
    }

    public void ApplyBiome(Biome biome)
    {
        material.SetShaderParameter(speedParameterName, biome.WaterCurrentSpeed);
        material.SetShaderParameter(chaoticnessParameterName, biome.WaterCurrentChaoticness);
        material.SetShaderParameter(scaleParameterName, biome.WaterCurrentScale);

        Amount = (int)((1.0f - biome.WaterCurrentSpeed * 0.33f) + (1.0f * biome.WaterCurrentChaoticness * 0.33f)
            + (1.0f - biome.WaterCurrentScale * 0.33f) * 300.0f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gameTimeParameterName.Dispose();
            speedParameterName.Dispose();
            chaoticnessParameterName.Dispose();
            scaleParameterName.Dispose();
        }

        base.Dispose(disposing);
    }
}
