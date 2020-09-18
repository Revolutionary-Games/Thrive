using Godot;

/// <summary>
///   A chromatic aberration and barrel distortion filter effect
/// </summary>
public class ChromaticFilter : TextureRect
{
    private ShaderMaterial material;

    public override void _Ready()
    {
        material = (ShaderMaterial)Material;
        SetAmount(Settings.Instance.ChromaticAmount.Value);
        OnChanged(Settings.Instance.ChromaticEnabled.Value);

        Settings.Instance.ChromaticEnabled.OnChanged += OnChanged;
    }

    public override void _ExitTree()
    {
        Settings.Instance.ChromaticEnabled.OnChanged -= OnChanged;
    }

    public void OnChanged(bool enabled)
    {
        if (enabled)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    public void SetAmount(float amount)
    {
        material.SetShaderParam("MAX_DIST_PX", amount);
    }
}
