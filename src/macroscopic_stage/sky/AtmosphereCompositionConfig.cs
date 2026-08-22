using System;
using Godot;

/// <summary>
///   Average global composition of a planet's atmosphere, as the fractions of the gases making it up.
/// </summary>
/// <remarks>
///   <para>
///     Fractions are by volume (equivalently, mole fractions) and are normalised before use, so they don't have to
///     add up to exactly one. Absorbing trace gases like ozone are not here, those are in
///     <see cref="TraceGasConfig"/>, as they contribute absorption rather than scattering.
///   </para>
/// </remarks>
[GlobalClass]
public sealed partial class AtmosphereCompositionConfig : ValidatedConfig
{
    [Export(PropertyHint.Range, "0.0,1.0")]
    public float Nitrogen = 0.7808f;

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float Oxygen = 0.2095f;

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float Argon = 0.0093f;

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float CarbonDioxide = 0.0004f;

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float Methane;

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float Hydrogen;

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float Helium;

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float WaterVapour;

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float Ammonia;

    [Export(PropertyHint.Range, "0.0,1.0")]
    public float SulfurDioxide;

    /// <summary>
    ///   Pressure at the ground in kPa. Earth is 101.325, Mars is around 0.6 and Venus around 9200.
    /// </summary>
    [Export(PropertyHint.Range, "0.0,10000.0,0.001,or_greater")]
    public float SurfacePressure = 101.325f;

    /// <summary>
    ///   Temperature at the ground in Kelvin.
    /// </summary>
    [Export(PropertyHint.Range, "1.0,2000.0,0.1,or_greater")]
    public float SurfaceTemperature = 288.15f;

    /// <summary>
    ///   Wavelengths in nanometres that the red, green and blue channels are taken to represent.
    /// </summary>
    [Export(PropertyHint.Range, "100.0,2000.0,0.1,or_greater")]
    public Vector3 Wavelengths = new(680.0f, 550.0f, 440.0f);

    /// <summary>
    ///   How many metres one world unit stands for.
    /// </summary>
    [Export(PropertyHint.Range, "1.0,10000.0,1,or_greater")]
    public float MetresPerUnit = 440.0f;

    private const double BoltzmannConstant = 1.380649e-23;
    private const double LoschmidtConstant = 2.6867811e25;

    // Refractivity (n - 1) at standard temperature and pressure, and the King correction factor accounting for how
    // non-spherical the molecule is.
    private const double NitrogenRefractivity = 2.9839e-4;
    private const double NitrogenKingFactor = 1.034;
    private const double OxygenRefractivity = 2.7100e-4;
    private const double OxygenKingFactor = 1.096;
    private const double ArgonRefractivity = 2.8100e-4;
    private const double ArgonKingFactor = 1.0;
    private const double CarbonDioxideRefractivity = 4.4900e-4;
    private const double CarbonDioxideKingFactor = 1.150;
    private const double MethaneRefractivity = 4.4100e-4;
    private const double MethaneKingFactor = 1.0;
    private const double HydrogenRefractivity = 1.3800e-4;
    private const double HydrogenKingFactor = 1.020;
    private const double HeliumRefractivity = 3.4800e-5;
    private const double HeliumKingFactor = 1.0;
    private const double WaterVapourRefractivity = 2.5400e-4;
    private const double WaterVapourKingFactor = 1.001;
    private const double AmmoniaRefractivity = 3.7600e-4;
    private const double AmmoniaKingFactor = 1.0;
    private const double SulfurDioxideRefractivity = 6.8600e-4;
    private const double SulfurDioxideKingFactor = 1.0;

    protected override int ValueCount => 16;

    public override bool Validate()
    {
        bool valid = true;

        valid &= CheckFraction(Nitrogen, "Nitrogen");
        valid &= CheckFraction(Oxygen, "Oxygen");
        valid &= CheckFraction(Argon, "Argon");
        valid &= CheckFraction(CarbonDioxide, "CarbonDioxide");
        valid &= CheckFraction(Methane, "Methane");
        valid &= CheckFraction(Hydrogen, "Hydrogen");
        valid &= CheckFraction(Helium, "Helium");
        valid &= CheckFraction(WaterVapour, "WaterVapour");
        valid &= CheckFraction(Ammonia, "Ammonia");
        valid &= CheckFraction(SulfurDioxide, "SulfurDioxide");

        float totalFraction = Nitrogen + Oxygen + Argon + CarbonDioxide + Methane + Hydrogen + Helium + WaterVapour +
            Ammonia + SulfurDioxide;

        valid &= Check(totalFraction > 0.0f,
            "The gas fractions add up to zero, leaving nothing for the sky to scatter off of");

        valid &= Check(SurfacePressure >= 0.0f, $"SurfacePressure cannot be negative, but is {SurfacePressure}");

        valid &= Check(SurfaceTemperature > 0.0f,
            $"SurfaceTemperature must be above absolute zero, but is {SurfaceTemperature}");

        valid &= Check(Wavelengths is { X: > 0.0f, Y: > 0.0f, Z: > 0.0f },
            $"Wavelengths must all be positive, but are {Wavelengths}");

        valid &= Check(MetresPerUnit > 0.0f, $"MetresPerUnit must be positive, but is {MetresPerUnit}");

        return valid;
    }

    /// <summary>
    ///   Calculates the Rayleigh scattering coefficient at the ground for each colour channel, per world unit.
    /// </summary>
    /// <returns>
    ///   Per-channel scattering coefficients, or a zero vector if the composition is empty.
    /// </returns>
    public Vector3 CalculateRayleighScattering()
    {
        float totalFraction = Nitrogen + Oxygen + Argon + CarbonDioxide + Methane + Hydrogen + Helium + WaterVapour +
            Ammonia + SulfurDioxide;

        if (totalFraction <= 0.0f)
        {
            GD.PrintErr("Atmosphere composition is empty, there is nothing for the sky to scatter off of");
            return Vector3.Zero;
        }

        // Ideal gas law, giving how many molecules per cubic metre there are at the ground
        double numberDensity = SurfacePressure * 1000.0 / (BoltzmannConstant * SurfaceTemperature);

        // Normalising by the total fraction is folded in here to keep it out of the per-gas sum
        double scale = numberDensity * MetresPerUnit / totalFraction;

        return new Vector3((float)(MixtureCrossSection(Wavelengths.X) * scale),
            (float)(MixtureCrossSection(Wavelengths.Y) * scale),
            (float)(MixtureCrossSection(Wavelengths.Z) * scale));
    }

    protected override void CaptureValues(Span<float> destination)
    {
        destination[0] = Nitrogen;
        destination[1] = Oxygen;
        destination[2] = Argon;
        destination[3] = CarbonDioxide;
        destination[4] = Methane;
        destination[5] = Hydrogen;
        destination[6] = Helium;
        destination[7] = WaterVapour;
        destination[8] = Ammonia;
        destination[9] = SulfurDioxide;
        destination[10] = SurfacePressure;
        destination[11] = SurfaceTemperature;
        destination[12] = Wavelengths.X;
        destination[13] = Wavelengths.Y;
        destination[14] = Wavelengths.Z;
        destination[15] = MetresPerUnit;
    }

    /// <summary>
    ///   Rayleigh scattering cross-section of a single gas at a given wavelength.
    /// </summary>
    private static double GasCrossSection(double refractivity, double kingFactor, double wavelengthNanometres)
    {
        double refractiveIndex = 1.0 + refractivity;
        double refractiveIndexSquared = refractiveIndex * refractiveIndex;

        double term = (refractiveIndexSquared - 1.0) / (refractiveIndexSquared + 2.0);

        double wavelength = wavelengthNanometres * 1e-9;
        double wavelengthFourth = wavelength * wavelength * wavelength * wavelength;

        return 24.0 * Math.PI * Math.PI * Math.PI * term * term * kingFactor /
            (wavelengthFourth * LoschmidtConstant * LoschmidtConstant);
    }

    private bool CheckFraction(float fraction, string name)
    {
        return Check(fraction is >= 0.0f and <= 1.0f,
            $"{name} is a fraction of the whole atmosphere so it must be between 0 and 1, but is {fraction}");
    }

    private double MixtureCrossSection(float wavelengthNanometres)
    {
        double result = 0.0;

        result += Nitrogen * GasCrossSection(NitrogenRefractivity, NitrogenKingFactor, wavelengthNanometres);
        result += Oxygen * GasCrossSection(OxygenRefractivity, OxygenKingFactor, wavelengthNanometres);
        result += Argon * GasCrossSection(ArgonRefractivity, ArgonKingFactor, wavelengthNanometres);
        result += CarbonDioxide *
            GasCrossSection(CarbonDioxideRefractivity, CarbonDioxideKingFactor, wavelengthNanometres);
        result += Methane * GasCrossSection(MethaneRefractivity, MethaneKingFactor, wavelengthNanometres);
        result += Hydrogen * GasCrossSection(HydrogenRefractivity, HydrogenKingFactor, wavelengthNanometres);
        result += Helium * GasCrossSection(HeliumRefractivity, HeliumKingFactor, wavelengthNanometres);
        result += WaterVapour * GasCrossSection(WaterVapourRefractivity, WaterVapourKingFactor, wavelengthNanometres);
        result += Ammonia * GasCrossSection(AmmoniaRefractivity, AmmoniaKingFactor, wavelengthNanometres);
        result += SulfurDioxide *
            GasCrossSection(SulfurDioxideRefractivity, SulfurDioxideKingFactor, wavelengthNanometres);

        return result;
    }
}
