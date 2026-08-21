using Godot;

/// <summary>
///   Configuration container for the atmosphere of a sky. These are the parameters of the atmosphere sky shader that
///   aren't sun related, as those live in <see cref="SunConfig"/> instead.
/// </summary>
[GlobalClass]
public sealed partial class AtmosphereConfig : Resource
{
    [Export]
    public Vector3 PlanetCenter = Vector3.Zero;

    /// <summary>
    ///   Radius of the planet's surface. Rays pointing below the resulting horizon hit the ground.
    /// </summary>
    [Export]
    public float GroundRadius = 2000.0f;

    /// <summary>
    ///   Radius at which the atmosphere ends. This must be greater than <see cref="GroundRadius"/>.
    /// </summary>
    [Export]
    public float TopRadius = 2100.0f;

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
}
