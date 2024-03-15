using Godot;

/// <summary>
///   A chromatic aberration and barrel distortion filter effect
/// </summary>
public partial class ChromaticFilter : TextureRect
{
#pragma warning disable CA2213
    private ShaderMaterial? material;
#pragma warning restore CA2213

    public override void _EnterTree()
    {
        base._EnterTree();

        material ??= (ShaderMaterial)Material;

        SetAmount(Settings.Instance.ChromaticAmount);
        OnChanged(Settings.Instance.ChromaticEnabled);

        Settings.Instance.ChromaticAmount.OnChanged += SetAmount;
        Settings.Instance.ChromaticEnabled.OnChanged += OnChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Settings.Instance.ChromaticAmount.OnChanged -= SetAmount;
        Settings.Instance.ChromaticEnabled.OnChanged -= OnChanged;
    }

    private void OnChanged(bool enabled)
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

    private void SetAmount(float amount)
    {
        material!.SetShaderParameter("MAX_DIST_PX", amount);
    }
}
