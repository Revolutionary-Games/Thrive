using System;
using Godot;

/// <summary>
///   Manages the microbe background plane, optionally applies blur.
/// </summary>
public partial class BackgroundPlane : Node3D, IGodotEarlyNodeResolve
{
    private readonly StringName blurAmountParameter = new("blurAmount");
    private readonly StringName textureAlbedoParameter = new("textureAlbedo");
    private readonly StringName worldPositionParameter = new("worldPos");
    private readonly StringName lightLevelParameter = new("lightLevel");
    private readonly StringName distortionStrengthParameter = new("distortionFactor");

    [Export]
    private NodePath? blurPlanePath;

    [Export]
    private NodePath blurColorRectPath = null!;

    [Export]
    private NodePath backgroundPlanePath = null!;

    [Export]
    private Texture2D blankTexture = null!;

#pragma warning disable CA2213

    /// <summary>
    ///   Background plane that is moved farther away from the camera when zooming out
    /// </summary>
    private Node3D backgroundPlane = null!;

    private Node3D blurPlane = null!;

    private GpuParticles3D? backgroundParticles;

    private ShaderMaterial spatialBlurMaterial = null!;

    private ShaderMaterial canvasBlurMaterial = null!;

    private ShaderMaterial currentBackgroundMaterial = null!;

    private SubViewport subViewport1 = null!;

    private SubViewport subViewport2 = null!;
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

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        NodeReferencesResolved = true;

        if (HasNode(backgroundPlanePath))
            backgroundPlane = GetNode<Node3D>(backgroundPlanePath);

        if (HasNode(blurPlanePath))
            blurPlane = GetNode<Node3D>(blurPlanePath);

        if (HasNode("SubViewport"))
            subViewport1 = GetNode<SubViewport>("SubViewport");

        if (HasNode("SubViewport2"))
            subViewport2 = GetNode<SubViewport>("SubViewport2");
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Settings.Instance.DisplayBackgroundParticles.OnChanged += OnDisplayBackgroundParticlesChanged;
        Settings.Instance.MicrobeDistortionStrength.OnChanged += OnBackgroundDistortionChanged;

        Settings.Instance.MicrobeBackgroundBlurStrength.OnChanged += OnBackgroundBlurStrengthChanged;
        Settings.Instance.MicrobeBackgroundBlurEnabled.OnChanged += OnBackgroundBlurToggleChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Settings.Instance.DisplayBackgroundParticles.OnChanged -= OnDisplayBackgroundParticlesChanged;
        Settings.Instance.MicrobeDistortionStrength.OnChanged -= OnBackgroundDistortionChanged;

        Settings.Instance.MicrobeBackgroundBlurStrength.OnChanged -= OnBackgroundBlurStrengthChanged;
        Settings.Instance.MicrobeBackgroundBlurEnabled.OnChanged -= OnBackgroundBlurToggleChanged;
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
            blankTexture.Dispose();

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

    private void OnBackgroundBlurToggleChanged(bool value)
    {
        ApplyBlurEffect();
    }

    private void ApplyBlurEffect()
    {
        float blurStrength = Settings.Instance.MicrobeBackgroundBlurStrength;
        bool enabled = blurStrength > 0 && Settings.Instance.MicrobeBackgroundBlurEnabled.Value;

        if (enabled)
        {
            blurPlane.Visible = true;

            canvasBlurMaterial.SetShaderParameter(blurAmountParameter, blurStrength);
            spatialBlurMaterial.SetShaderParameter(blurAmountParameter, blurStrength);

            if (backgroundPlane.GetParent() == this)
            {
                RemoveChild(backgroundPlane);
                subViewport1.AddChild(backgroundPlane);
            }

            canvasBlurMaterial.SetShaderParameter(textureAlbedoParameter, subViewport1.GetTexture());
            spatialBlurMaterial.SetShaderParameter(textureAlbedoParameter, subViewport2.GetTexture());
        }
        else
        {
            if (backgroundPlane.GetParent() != this)
            {
                subViewport1.RemoveChild(backgroundPlane);
                AddChild(backgroundPlane);
            }

            // Remove viewport textures from shaders, so that the viewports aren't rendered
            canvasBlurMaterial.SetShaderParameter(textureAlbedoParameter, blankTexture);
            spatialBlurMaterial.SetShaderParameter(textureAlbedoParameter, blankTexture);

            blurPlane.Visible = false;
        }
    }
}
