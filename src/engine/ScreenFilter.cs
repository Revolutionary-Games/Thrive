using Godot;

/// <summary>
///   Handles what colorblind filter is used
/// </summary>
public class ScreenFilter : TextureRect
{
    private static ScreenFilter instance;
    private ShaderMaterial screenFilterMaterial;
    public ScreenFilter()
    {
        instance = this;
    }

    public static ScreenFilter Instance
    {
        get
        {
            return instance;
        }
    }

    public override void _Ready()
    {
        screenFilterMaterial = (ShaderMaterial)Material;
        Material = null;
        Hide();
    }

    public void SetColorblindSetting(int index)
    {
        switch (index)
        {
            case 0:
                SetNormal();
                break;
            case 1:
                SetRedGreen();
                break;
            case 2:
                SetBlueYellow();
                break;
            default:
                GD.PrintErr("Invalid Colorblind Setting");
                break;
        }
    }

    private void SetNormal()
    {
        Material = null;
        Hide();
    }

    private void SetRedGreen()
    {
        Material = screenFilterMaterial;
        screenFilterMaterial.SetShaderParam("mode", 1);
        Show();
    }

    private void SetBlueYellow()
    {
        Material = screenFilterMaterial;
        screenFilterMaterial.SetShaderParam("mode", 2);
        Show();
    }
}
