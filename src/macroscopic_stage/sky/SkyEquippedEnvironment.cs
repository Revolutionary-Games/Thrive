using System;
using Godot;
using Godot.Collections;
using Environment = Godot.Environment;

/// <summary>
///   A special environment to be used in contexts where the sky is visible.
/// </summary>
/// <remarks>
///   <para>
///     This is designed to be compatible with both Forward+ and Compatibility renderers. If the RenderingDevice is
///     available, it is going to use compute shaders and compositor effects for cloud rendering, otherwise it falls
///     back to a fullscreen quad.
///   </para>
///   <para>
///     This is also responsible for sky clouds and colours renderer on specific planets.
///   </para>
/// </remarks>
[GlobalClass]
#if TOOLS_ENABLED
[Tool]
#endif
public partial class SkyEquippedEnvironment : WorldEnvironment
{
    /// <summary>
    ///   If true, forces the use of the fullscreen quad, even when the RenderingDevice is available. It also prevents
    ///   the VolumetricCloudsEffect initialization.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that this isn't equivalent to using the Compatibility renderer. Forward+ still uses different
    ///     techniques for lighting and tonemapping, so the results aren't equivalent.
    ///   </para>
    /// </remarks>
    [Export]
    public bool ForceUseFullscreenQuad;

#pragma warning disable CA2213
    /// <summary>
    ///   Material the sky is rendered with. When left unset, one is created automatically and bound to the shader at
    ///   <see cref="SkyShaderPath"/>.
    /// </summary>
    [Export]
    public ShaderMaterial? SkyMaterial;

    [Export]
    public AtmosphereConfig AtmosphereConfig = new();

    /// <summary>
    ///   Which gases the atmosphere is made of. This is what the sky's colour is derived from.
    /// </summary>
    [Export]
    public AtmosphereCompositionConfig Composition = new();

    [Export]
    public TraceGasConfig TraceGases = new();

    [Export]
    public SunConfig SunConfig = new();

    /// <summary>
    ///   Parameters of the cloud layer, used by the compositor effect or the fullscreen quad fallback.
    /// </summary>
    [Export]
    public CloudsConfig CloudsConfig = new();

    /// <summary>
    ///   Deliberately has no default value, as one gets created on demand below. An initializer here would be
    ///   thrown away when the scene value is applied, while still leaving a stray instance behind.
    /// </summary>
    [Export]
    public VolumetricCloudsEffect? CloudsEffect;
#pragma warning restore CA2213

    private const string SkyShaderPath = "res://shaders/sky/atmosphere_sky.gdshader";

    private const float TonemapAgxWhite = 6.0f;

    /// <summary>
    ///   Half size of the bounds given to the cloud quad. Its vertex shader places it over the whole screen no matter
    ///   where it is, so the bounds only need to be large enough for it to never be culled.
    /// </summary>
    private const float CloudQuadCullExtent = 1.0e7f;

    private readonly StringName planetCenterParameter = new("planetCenter");
    private readonly StringName sunDirectionParameter = new("sunDirection");
    private readonly StringName groundRadiusParameter = new("groundRadius");
    private readonly StringName topRadiusParameter = new("topRadius");
    private readonly StringName sunIlluminanceParameter = new("sunIlluminance");
    private readonly StringName sunAngularRadiusParameter = new("sunAngularRadius");
    private readonly StringName sunLimbDarkeningParameter = new("sunLimbDarkening");
    private readonly StringName rayleighScatteringParameter = new("rayleighScattering");
    private readonly StringName rayleighScaleHeightParameter = new("rayleighScaleHeight");
    private readonly StringName ozoneAbsorptionParameter = new("ozoneAbsorption");
    private readonly StringName ozoneLayerCenterParameter = new("ozoneLayerCenter");
    private readonly StringName ozoneLayerWidthParameter = new("ozoneLayerWidth");
    private readonly StringName viewRayStepsParameter = new("viewRaySteps");
    private readonly StringName lightRayStepsParameter = new("lightRaySteps");

    private readonly StringName cloudNoiseParameter = new("cloudNoise");
    private readonly StringName cloudPlanetCenterParameter = new("cloudPlanetCenter");
    private readonly StringName cloudInnerRadiusParameter = new("cloudInnerRadius");
    private readonly StringName cloudOuterRadiusParameter = new("cloudOuterRadius");
    private readonly StringName cloudTileSizeParameter = new("cloudTileSize");
    private readonly StringName cloudDensityMultiplierParameter = new("cloudDensityMultiplier");
    private readonly StringName cloudCoverageParameter = new("cloudCoverage");
    private readonly StringName cloudMaxDistanceParameter = new("cloudMaxDistance");
    private readonly StringName cloudSunDirectionParameter = new("cloudSunDirection");
    private readonly StringName cloudSunEnergyParameter = new("cloudSunEnergy");

#pragma warning disable CA2213
    private Sky sky = null!;
    private Compositor skyCompositor = null!;
    private Environment skyEnvironment = null!;

    private ShaderMaterial? cloudQuadMaterial;

    private DirectionalLight3D? sunLight;
#pragma warning restore CA2213

    public override void _Ready()
    {
        skyEnvironment = Environment ?? new Environment();

        skyEnvironment.TonemapMode = Environment.ToneMapper.Agx;
        skyEnvironment.TonemapAgxWhite = TonemapAgxWhite;

        sky = new Sky();

        sunLight = new DirectionalLight3D();
        AddChild(sunLight);

        bool useFullscreenQuad = ForceUseFullscreenQuad || !RenderingUtils.IsRenderingDeviceAvailable();

        SetupSky();

        // Compositor effects need a RenderingDevice, which only the Forward+ renderer provides.
        if (useFullscreenQuad)
        {
            SetupFallbackQuad();
        }
        else
        {
            SetupCompositorEffects();
        }

        Environment = skyEnvironment;
    }

    /// <summary>
    ///   Applies all the configured parameters to the sky shader and updates the VolumetricCloudsEffect dependencies.
    ///   Needs to be called again after changing <see cref="AtmosphereConfig"/> or when replacing
    ///   <see cref="SunConfig"/> with another instance for the change to have effect on the clouds. On the
    ///   Compatibility renderer this is also needed after changing the clouds config. Changes to the sun light only
    ///   take effect after calling this.
    /// </summary>
    public void ApplyParameters()
    {
        ApplyShaderParameters();
        ApplySunLightParameters();

        CloudsConfig.PlanetCenter = AtmosphereConfig.PlanetCenter;

        if (CloudsEffect is not null)
        {
            CloudsEffect.BindCloudsConfig(CloudsConfig);
            CloudsEffect.SunConfig = SunConfig;
        }

        ApplyCloudQuadParameters();
    }

    /// <summary>
    ///   Applies all the configured parameters to the sky shader. Needs to be called again after changing
    ///   <see cref="Composition"/> or <see cref="TraceGases"/> for the change to become visible.
    ///   For the other configuration changes, please call <see cref="ApplyParameters"/>
    /// </summary>
    public void ApplyShaderParameters()
    {
        if (SkyMaterial is null)
        {
            GD.PrintErr("Sky material is not set up yet, cannot apply the sky shader parameters");
            return;
        }

        AtmosphereConfig.ValidateOnce();
        Composition.ValidateOnce();
        TraceGases.ValidateOnce();
        SunConfig.ValidateOnce();

        SkyMaterial.SetShaderParameter(planetCenterParameter, AtmosphereConfig.PlanetCenter);
        SkyMaterial.SetShaderParameter(groundRadiusParameter, AtmosphereConfig.GroundRadius);
        SkyMaterial.SetShaderParameter(topRadiusParameter, AtmosphereConfig.TopRadius);

        SkyMaterial.SetShaderParameter(sunDirectionParameter, SunConfig.GetNormalizedDirection());
        SkyMaterial.SetShaderParameter(sunIlluminanceParameter, SunConfig.SunIlluminance);
        SkyMaterial.SetShaderParameter(sunAngularRadiusParameter, SunConfig.SunAngularRadius);
        SkyMaterial.SetShaderParameter(sunLimbDarkeningParameter, SunConfig.SunLimbDarkening);

        // Scattering and absorption parameters. These are computed on the CPU as they rarely change.
        SkyMaterial.SetShaderParameter(rayleighScatteringParameter, Composition.CalculateRayleighScattering());
        SkyMaterial.SetShaderParameter(rayleighScaleHeightParameter, AtmosphereConfig.RayleighScaleHeight);

        SkyMaterial.SetShaderParameter(ozoneAbsorptionParameter,
            TraceGases.CalculateOzoneAbsorption(Composition.MetresPerUnit));
        SkyMaterial.SetShaderParameter(ozoneLayerCenterParameter, TraceGases.OzoneLayerCenter);
        SkyMaterial.SetShaderParameter(ozoneLayerWidthParameter, TraceGases.OzoneLayerWidth);

        SkyMaterial.SetShaderParameter(viewRayStepsParameter, AtmosphereConfig.ViewRaySteps);
        SkyMaterial.SetShaderParameter(lightRayStepsParameter, AtmosphereConfig.LightRaySteps);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            planetCenterParameter.Dispose();
            sunDirectionParameter.Dispose();
            groundRadiusParameter.Dispose();
            topRadiusParameter.Dispose();
            sunIlluminanceParameter.Dispose();
            sunAngularRadiusParameter.Dispose();
            sunLimbDarkeningParameter.Dispose();
            rayleighScatteringParameter.Dispose();
            rayleighScaleHeightParameter.Dispose();
            ozoneAbsorptionParameter.Dispose();
            ozoneLayerCenterParameter.Dispose();
            ozoneLayerWidthParameter.Dispose();
            viewRayStepsParameter.Dispose();
            lightRayStepsParameter.Dispose();

            cloudNoiseParameter.Dispose();
            cloudPlanetCenterParameter.Dispose();
            cloudInnerRadiusParameter.Dispose();
            cloudOuterRadiusParameter.Dispose();
            cloudTileSizeParameter.Dispose();
            cloudDensityMultiplierParameter.Dispose();
            cloudCoverageParameter.Dispose();
            cloudMaxDistanceParameter.Dispose();
            cloudSunDirectionParameter.Dispose();
            cloudSunEnergyParameter.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Binds the sky shader to the sky material and makes the environment render that sky as its background.
    /// </summary>
    private void SetupSky()
    {
        SkyMaterial ??= new ShaderMaterial
        {
            Shader = GD.Load<Shader>(SkyShaderPath),
        };

        sky.SkyMaterial = SkyMaterial;

        skyEnvironment.BackgroundMode = Environment.BGMode.Sky;
        skyEnvironment.Sky = sky;

        ApplyParameters();
    }

    /// <summary>
    ///   Sets up the compute shader based cloud rendering used on the Forward+ renderer.
    /// </summary>
    private void SetupCompositorEffects()
    {
        skyCompositor = Compositor ?? new Compositor();

        CloudsEffect ??= new VolumetricCloudsEffect();

        CloudsEffect.SunConfig = SunConfig;
        CloudsEffect.BindCloudsConfig(CloudsConfig);

        var effects = new Array<CompositorEffect>([CloudsEffect]);

        skyCompositor.SetCompositorEffects(effects);

        Compositor = skyCompositor;
    }

    /// <summary>
    ///   Fallback cloud rendering for renderers without a RenderingDevice (Compatibility / OpenGL), which cannot run
    ///   the compute shaders the compositor effect relies on. A fullscreen quad is used instead.
    /// </summary>
    private void SetupFallbackQuad()
    {
        if (!ResourceLoader.Exists(VolumetricCloudsEffect.NoiseProfilePath))
        {
            GD.PrintErr("No cloud noise profile resource has been found, clouds will not be visible");
            return;
        }

        var builder = new ShaderBuilder();

        builder.AddModule("cloud_interface",
            VolumetricCloudsEffect.ShaderModuleDir + "clouds_quad_interface.gdshaderinc");

        VolumetricCloudsEffect.AddSharedCloudModules(builder);

        builder.AddModule("cloud_main", VolumetricCloudsEffect.ShaderModuleDir + "clouds_quad_main.gdshaderinc",
            "math", "cloud_march", "cloud_interface");

        cloudQuadMaterial = new ShaderMaterial
        {
            Shader = new Shader { Code = builder.Build("cloud_interface", "cloud_main") },
            RenderPriority = (int)Material.RenderPriorityMin,
        };

        cloudQuadMaterial.SetShaderParameter(cloudNoiseParameter,
            GD.Load<ImageTexture3D>(VolumetricCloudsEffect.NoiseProfilePath));

        var quad = new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = new Vector2(2.0f, 2.0f) },
            MaterialOverride = cloudQuadMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            CustomAabb = new Aabb(-Vector3.One * CloudQuadCullExtent, Vector3.One * (CloudQuadCullExtent * 2.0f)),
            IgnoreOcclusionCulling = true,
        };

        AddChild(quad);

        ApplyCloudQuadParameters();
    }

    private void ApplySunLightParameters()
    {
        if (sunLight is null)
            return;

        var direction = SunConfig.GetNormalizedDirection();
        var up = MathF.Abs(direction.Y) > 0.99f ? Vector3.Forward : Vector3.Up;

        sunLight.Basis = Basis.LookingAt(-direction, up);
        sunLight.LightEnergy = SunConfig.LightEnergy;
    }

    private void ApplyCloudQuadParameters()
    {
        if (cloudQuadMaterial is null)
            return;

        CloudsConfig.ValidateOnce();

        float inner = MathF.Max(CloudsConfig.CloudInnerHeight, 0.0f);
        float outer = MathF.Max(CloudsConfig.CloudOuterHeight, inner + 1.0f);

        cloudQuadMaterial.SetShaderParameter(cloudPlanetCenterParameter, CloudsConfig.PlanetCenter);
        cloudQuadMaterial.SetShaderParameter(cloudInnerRadiusParameter, CloudsConfig.PlanetRadius + inner);
        cloudQuadMaterial.SetShaderParameter(cloudOuterRadiusParameter, CloudsConfig.PlanetRadius + outer);
        cloudQuadMaterial.SetShaderParameter(cloudTileSizeParameter, CloudsConfig.CloudTileSize);
        cloudQuadMaterial.SetShaderParameter(cloudDensityMultiplierParameter, CloudsConfig.DensityMultiplier);
        cloudQuadMaterial.SetShaderParameter(cloudCoverageParameter, CloudsConfig.Coverage);
        cloudQuadMaterial.SetShaderParameter(cloudMaxDistanceParameter, CloudsConfig.MaxMarchDistance);
        cloudQuadMaterial.SetShaderParameter(cloudSunDirectionParameter, SunConfig.GetNormalizedDirection());
        cloudQuadMaterial.SetShaderParameter(cloudSunEnergyParameter, SunConfig.SunEnergy);
    }
}
