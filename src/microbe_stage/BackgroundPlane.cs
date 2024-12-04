using System;
using Godot;

/// <summary>
///   Manages the microbe background plane, optionally applies blur.
/// </summary>
public partial class BackgroundPlane : CsgMesh3D, IGodotEarlyNodeResolve
{
    private readonly StringName applyBlurParameter = new("applyBlur");
    private readonly StringName worldPositionParameter = new("worldPos");
    private readonly StringName lightLevelParameter = new("lightLevel");
    private readonly StringName distortionStrengthParameter = new("distortionFactor");

    [Export]
    private NodePath? blurPlanePath;

    [Export]
    private NodePath blurColorRectPath = null!;

    [Export]
    private NodePath backgroundPlanePath = null!;

#pragma warning disable CA2213

    /// <summary>
    ///   Background plane that is moved farther away from the camera when zooming out
    /// </summary>
    private Node3D backgroundPlane = null!;

    private GpuParticles3D? backgroundParticles;

    private ShaderMaterial? spatialBlurMaterial;

    private ShaderMaterial? canvasBlurMaterial;

    private ShaderMaterial? currentBackgroundMaterial;
#pragma warning restore CA2213

    public bool NodeReferencesResolved { get; private set; }

    public float PlaneOffset
    {
        get
        {
            return backgroundPlane.Position.Z + 15;
        }
        set
        {
            backgroundPlane.Position = new Vector3(0, 0, -15 + value);
        }
    }

    public override void _Ready()
    {
        var material = GetNode<CsgMesh3D>(backgroundPlanePath).Material;
        var planeBlurMaterial = GetNode<CsgMesh3D>(blurPlanePath).Material;
        var colorRectBlurMaterial = GetNode<CanvasItem>(blurColorRectPath).Material;

        if (material == null || planeBlurMaterial == null || colorRectBlurMaterial == null)
        {
            GD.PrintErr("MicrobeCamera didn't find material to update");
            return;
        }

        currentBackgroundMaterial = (ShaderMaterial)material;
        spatialBlurMaterial = (ShaderMaterial)planeBlurMaterial;
        canvasBlurMaterial = (ShaderMaterial)colorRectBlurMaterial;

        ResolveNodeReferences();
        ApplyDistortionEffect();
        ApplyBlurEffect();
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Settings.Instance.DisplayBackgroundParticles.OnChanged += OnDisplayBackgroundParticlesChanged;
        Settings.Instance.MicrobeDistortionStrength.OnChanged += OnBackgroundDistortionChanged;

        ApplyDistortionEffect();
        ApplyBlurEffect();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Settings.Instance.DisplayBackgroundParticles.OnChanged -= OnDisplayBackgroundParticlesChanged;
        Settings.Instance.MicrobeDistortionStrength.OnChanged -= OnBackgroundDistortionChanged;
    }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        NodeReferencesResolved = true;

        if (HasNode(backgroundPlanePath))
            backgroundPlane = GetNode<Node3D>(backgroundPlanePath);
    }

    /// <summary>
    ///   Set the used background images and particles
    /// </summary>
    public void SetBackground(Background background)
    {
        // TODO: skip duplicate background changes

        if (currentBackgroundMaterial == null)
            throw new InvalidOperationException("Camera not initialized yet");

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
        currentBackgroundMaterial?.SetShaderParameter(worldPositionParameter, position);
    }

    public void SetVisibility(bool visible)
    {
        Visible = visible;

        if (backgroundParticles != null)
            OnDisplayBackgroundParticlesChanged(Settings.Instance.DisplayBackgroundParticles);
    }

    public void UpdateLightLevel(float lightLevel)
    {
        currentBackgroundMaterial?.SetShaderParameter(lightLevelParameter, lightLevel);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            applyBlurParameter.Dispose();
            lightLevelParameter.Dispose();
            distortionStrengthParameter.Dispose();
            worldPositionParameter.Dispose();

            if (blurPlanePath != null)
            {
                blurPlanePath.Dispose();
                blurColorRectPath.Dispose();
                backgroundPlanePath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnDisplayBackgroundParticlesChanged(bool displayed)
    {
        if (backgroundParticles == null)
        {
            GD.PrintErr("MicrobeCamera didn't find background particles on settings change");
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
        ApplyBlurEffect();
    }

    private void ApplyDistortionEffect()
    {
        currentBackgroundMaterial?.SetShaderParameter(distortionStrengthParameter,
            Settings.Instance.MicrobeDistortionStrength.Value);
    }

    private void ApplyBlurEffect()
    {
        bool enabled = Settings.Instance.MicrobeDistortionStrength.Value > 0.0f;
        canvasBlurMaterial?.SetShaderParameter(applyBlurParameter, enabled);
        spatialBlurMaterial?.SetShaderParameter(applyBlurParameter, enabled);
    }
}
