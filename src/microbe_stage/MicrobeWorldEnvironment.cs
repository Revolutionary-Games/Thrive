using Godot;

/// <summary>
///   Handles updating the <see cref="WorldEnvironment"/> for the microbe stage
/// </summary>
public partial class MicrobeWorldEnvironment : WorldEnvironment
{
    private readonly StringName skyColorShaderParameter = new("colour");

#pragma warning disable CA2213
    private ShaderMaterial skyMaterial = null!;
#pragma warning restore CA2213

    private Color lastSkyColour;

    public override void _EnterTree()
    {
        Settings.Instance.BloomEnabled.OnChanged += OnBloomChanged;
        Settings.Instance.BloomStrength.OnChanged += OnStrengthChanged;
        ApplyBloom();

        skyMaterial = (ShaderMaterial)Environment.Sky.SkyMaterial;
    }

    public override void _ExitTree()
    {
        Settings.Instance.BloomEnabled.OnChanged -= OnBloomChanged;
        Settings.Instance.BloomStrength.OnChanged -= OnStrengthChanged;
    }

    public void UpdateAmbientReflection(Color colour)
    {
        if (colour == lastSkyColour)
            return;

        lastSkyColour = colour;
        skyMaterial.SetShaderParameter(skyColorShaderParameter, colour);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            skyColorShaderParameter.Dispose();
        }
    }

    private void OnBloomChanged(bool value)
    {
        ApplyBloom();
    }

    private void OnStrengthChanged(float value)
    {
        ApplyBloom();
    }

    private void ApplyBloom()
    {
        Environment.GlowEnabled = Settings.Instance.BloomEnabled.Value;
        Environment.GlowStrength = Settings.Instance.BloomStrength.Value;
    }
}
