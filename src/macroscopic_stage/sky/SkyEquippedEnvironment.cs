using Godot;
using Godot.Collections;

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
    ///   Deliberately has no default value, as one gets created on demand below. An initializer here would be
    ///   thrown away when the scene value is applied, while still leaving a stray instance behind.
    /// </summary>
    [Export]
    public VolumetricCloudsEffect? CloudsEffect;
#pragma warning restore CA2213

    private const string SkyShaderPath = "res://shaders/sky/atmosphere_sky.gdshader";

    private const float TonemapAgxWhite = 6.0f;

    private readonly StringName planetCenterParameter = new("planet_center");
    private readonly StringName sunDirectionParameter = new("sun_direction");
    private readonly StringName groundRadiusParameter = new("ground_radius");
    private readonly StringName topRadiusParameter = new("top_radius");
    private readonly StringName sunIlluminanceParameter = new("sun_illuminance");
    private readonly StringName sunAngularRadiusParameter = new("sun_angular_radius");
    private readonly StringName sunLimbDarkeningParameter = new("sun_limb_darkening");
    private readonly StringName rayleighScatteringParameter = new("rayleigh_scattering");
    private readonly StringName rayleighScaleHeightParameter = new("rayleigh_scale_height");
    private readonly StringName ozoneAbsorptionParameter = new("ozone_absorption");
    private readonly StringName ozoneLayerCenterParameter = new("ozone_layer_center");
    private readonly StringName ozoneLayerWidthParameter = new("ozone_layer_width");
    private readonly StringName viewRayStepsParameter = new("view_ray_steps");
    private readonly StringName lightRayStepsParameter = new("light_ray_steps");

#pragma warning disable CA2213
    private readonly Sky sky = new();

    private Compositor skyCompositor = new();
    private Environment skyEnvironment = new();
#pragma warning restore CA2213

    public override void _Ready()
    {
        skyEnvironment.TonemapMode = Environment.ToneMapper.Agx;
        skyEnvironment.TonemapAgxWhite = TonemapAgxWhite;

        SetupSky();

        // Compositor effects need a RenderingDevice, which only the Forward+ renderer provides.
        if (RenderingUtils.IsRenderingDeviceAvailable())
        {
            SetupCompositorEffects();
        }
        else
        {
            SetupFallbackQuad();
        }

        Environment = skyEnvironment;
    }

    /// <summary>
    ///   Applies all of the configured parameters to the sky shader. Needs to be called again after changing
    ///   <see cref="AtmosphereConfig"/>, <see cref="Composition"/>, <see cref="TraceGases"/> or
    ///   <see cref="SunConfig"/> for the change to become visible.
    /// </summary>
    public void ApplyShaderParameters()
    {
        if (SkyMaterial is null)
        {
            GD.PrintErr("Sky material is not set up yet, cannot apply the sky shader parameters");
            return;
        }

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

        ApplyShaderParameters();
    }

    /// <summary>
    ///   Sets up the compute shader based cloud rendering used on the Forward+ renderer.
    /// </summary>
    private void SetupCompositorEffects()
    {
        CloudsEffect ??= new VolumetricCloudsEffect();

        CloudsEffect.SunConfig = SunConfig;

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
        // TODO: implement the fullscreen quad cloud fallback for the Compatibility renderer
        GD.PrintErr("Cloud rendering fallback quad is not implemented yet, clouds will not be visible");
    }
}
