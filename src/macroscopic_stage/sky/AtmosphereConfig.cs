using System;
using Godot;

/// <summary>
///   Configuration container for the atmosphere of a sky. These are the parameters of the atmosphere sky shader that
///   aren't sun related, as those live in <see cref="SunConfig"/> instead.
/// </summary>
[GlobalClass]
public sealed partial class AtmosphereConfig : ValidatedConfig
{
    /// <summary>
    ///   The planet center. This is automatically propagated to <see cref="CloudsConfig.PlanetCenter"/> by the
    ///   <see cref="SkyEquippedEnvironment"/> during setup.
    /// </summary>
    [Export]
    public Vector3 PlanetCenter = Vector3.Zero;

    /// <summary>
    ///   Radius of the planet's surface. Rays pointing below the resulting horizon hit the ground.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Keeping this slightly below the actual planet radius prevents artifacts on the horizon from mesh
    ///     inaccuracies.
    ///   </para>
    /// </remarks>
    [Export]
    public float GroundRadius = 995.0f;

    /// <summary>
    ///   Radius at which the atmosphere ends. This must be greater than <see cref="GroundRadius"/>.
    /// </summary>
    [Export]
    public float TopRadius = 1050.0f;

    /// <summary>
    ///   Altitude in world units over which the air thins out by a factor of e. The atmosphere wants to be several
    ///   of these thick for the falloff to look natural.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,1000.0,0.1,or_greater")]
    public float RayleighScaleHeight = 20.0f;

    /// <summary>
    ///   How many steps the sky shader marches along the view ray. Higher is smoother but costs more, and the cost
    ///   multiplies with <see cref="LightRaySteps"/>.
    /// </summary>
    [Export(PropertyHint.Range, "4,128,1")]
    public int ViewRaySteps = 32;

    /// <summary>
    ///   How many steps the sky shader marches towards the sun from each view ray sample.
    /// </summary>
    [Export(PropertyHint.Range, "2,32,1")]
    public int LightRaySteps = 8;

    protected override int ValueCount => 5;

    public override bool Validate()
    {
        bool valid = true;

        valid &= Check(GroundRadius > 0.0f, $"GroundRadius must be positive, but is {GroundRadius}");

        valid &= Check(TopRadius > GroundRadius, $"TopRadius ({TopRadius}) must be greater than GroundRadius " +
            $"({GroundRadius}), otherwise the atmosphere has no thickness to scatter in");

        valid &= Check(RayleighScaleHeight > 0.0f,
            $"RayleighScaleHeight must be positive, but is {RayleighScaleHeight}");

        valid &= Check(ViewRaySteps is >= 4 and <= 128, $"ViewRaySteps must be between 4 and 128, but is " +
            $"{ViewRaySteps}");

        valid &= Check(LightRaySteps is >= 2 and <= 32, $"LightRaySteps must be between 2 and 32, but is " +
            $"{LightRaySteps}");

        if (TopRadius <= GroundRadius || RayleighScaleHeight <= 0.0f)
            return valid;

        float thickness = TopRadius - GroundRadius;

        valid &= Check(thickness >= RayleighScaleHeight, $"The atmosphere is only {thickness} thick while " +
            $"RayleighScaleHeight is {RayleighScaleHeight}, so the air is still dense where it ends");

        return valid;
    }

    protected override void CaptureValues(Span<float> destination)
    {
        destination[0] = GroundRadius;
        destination[1] = TopRadius;
        destination[2] = RayleighScaleHeight;
        destination[3] = ViewRaySteps;
        destination[4] = LightRaySteps;
    }
}
