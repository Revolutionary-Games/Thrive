using System;
using Godot;

/// <summary>
///   Displays fluid currents in the microbe stage
/// </summary>
public partial class FluidCurrentDisplay : GpuParticles3D
{
    private const float MIN_DISTANCE_TO_REPOSITION = 10.0f;
    private readonly StringName gameTimeParameterName = new("gameTime");
    private readonly StringName speedParameterName = new("speed");
    private readonly StringName chaoticnessParameterName = new("chaoticness");
    private readonly StringName inverseScaleParameterName = new("inverseScale");
    private readonly StringName brightnessParameterName = new("brightness");
    private readonly StringName colorParameterName = new("colorValue");

#pragma warning disable CA2213
    [Export]
    private Mesh normalParticleMesh = null!;

    [Export]
    private Mesh trailedParticleMesh = null!;

    private ShaderMaterial material = null!;

    private Node3D parent = null!;

    private IWorldSimulation timeScaling = null!;
#pragma warning restore CA2213

    private float time;

    private Vector3 previousParentPosition;

    public override void _Ready()
    {
        base._Ready();

        material = (ShaderMaterial)ProcessMaterial;
    }

    public void Init(IWorldSimulation worldTimeSource, Node3D parentNode)
    {
        timeScaling = worldTimeSource;
        parent = parentNode;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var parentPos = parent.Position;

        // Position is only updated once it is different enough to avoid visual jitter when the player is being carried
        // by a current
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
            SpeedScale = timeScaling.WorldTimeScale;

            time += (float)(delta * SpeedScale);

            material.SetShaderParameter(gameTimeParameterName, time);
        }
    }

    public void ApplyBiome(Biome biome)
    {
        material.SetShaderParameter(speedParameterName, biome.WaterCurrents.Speed);
        material.SetShaderParameter(chaoticnessParameterName, biome.WaterCurrents.Chaoticness);
        material.SetShaderParameter(inverseScaleParameterName, biome.WaterCurrents.InverseScale);

        material.SetShaderParameter(colorParameterName, biome.WaterCurrents.Colour);

        TrailEnabled = biome.WaterCurrents.UseTrails;
        if (biome.WaterCurrents.UseTrails)
        {
            DrawPass1 = trailedParticleMesh;
        }
        else
        {
            DrawPass1 = normalParticleMesh;
        }

        Amount = biome.WaterCurrents.ParticleCount;
    }

    public void UpdateLightLevel(Patch patch)
    {
        float lightLevel = patch.Biome.GetCompound(Compound.Sunlight, CompoundAmountType.Current).Ambient;

        material.SetShaderParameter(brightnessParameterName,
            (patch.BiomeTemplate.CompoundCloudBrightness - 1.0f) * lightLevel + 1.0f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gameTimeParameterName.Dispose();
            speedParameterName.Dispose();
            chaoticnessParameterName.Dispose();
            inverseScaleParameterName.Dispose();
            brightnessParameterName.Dispose();
            colorParameterName.Dispose();
        }

        base.Dispose(disposing);
    }
}
