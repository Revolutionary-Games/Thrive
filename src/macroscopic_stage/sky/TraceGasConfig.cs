using System;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
///   Trace gases that absorb light rather than scattering a meaningful amount of it.
/// </summary>
[GlobalClass]
public sealed partial class TraceGasConfig : ValidatedConfig
{
    /// <summary>
    ///   How much ozone there is compared to Earth. Zero removes the layer entirely.
    /// </summary>
    [Export(PropertyHint.Range, "0.0,10.0,0.01,or_greater")]
    public float OzoneConcentration = 1.0f;

    /// <summary>
    ///   Extinction of an Earth-like ozone layer at its peak, per metre, per colour channel.
    /// </summary>
    [Export]
    public Vector3 OzoneAbsorption = new(0.650e-6f, 1.881e-6f, 0.085e-6f);

    /// <summary>
    ///   Altitude in world units the ozone layer peaks at, measured from the ground.
    /// </summary>
    [Export(PropertyHint.Range, "0.0,1000.0,0.1,or_greater")]
    public float OzoneLayerCenter = 57.0f;

    /// <summary>
    ///   Full thickness of the ozone layer in world units. Density falls off linearly from the centre to nothing at
    ///   half this distance either side.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,1000.0,0.1,or_greater")]
    public float OzoneLayerWidth = 68.0f;

    protected override int ValueCount => 6;

    public override bool Validate()
    {
        bool valid = true;

        valid &= Check(OzoneConcentration >= 0.0f,
            $"OzoneConcentration cannot be negative, but is {OzoneConcentration}");

        valid &= Check(OzoneAbsorption.X >= 0.0f && OzoneAbsorption.Y >= 0.0f && OzoneAbsorption.Z >= 0.0f,
            $"OzoneAbsorption cannot have negative channels, as that would create light rather than absorb it, " +
            $"but is {OzoneAbsorption}");

        valid &= Check(OzoneLayerCenter >= 0.0f, $"OzoneLayerCenter is an altitude above the ground so it cannot " +
            $"be negative, but is {OzoneLayerCenter}");

        // The shader halves this to get the tent falloff, so a zero width would collapse the layer
        valid &= Check(OzoneLayerWidth > 0.0f, $"OzoneLayerWidth must be positive, but is {OzoneLayerWidth}");

        return valid;
    }

    /// <summary>
    ///   Calculates the peak ozone extinction per world unit, for each colour channel.
    /// </summary>
    /// <param name="metresPerUnit">
    ///   How many metres one world unit stands for, from <see cref="AtmosphereCompositionConfig.MetresPerUnit"/>.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 CalculateOzoneAbsorption(float metresPerUnit)
    {
        return OzoneAbsorption * (OzoneConcentration * metresPerUnit);
    }

    protected override void CaptureValues(Span<float> destination)
    {
        destination[0] = OzoneConcentration;
        destination[1] = OzoneAbsorption.X;
        destination[2] = OzoneAbsorption.Y;
        destination[3] = OzoneAbsorption.Z;
        destination[4] = OzoneLayerCenter;
        destination[5] = OzoneLayerWidth;
    }
}
