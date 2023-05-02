using Godot;

public class ScreenFilter : ColorRect
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
        if (currentEffect?.ShaderPath == null)
        {
            ((ShaderMaterial)Material).Shader = null;
            return;
        }

        var effectShader = GD.Load<Shader>(currentEffect.ShaderPath);

        ((ShaderMaterial)Material).Shader = effectShader;
    }
}
