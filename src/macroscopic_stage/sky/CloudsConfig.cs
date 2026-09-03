using System;
using Godot;

/// <summary>
///   Configuration container for the clouds in a sky. The sun parameters the clouds are lit with live in
///   <see cref="SunConfig"/> instead, as the sky shader needs them as well.
/// </summary>
[GlobalClass]
public sealed partial class CloudsConfig : ValidatedConfig
{
    /// <summary>
    ///   The planet center. This is set in AtmosphereConfig.
    /// </summary>
    public Vector3 PlanetCenter = Vector3.Zero;

    [Export]
    public float PlanetRadius = 1000.0f;

    [Export]
    public float CloudInnerHeight = 10.0f;

    [Export]
    public float CloudOuterHeight = 50.0f;

    [Export]
    public int Seed = 1234;

    [Export(PropertyHint.Range, "1,1000,1,or_greater")]
    public float CloudTileSize = 200.0f;

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float DensityMultiplier = 1.0f;

    [Export(PropertyHint.Range, "0.1,1.0")]
    public float Coverage = 0.3f;

    [Export(PropertyHint.Range, "1,256")]
    public int MarchSteps = 64;

    [Export(PropertyHint.Range, "1,10")]
    public int LightSteps = 6;

    [Export(PropertyHint.Range, "0,10000,1,or_greater")]
    public float MaxMarchDistance = 8000.0f;

    [Export(PropertyHint.Range, "1,4,1")]
    public int ResolutionDivisor = 2;

    protected override int ValueCount => 10;

    public override bool Validate()
    {
        bool valid = true;

        valid &= Check(PlanetRadius > 0.0f, $"PlanetRadius must be positive, but is {PlanetRadius}");

        valid &= Check(CloudInnerHeight >= 0.0f, $"CloudInnerHeight is an altitude above the ground so it cannot " +
            $"be negative, but is {CloudInnerHeight}");

        // The ray marcher derives its shell from these two, and an inverted or empty shell leaves it nothing to
        // march through
        valid &= Check(CloudOuterHeight > CloudInnerHeight, $"CloudOuterHeight ({CloudOuterHeight}) must be " +
            $"greater than CloudInnerHeight ({CloudInnerHeight}), otherwise the cloud layer has no thickness");

        valid &= Check(CloudTileSize > 0.0f, $"CloudTileSize must be positive, but is {CloudTileSize}");

        valid &= Check(DensityMultiplier is >= 0.0f and <= 1.0f, $"DensityMultiplier must be between 0 and 1, but " +
            $"is {DensityMultiplier}");

        valid &= Check(Coverage is >= 0.1f and <= 1.0f, $"Coverage must be between 0.1 and 1, but is {Coverage}");

        valid &= Check(MarchSteps is >= 1 and <= 256, $"MarchSteps must be between 1 and 256, but is {MarchSteps}");

        valid &= Check(LightSteps is >= 1 and <= 10, $"LightSteps must be between 1 and 10, but is {LightSteps}");

        valid &= Check(MaxMarchDistance > 0.0f, $"MaxMarchDistance must be positive, but is {MaxMarchDistance}");

        valid &= Check(ResolutionDivisor is >= 1 and <= 4, $"ResolutionDivisor must be between 1 and 4, but is " +
            $"{ResolutionDivisor}");

        return valid;
    }

    protected override void CaptureValues(Span<float> destination)
    {
        destination[0] = PlanetRadius;
        destination[1] = CloudInnerHeight;
        destination[2] = CloudOuterHeight;
        destination[3] = CloudTileSize;
        destination[4] = DensityMultiplier;
        destination[5] = Coverage;
        destination[6] = MarchSteps;
        destination[7] = LightSteps;
        destination[8] = MaxMarchDistance;
        destination[9] = ResolutionDivisor;
    }
}
