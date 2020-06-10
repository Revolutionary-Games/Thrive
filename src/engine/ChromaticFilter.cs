using Godot;

/// <summary>
/// A chromatic aberration and barrel distrortion filter effect
/// </summery>
public class ChromaticFilter : TextureRect
{
    ShaderMaterial material;
    const float defaultAmount = 20f;
    public override void _Ready()
    {
        material = (ShaderMaterial) Material;
        SetAmount(defaultAmount);
        Show();
    }

    public void ToggleEffect()
    {
        if(Visible)
        {
            Hide();
        } else
        {
            Show();
        }
    }

    public void SetAmount(float amount)
    {
        material.SetShaderParam("MAX_DIST_PX", amount);
    }
}
