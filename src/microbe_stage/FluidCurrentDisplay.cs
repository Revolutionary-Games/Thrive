﻿using System;
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
    private readonly StringName scaleParameterName = new("scale");
    private readonly StringName brightnessParameterName = new("brightness");

#pragma warning disable CA2213
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

        material.SetShaderParameter(brightnessParameterName, biome.CompoundCloudBrightness);

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
            brightnessParameterName.Dispose();
        }

        base.Dispose(disposing);
    }
}
