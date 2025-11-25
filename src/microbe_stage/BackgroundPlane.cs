using System;
using Godot;

/// <summary>
///   Manages the microbe background plane, optionally applies blur.
/// </summary>
public partial class BackgroundPlane : Node3D
{
    private readonly StringName blurAmountParameter = new("blurAmount");
    private readonly StringName textureAlbedoParameter = new("textureAlbedo");
    private readonly StringName worldPositionParameter = new("worldPos");
    private readonly StringName sunlightColorParameter = new("sunlightColor");
    private readonly StringName lightIntensityParamter = new("lightIntensity");
    private readonly StringName distortionStrengthParameter = new("distortionFactor");
    private readonly StringName lightingColorParameter = new("lightColor");

#pragma warning disable CA2213

    /// <summary>
    ///   Background plane that is moved farther away from the camera when zooming out
    /// </summary>
    [Export]
    private MeshInstance3D backgroundPlane = null!;

    [Export]
    private MeshInstance3D blurResultPlane = null!;

    [Export]
    private ColorRect blurColorRect = null!;

    private GpuParticles3D? backgroundParticles;

    [Export]
    private SubViewport backgroundSubViewport = null!;

    [Export]
    private SubViewport partialBlurSubViewport = null!;

    private ShaderMaterial currentBackgroundMaterial = null!;

    private ShaderMaterial spatialBlurMaterial = null!;

    private ShaderMaterial canvasBlurMaterial = null!;
#pragma warning restore CA2213

    private bool blurEnabledLastTime;

    private double elapsed;

    private Vector2 previousWindowSize = new(1280, 720);

    private float lastSetLightLevel;

    public float PlaneOffset
    {
        get => backgroundPlane.Position.Z;
        set
        {
            backgroundPlane.Position = new Vector3(0, 0, value);

            // Move the result plane too to avoid it occluding something
            blurResultPlane.Position = new Vector3(0, 0, value);
        }
    }

    public override void _Ready()
    {
        var material = backgroundPlane.MaterialOverride;
        var planeBlurMaterial = blurResultPlane.MaterialOverride;
        var colorRectBlurMaterial = blurColorRect.Material;

        if (material == null || planeBlurMaterial == null || colorRectBlurMaterial == null)
        {
            GD.PrintErr("BackgroundPlane didn't find material to update");
            return;
        }

        currentBackgroundMaterial = (ShaderMaterial)material;
        spatialBlurMaterial = (ShaderMaterial)planeBlurMaterial;
        canvasBlurMaterial = (ShaderMaterial)colorRectBlurMaterial;

        UpdateSubViewportResolution();
        ApplyDistortionEffect();
        ApplyBlurEffect();
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Settings.Instance.DisplayBackgroundParticles.OnChanged += OnDisplayBackgroundParticlesChanged;
        Settings.Instance.MicrobeDistortionStrength.OnChanged += OnBackgroundDistortionChanged;

        Settings.Instance.MicrobeBackgroundBlurStrength.OnChanged += OnBackgroundBlurStrengthChanged;
        Settings.Instance.MicrobeBackgroundBlurLowQuality.OnChanged += UpdateBlurQuality;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Settings.Instance.DisplayBackgroundParticles.OnChanged -= OnDisplayBackgroundParticlesChanged;
        Settings.Instance.MicrobeDistortionStrength.OnChanged -= OnBackgroundDistortionChanged;

        Settings.Instance.MicrobeBackgroundBlurStrength.OnChanged -= OnBackgroundBlurStrengthChanged;
        Settings.Instance.MicrobeBackgroundBlurLowQuality.OnChanged -= UpdateBlurQuality;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        SetWorldPosition(new Vector2(GlobalPosition.X, GlobalPosition.Z));

        elapsed += delta;

        if (elapsed > 1.0f)
        {
            UpdateSubViewportResolution();

            elapsed = 0.0f;
        }
    }

    /// <summary>
    ///   Set the used background images and particles
    /// </summary>
    public void SetBackground(Background background)
    {
        // TODO: skip duplicate background changes

        for (int i = 0; i < 4; ++i)
        {
            // TODO: switch this loop away to reuse StringName instances if this causes significant allocations
            currentBackgroundMaterial.SetShaderParameter($"layer{i:n0}", GD.Load<Texture2D>(background.Textures[i]));
        }

        // Skip the particle recreation if they are already set correctly
        if (backgroundParticles != null && background.ParticleEffect != null &&
            backgroundParticles.SceneFilePath == background.ParticleEffect)
        {
            GD.Print("Particles are already correct, no need to recreate");
            return;
        }

        if (background.ParticleEffectScene == null)
        {
            backgroundParticles?.DetachAndQueueFree();
            backgroundParticles = null;
            return;
        }

        backgroundParticles = background.ParticleEffectScene.Instantiate<GpuParticles3D>();
        backgroundParticles.Rotation = Rotation;
        backgroundParticles.LocalCoords = false;

        AddChild(backgroundParticles);

        OnDisplayBackgroundParticlesChanged(Settings.Instance.DisplayBackgroundParticles);
    }

    public void SetVisibility(bool visible)
    {
        Visible = visible;

        if (backgroundParticles != null)
            OnDisplayBackgroundParticlesChanged(Settings.Instance.DisplayBackgroundParticles);
    }

    public void UpdateLightLevel(float lightLevel)
    {
        // Don't do the calculation and sending to gpu again if not needed.
        if (lastSetLightLevel == lightLevel)
            return;

        lastSetLightLevel = lightLevel;
        currentBackgroundMaterial.SetShaderParameter(lightIntensityParamter, lightLevel);
        currentBackgroundMaterial.SetShaderParameter(sunlightColorParameter, CalculateSunlightColor(lightLevel));
    }

    public void SetLightingColor(float oxygen, float iron)
    {
        // Iron and oxygen is red
        if (iron > 2.5f && oxygen > 0.1f)
        {
            GD.Print("Red");
            currentBackgroundMaterial.SetShaderParameter(lightingColorParameter,
                new Vector3(1.0f, 0.2f, 0.2f));
        }

        // Lots of iron is greener
        else if (iron > 2.5f)
        {
            GD.Print("Green");
            currentBackgroundMaterial.SetShaderParameter(lightingColorParameter,
                new Vector3(0.2f, 1.0f, 0.2f));
        }

        // Regular color
        else
        {
            GD.Print("None");
            currentBackgroundMaterial.SetShaderParameter(lightingColorParameter,
                new Vector3(1.0f, 1.0f, 1.0f));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            textureAlbedoParameter.Dispose();
            blurAmountParameter.Dispose();
            sunlightColorParameter.Dispose();
            distortionStrengthParameter.Dispose();
            worldPositionParameter.Dispose();
            lightIntensityParamter.Dispose();
            lightingColorParameter.Dispose();
        }

        base.Dispose(disposing);
    }

    private Vector3 CalculateSunlightColor(float lightLevel)
    {
        // I'm not sure how any of these values were picked. I've just copied from the shader file

        // Day
        if (lightLevel > 0.5f)
            return new Vector3(0.75f, 0.5f, 0.5f).Lerp(Vector3.One, 2.0f * lightLevel - 1.0f);

        // Dawn and Dusk
        if (lightLevel > 0.25f)
            return new Vector3(0.25f, 0.25f, 0.25f).Lerp(new Vector3(0.75f, 0.5f, 0.5f), 4.0f * lightLevel - 1.0f);

        // Night
        return new Vector3(0.052f, 0.05f, 0.17f)
            .Lerp(new Vector3(0.25f, 0.25f, 0.25f), 4.0f * lightLevel);
    }

    private void UpdateSubViewportResolution()
    {
        Vector2I newSize;

        var settings = Settings.Instance;

        float renderScale = settings.RenderScale.Value;

        var rawSize = GetWindow().Size;

        var scaledSize = new Vector2I((int)Math.Round(rawSize.X * renderScale),
            (int)Math.Round(rawSize.Y * renderScale));

        if (settings.MicrobeBackgroundBlurLowQuality.Value)
        {
            newSize = new Vector2I(1280, 720);

            // If low quality is higher than the scaled value, go down to that resolution
            if (scaledSize.X < newSize.X || scaledSize.Y < newSize.Y)
            {
                newSize = scaledSize;
            }
        }
        else
        {
            newSize = scaledSize;
        }

        if (previousWindowSize != newSize)
        {
            previousWindowSize = newSize;
            backgroundSubViewport.Size = newSize;
            partialBlurSubViewport.Size = newSize;
        }
    }

    private void UpdateBlurQuality(bool isLowQuality)
    {
        UpdateSubViewportResolution();
    }

    private void SetWorldPosition(Vector2 position)
    {
        currentBackgroundMaterial.SetShaderParameter(worldPositionParameter, position);
    }

    private void OnDisplayBackgroundParticlesChanged(bool displayed)
    {
        if (backgroundParticles == null)
        {
            GD.PrintErr("BackgroundPlane didn't find background particles on settings change");
            return;
        }

        backgroundParticles.Emitting = displayed;

        if (displayed)
        {
            backgroundParticles.Show();
        }
        else
        {
            backgroundParticles.Hide();
        }
    }

    private void OnBackgroundDistortionChanged(float value)
    {
        ApplyDistortionEffect();
    }

    private void ApplyDistortionEffect()
    {
        currentBackgroundMaterial.SetShaderParameter(distortionStrengthParameter,
            Settings.Instance.MicrobeDistortionStrength.Value);
    }

    private void OnBackgroundBlurStrengthChanged(float value)
    {
        ApplyBlurEffect();
    }

    private void ApplyBlurEffect()
    {
        float blurStrength = Settings.Instance.MicrobeBackgroundBlurStrength;
        bool enabled = blurStrength > 0;

        SetBlurStrength(blurStrength);

        if (blurEnabledLastTime == enabled)
            return;

        blurEnabledLastTime = enabled;

        if (enabled)
        {
            blurResultPlane.Visible = true;

            RemoveChild(backgroundPlane);
            backgroundSubViewport.AddChild(backgroundPlane);

            backgroundSubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
            partialBlurSubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        }
        else
        {
            backgroundSubViewport.RemoveChild(backgroundPlane);
            AddChild(backgroundPlane);

            backgroundSubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            partialBlurSubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;

            blurResultPlane.Visible = false;
        }
    }

    private void SetBlurStrength(float value)
    {
        canvasBlurMaterial.SetShaderParameter(blurAmountParameter, value);
        spatialBlurMaterial.SetShaderParameter(blurAmountParameter, value);
    }
}
