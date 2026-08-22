using System;
using Godot;

/// <summary>
///   Configuration container for the sun lighting a sky.
/// </summary>
/// <remarks>
///   <para>
///     This is shared between the sky shader and the clouds effect, so that the sun disc drawn in the sky and the
///     lighting of the clouds always agree with each other.
///   </para>
/// </remarks>
[GlobalClass]
public sealed partial class SunConfig : ValidatedConfig
{
    /// <summary>
    ///   The sun direction.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Please note that this cannot be a zero vector. This gets normalised before being passed to the shaders, so
    ///     setting a zero-vector should be considered UB and an arbitrary unit-length vector will be used instead.
    ///     Use <see cref="GetNormalizedDirection"/> to get a direction that is always safe to pass on.
    ///   </para>
    /// </remarks>
    [Export]
    public Vector3 SunDirection = new Vector3(0.4f, 0.8f, 0.3f).Normalized();

    /// <summary>
    ///   How brightly the sun lights up the clouds.
    /// </summary>
    [Export(PropertyHint.Range, "0,100")]
    public float SunEnergy = 25.0f;

    /// <summary>
    ///   How brightly the sun disc itself is drawn in the sky.
    /// </summary>
    [Export(PropertyHint.Range, "0,100")]
    public float SunIlluminance = 25.0f;

    /// <summary>
    ///   Angular radius of the sun disc, in radians. The default is roughly that of Earth's sun.
    /// </summary>
    [Export(PropertyHint.Range, "0.0,0.1,0.0001")]
    public float SunAngularRadius = 0.00465f;

    /// <summary>
    ///   How much dimmer the edge of the sun disc is compared to its centre. 0 disables limb darkening entirely.
    /// </summary>
    [Export(PropertyHint.Range, "0.0,1.0")]
    public float SunLimbDarkening = 0.6f;

    protected override int ValueCount => 7;

    public override bool Validate()
    {
        bool valid = true;

        // A zero direction leaves the sun position undefined, and the shaders would silently fall back to an
        // arbitrary one
        valid &= Check(!SunDirection.IsZeroApprox(),
            "SunDirection must not be a zero vector, as that leaves the sun with no direction to shine from");

        valid &= Check(SunEnergy is >= 0.0f and <= 100.0f, $"SunEnergy must be between 0 and 100, but is " +
            $"{SunEnergy}");

        valid &= Check(SunIlluminance is >= 0.0f and <= 100.0f, $"SunIlluminance must be between 0 and 100, but " +
            $"is {SunIlluminance}");

        // The shader divides by the disc solid angle, which collapses as the radius reaches zero
        valid &= Check(SunAngularRadius is > 0.0f and <= 0.1f, $"SunAngularRadius must be greater than 0 and at " +
            $"most 0.1 radians, but is {SunAngularRadius}");

        valid &= Check(SunLimbDarkening is >= 0.0f and <= 1.0f, $"SunLimbDarkening must be between 0 and 1, but " +
            $"is {SunLimbDarkening}");

        return valid;
    }

    /// <summary>
    ///   Gets <see cref="SunDirection"/> as a unit-length vector that is always safe to hand to a shader.
    /// </summary>
    public Vector3 GetNormalizedDirection()
    {
        // Prevent singularities and erratic behaviour in the shaders by returning a non-zero vector.
        // Vector3.One.Normalized() is purely arbitrary (as we can choose any unit-length vector).
        return SunDirection.IsZeroApprox() ? Vector3.One.Normalized() : SunDirection.Normalized();
    }

    protected override void CaptureValues(Span<float> destination)
    {
        destination[0] = SunDirection.X;
        destination[1] = SunDirection.Y;
        destination[2] = SunDirection.Z;
        destination[3] = SunEnergy;
        destination[4] = SunIlluminance;
        destination[5] = SunAngularRadius;
        destination[6] = SunLimbDarkening;
    }
}
