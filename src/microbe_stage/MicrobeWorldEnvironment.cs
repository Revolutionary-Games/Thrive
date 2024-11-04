using Godot;

/// <summary>
///   Handles updating the <see cref="WorldEnvironment"/> for the microbe stage
/// </summary>
public partial class MicrobeWorldEnvironment : WorldEnvironment
{
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
