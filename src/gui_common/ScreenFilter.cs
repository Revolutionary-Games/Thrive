using Godot;

public class ScreenFilter : ColorRect
{
    private ShaderMaterial? material;

    public override void _EnterTree()
    {
        base._EnterTree();

        material ??= (ShaderMaterial)Material;

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

        Shader effectShader = (Shader)GD.Load(currentEffect.ShaderPath);

        ((ShaderMaterial)Material).Shader = effectShader;
    }
}
