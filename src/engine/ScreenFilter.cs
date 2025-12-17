using Godot;

/// <summary>
///   Provides fun fullscreen filters
/// </summary>
public partial class ScreenFilter : ColorRect
{
    public override void _EnterTree()
    {
        base._EnterTree();

        UpdateEffect(Settings.Instance.CurrentScreenEffect);

        Settings.Instance.CurrentScreenEffect.OnChanged += UpdateEffect;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Settings.Instance.CurrentScreenEffect.OnChanged -= UpdateEffect;
    }

    public void UpdateEffect(ScreenEffect? currentEffect)
    {
        // Free any previous material
        Visible = false;
        Material = null;

        if (currentEffect?.ShaderPath == null)
        {
            return;
        }

        GD.Print("Applying shader filter effect: ", currentEffect.ShaderPath);

        var effectShader = GD.Load<Shader>(currentEffect.ShaderPath);

        Material = new ShaderMaterial
        {
            Shader = effectShader,
        };

        Visible = true;
    }
}
