using Godot;

/// <summary>
///   Handles updating the <see cref="WorldEnvironment"/> for the microbe stage
/// </summary>
public partial class MicrobeWorldEnvironment : WorldEnvironment
{
    public override void _EnterTree()
    {
        Settings.Instance.BloomEnabled.OnChanged += OnBloomChanged;
        ApplyBloom();
    }

    public override void _ExitTree()
    {
        Settings.Instance.BloomEnabled.OnChanged -= OnBloomChanged;
    }

    private void OnBloomChanged(bool value)
    {
        ApplyBloom();
    }

    private void ApplyBloom()
    {
        Environment.GlowEnabled = Settings.Instance.BloomEnabled.Value;
    }
}
