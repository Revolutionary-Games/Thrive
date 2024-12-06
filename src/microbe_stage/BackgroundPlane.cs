using Godot;

/// <summary>
///   Manages the microbe background plane, optionally applies blur.
/// </summary>
public partial class BackgroundPlane : Node3D
{
    private readonly StringName blurAmountParameter = new("blurAmount");
    private readonly StringName textureAlbedoParameter = new("textureAlbedo");
    private readonly StringName worldPositionParameter = new("worldPos");
    private readonly StringName lightLevelParameter = new("lightLevel");
    private readonly StringName distortionStrengthParameter = new("distortionFactor");

#pragma warning disable CA2213

    /// <summary>
    ///   Background plane that is moved farther away from the camera when zooming out
    /// </summary>
    [Export]
    private CsgMesh3D backgroundPlane = null!;

    [Export]
    private CsgMesh3D blurPlane = null!;

    [Export]
    private ColorRect blurColorRect = null!;

    private GpuParticles3D? backgroundParticles;

    [Export]
    private SubViewport subViewport1 = null!;

    [Export]
    private SubViewport subViewport2 = null!;

    private ShaderMaterial currentBackgroundMaterial = null!;

    private ShaderMaterial spatialBlurMaterial = null!;

    private ShaderMaterial canvasBlurMaterial = null!;
#pragma warning restore CA2213

    private bool blurEnabledLastTime = true;

    public float PlaneOffset
    {
        get
        {
            return backgroundPlane.Position.Z;
        }
        set
        {
            backgroundPlane.Position = new Vector3(0, 0, value);
        }
    }

    public override void _Ready()
    {
        var material = backgroundPlane.Material;
        var planeBlurMaterial = blurPlane.Material;
        var colorRectBlurMaterial = blurColorRect.Material;

        if (material == null || planeBlurMaterial == null || colorRectBlurMaterial == null)
        {
            GD.PrintErr("BackgroundPlane didn't find material to update");
            return;
        }

        currentBackgroundMaterial = (ShaderMaterial)material;
        spatialBlurMaterial = (ShaderMaterial)planeBlurMaterial;
        canvasBlurMaterial = (ShaderMaterial)colorRectBlurMaterial;

        ApplyDistortionEffect();
        ApplyBlurEffect();
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Settings.Instance.DisplayBackgroundParticles.OnChanged += OnDisplayBackgroundParticlesChanged;
        Settings.Instance.MicrobeDistortionStrength.OnChanged += OnBackgroundDistortionChanged;

        Settings.Instance.MicrobeBackgroundBlurStrength.OnChanged += OnBackgroundBlurStrengthChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Settings.Instance.DisplayBackgroundParticles.OnChanged -= OnDisplayBackgroundParticlesChanged;
        Settings.Instance.MicrobeDistortionStrength.OnChanged -= OnBackgroundDistortionChanged;

        Settings.Instance.MicrobeBackgroundBlurStrength.OnChanged -= OnBackgroundBlurStrengthChanged;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
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

        backgroundParticles?.DetachAndQueueFree();

        backgroundParticles = background.ParticleEffectScene.Instantiate<GpuParticles3D>();
        backgroundParticles.Rotation = Rotation;
        backgroundParticles.LocalCoords = false;

        AddChild(backgroundParticles);

        OnDisplayBackgroundParticlesChanged(Settings.Instance.DisplayBackgroundParticles);
    }

    public void SetWorldPosition(Vector2 position)
    {
        currentBackgroundMaterial.SetShaderParameter(worldPositionParameter, position);
    }

    public void SetVisibility(bool visible)
    {
        Visible = visible;

        if (backgroundParticles != null)
            OnDisplayBackgroundParticlesChanged(Settings.Instance.DisplayBackgroundParticles);
    }

    public void UpdateLightLevel(float lightLevel)
    {
        currentBackgroundMaterial.SetShaderParameter(lightLevelParameter, lightLevel);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            textureAlbedoParameter.Dispose();
            blurAmountParameter.Dispose();
            lightLevelParameter.Dispose();
            distortionStrengthParameter.Dispose();
            worldPositionParameter.Dispose();
        }

        base.Dispose(disposing);
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
        {
            return;
        }

        blurEnabledLastTime = enabled;

        if (enabled)
        {
            blurPlane.Visible = true;

            if (backgroundPlane.GetParent() == this)
            {
                RemoveChild(backgroundPlane);
                subViewport1.AddChild(backgroundPlane);
            }

            subViewport1.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
            subViewport2.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        }
        else
        {
            if (backgroundPlane.GetParent() != this)
            {
                subViewport1.RemoveChild(backgroundPlane);
                AddChild(backgroundPlane);
            }

            subViewport1.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            subViewport2.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;

            blurPlane.Visible = false;
        }
    }

    private void SetBlurStrength(float value)
    {
        canvasBlurMaterial.SetShaderParameter(blurAmountParameter, value);
        spatialBlurMaterial.SetShaderParameter(blurAmountParameter, value);
    }
}
