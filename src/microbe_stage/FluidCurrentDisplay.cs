using System;
using Godot;

/// <summary>
///   Displays fluid currents in the microbe stage
/// </summary>
public partial class FluidCurrentDisplay : GpuParticles3D
{
    private const float MIN_DISTANCE_TO_REPOSITION = 1.0f;

#pragma warning disable CA2213
    private ShaderMaterial material = null!;

    private MicrobeStage stage = null!;

    private Node3D parent = null!;
#pragma warning restore CA2213

    private StringName gameTimeParameterName = new("gameTime");
    private StringName speedParameterName = new("speed");
    private StringName chaoticnessParameterName = new("chaoticness");
    private StringName scaleParameterName = new("scale");

    private float time;

    private Vector3 previousParentPosition;

    public void Init(MicrobeStage microbeStage, Node3D parentNode)
    {
        stage = microbeStage;
        parent = parentNode;
    }

    public override void _Ready()
    {
        base._Ready();

        material = (ShaderMaterial)ProcessMaterial;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var parentPos = parent.Position;

        if (MathF.Abs(parentPos.X - previousParentPosition.X) > MIN_DISTANCE_TO_REPOSITION
            || MathF.Abs(parentPos.Z - previousParentPosition.Z) > MIN_DISTANCE_TO_REPOSITION)
        {
            GlobalPosition = new Vector3(parentPos.X, 1.0f, parentPos.Z);
            previousParentPosition = parentPos;
        }
        else
        {
            GlobalPosition = new Vector3(previousParentPosition.X, 1.0f, previousParentPosition.Z);
        }

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
