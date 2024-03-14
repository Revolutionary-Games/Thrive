using Godot;

public partial class ScreenFilter : ColorRect
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
            material!.Shader = null;
            return;
        }

        var effectShader = GD.Load<Shader>(currentEffect.ShaderPath);

        material!.Shader = effectShader;
    }
}
