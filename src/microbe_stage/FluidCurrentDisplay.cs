using Godot;

/// <summary>
///   Displays fluid currents in the microbe stage
/// </summary>
public partial class FluidCurrentDisplay : GpuParticles3D
{
#pragma warning disable CA2213
    private ShaderMaterial material = null!;

    private MicrobeStage stage = null!;
#pragma warning restore CA2213

    private StringName gameTimeParameterName = new("gameTime");
    private StringName speedParameterName = new("speed");
    private StringName chaoticnessParameterName = new("chaoticness");
    private StringName scaleParameterName = new("scale");

    private float time;

    public void Init(MicrobeStage microbeStage)
    {
        stage = microbeStage;
    }

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
            material.SetShaderParameter(gameTimeParameterName, time);

            SpeedScale = stage.WorldSimulation.WorldTimeScale;

            time += (float)(delta * SpeedScale);
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
