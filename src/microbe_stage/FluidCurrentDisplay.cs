using Godot;

/// <summary>
///   Displays fluid currents in the microbe stage
/// </summary>
public partial class FluidCurrentDisplay : GpuParticles3D
{
    private readonly StringName gameTimeParameterName = new("gameTime");
    private readonly StringName speedParameterName = new("speed");
    private readonly StringName chaoticnessParameterName = new("chaoticness");
    private readonly StringName scaleParameterName = new("scale");

#pragma warning disable CA2213
    private ShaderMaterial material = null!;

    private IWorldSimulation timeScaling = null!;
#pragma warning restore CA2213

    private float time;

    public override void _Ready()
    {
        base._Ready();

        material = (ShaderMaterial)ProcessMaterial;
    }

    public void Init(IWorldSimulation worldTimeSource)
    {
        timeScaling = worldTimeSource;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        GlobalPosition = new Vector3(GlobalPosition.X, 1.0f, GlobalPosition.Z);

        if (!PauseManager.Instance.Paused)
        {
            SpeedScale = timeScaling.WorldTimeScale;

            time += (float)(delta * SpeedScale);

            material.SetShaderParameter(gameTimeParameterName, time);
        }
        else
        {
            SpeedScale = 0.0f;
        }
    }

    public void ApplyBiome(Biome biome)
    {
        material.SetShaderParameter(speedParameterName, biome.WaterCurrentSpeed);
        material.SetShaderParameter(chaoticnessParameterName, biome.WaterCurrentChaoticness);
        material.SetShaderParameter(scaleParameterName, biome.WaterCurrentScale);

        Amount = biome.WaterCurrentParticleCount;
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
