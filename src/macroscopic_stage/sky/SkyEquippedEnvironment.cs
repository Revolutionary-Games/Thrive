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

#pragma warning disable CA2213
    private Sky sky = null!;
    private Compositor skyCompositor = null!;
    private Environment skyEnvironment = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        skyEnvironment = Environment ?? new Environment();

        skyEnvironment.TonemapMode = Environment.ToneMapper.Agx;
        skyEnvironment.TonemapAgxWhite = TonemapAgxWhite;

        sky = new Sky();

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
    ///   Applies all the configured parameters to the sky shader and updates the VolumetricCloudsEffect dependencies.
    ///   Needs to be called again after changing <see cref="AtmosphereConfig"/> or when replacing
    ///   <see cref="SunConfig"/> with another instance for the change to have effect on the clouds.
    /// </summary>
    public void ApplyParameters()
    {
        ApplyShaderParameters();

        if (CloudsEffect is null)
            return;

        CloudsEffect.CloudsConfig.PlanetCenter = AtmosphereConfig.PlanetCenter;
        CloudsEffect.SunConfig = SunConfig;
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
        CloudsEffect.CloudsConfig.PlanetCenter = AtmosphereConfig.PlanetCenter;

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
