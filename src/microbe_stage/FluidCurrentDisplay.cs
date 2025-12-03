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
    private readonly StringName particleDepthVariationParameterName = new("particleDepthVariation");

#pragma warning disable CA2213
    [Export]
    private Mesh normalParticleMesh = null!;

    [Export]
    private Mesh trailedParticleMesh = null!;

    private ShaderMaterial material = null!;

    private Node3D parent = null!;

    private IWorldSimulation timeScaling = null!;
#pragma warning restore CA2213

    private Settings.MicrobeCurrentParticlesMode initializedMode;

    private float time;

    private Vector3 previousParentPosition;

    /// <summary>
    ///   Remembered previous biome for re-enabling the effect
    /// </summary>
    private Biome? previousBiome;

    public override void _Ready()
    {
        base._Ready();

        material = (ShaderMaterial)ProcessMaterial;
        initializedMode = Settings.Instance.MicrobeCurrentParticles;
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Settings.Instance.MicrobeCurrentParticles.OnChanged += OnModeChanged;

        if (material != null!)
        {
            // Apply any mode updates we missed while being detached
            OnModeChanged(Settings.Instance.MicrobeCurrentParticles);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Settings.Instance.MicrobeCurrentParticles.OnChanged -= OnModeChanged;
    }

    public void Init(IWorldSimulation worldTimeSource, Node3D parentNode)
    {
        timeScaling = worldTimeSource;
        parent = parentNode;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (initializedMode == Settings.MicrobeCurrentParticlesMode.None)
            return;

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

        SpeedScale = timeScaling.WorldTimeScale;
    }

    public void ApplyBiome(Biome biome)
    {
        // Remember the settings we should have if we are enabled later
        previousBiome = biome;

        if (initializedMode == Settings.MicrobeCurrentParticlesMode.None)
        {
            Visible = false;
            return;
        }

        Visible = true;

        material.SetShaderParameter(speedParameterName, biome.WaterCurrents.Speed);
        material.SetShaderParameter(chaoticnessParameterName, biome.WaterCurrents.Chaoticness);
        material.SetShaderParameter(inverseScaleParameterName, biome.WaterCurrents.InverseScale);

        material.SetShaderParameter(colorParameterName, biome.WaterCurrents.Colour);

        TrailEnabled = biome.WaterCurrents.UseTrails && initializedMode == Settings.MicrobeCurrentParticlesMode.All;
        if (TrailEnabled)
        {
            DrawPass1 = trailedParticleMesh;
            material.SetShaderParameter(particleDepthVariationParameterName, 0.0f);
        }
        else
        {
            DrawPass1 = normalParticleMesh;
            material.SetShaderParameter(particleDepthVariationParameterName, 1.0f);
        }

        Amount = biome.WaterCurrents.ParticleCount;
    }

    public void UpdateLightLevel(Patch patch)
    {
        float lightLevel = patch.Biome.GetCompound(Compound.Sunlight, CompoundAmountType.Current).Ambient;

        // Store this in the material as we don't have an easy variable to remember this if we happen to be re-enabled
        // later
        material.SetShaderParameter(brightnessParameterName,
            (patch.BiomeTemplate.CompoundCloudBrightness - 1.0f) * lightLevel + 1.0f);
    }

    public void UpdateTime(float newTime)
    {
        time = newTime;

        if (initializedMode == Settings.MicrobeCurrentParticlesMode.None)
            return;

        material.SetShaderParameter(gameTimeParameterName, time);
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
            particleDepthVariationParameterName.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnModeChanged(Settings.MicrobeCurrentParticlesMode value)
    {
        if (initializedMode == value)
            return;

        var old = initializedMode;

        // Need to react to option change while the game is running
        initializedMode = value;

        if (initializedMode == Settings.MicrobeCurrentParticlesMode.None)
        {
            Visible = false;
            return;
        }

        // Switching between the enabled modes
        if (old == Settings.MicrobeCurrentParticlesMode.All &&
            initializedMode == Settings.MicrobeCurrentParticlesMode.OnlyCircles)
        {
            if (TrailEnabled)
            {
                TrailEnabled = false;
                DrawPass1 = normalParticleMesh;
                material.SetShaderParameter(particleDepthVariationParameterName, 1.0f);
            }

            return;
        }

        if (old == Settings.MicrobeCurrentParticlesMode.OnlyCircles &&
            initializedMode == Settings.MicrobeCurrentParticlesMode.All)
        {
            // Only switch to trails if the biome had trails before
            if (!TrailEnabled)
            {
                if (previousBiome == null)
                {
                    GD.PrintErr("Cannot enable trails without knowing a biome to apply");
                    return;
                }

                if (previousBiome.WaterCurrents.UseTrails)
                {
                    TrailEnabled = true;
                    DrawPass1 = trailedParticleMesh;
                    material.SetShaderParameter(particleDepthVariationParameterName, 0.0f);
                }
            }

            return;
        }

        // Disabled to activated state change
        if (previousBiome != null)
        {
            ApplyBiome(previousBiome);
            material.SetShaderParameter(gameTimeParameterName, time);
            Visible = true;
        }
        else
        {
            GD.PrintErr("Cannot enable fluid current display without knowing a biome to apply");
        }
    }
}
