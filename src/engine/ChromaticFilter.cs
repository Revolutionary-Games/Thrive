using Godot;

/// <summary>
/// A chromatic aberration and barrel distrortion filter effect
/// </summery>
public class ChromaticFilter : TextureRect
{
    private static ChromaticFilter instance;
    ShaderMaterial material;

    public static ChromaticFilter Instance => instance;
    public override void _Ready()
    {
        material = (ShaderMaterial) Material;
        SetAmount(Settings.Instance.ChromaticAmount);
        ToggleEffect(Settings.Instance.ChromaticEnabled);
    }

    public void ToggleEffect(bool enabled)
    {
        if(enabled)
        {
            Show();
        } else
        {
            Hide();
        }
    }

    public void SetAmount(float amount)
    {
        material.SetShaderParam("MAX_DIST_PX", amount);
    }
}
