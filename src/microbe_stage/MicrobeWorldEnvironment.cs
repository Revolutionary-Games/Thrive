using Godot;

/// <summary>
///   Handles updating the <see cref="WorldEnvironment"/> for the microbe stage
/// </summary>
public partial class MicrobeWorldEnvironment : WorldEnvironment
{
    private static StringName skyColorShaderParameter = new("colour");

    public override void _EnterTree()
    {
        Settings.Instance.BloomEnabled.OnChanged += OnBloomChanged;
        Settings.Instance.BloomStrength.OnChanged += OnStrengthChanged;
        ApplyBloom();
    }

    public override void _ExitTree()
    {
        Settings.Instance.BloomEnabled.OnChanged -= OnBloomChanged;
        Settings.Instance.BloomStrength.OnChanged -= OnStrengthChanged;
    }

    public void UpdateAmbientReflection(Color colour)
    {
        ((ShaderMaterial)Environment.Sky.SkyMaterial).SetShaderParameter(skyColorShaderParameter, colour);
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
