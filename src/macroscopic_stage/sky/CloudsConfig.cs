using Godot;

/// <summary>
///   Configuration container for the clouds in a sky. The sun parameters the clouds are lit with live in
///   <see cref="SunConfig"/> instead, as the sky shader needs them as well.
/// </summary>
[GlobalClass]
public sealed partial class CloudsConfig : Resource
{
    [Export]
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

    [Export]
    public bool ProfileGpu;
}
