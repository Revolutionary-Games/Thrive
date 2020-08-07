using Godot;

/// <summary>
///   Handles what colourblind filter is used
/// </summary>
public class ColourblindScreenFilter : TextureRect
{
    private static ColourblindScreenFilter instance;
    private ShaderMaterial screenFilterMaterial;

    private ColourblindScreenFilter()
    {
        instance = this;
    }

    public static ColourblindScreenFilter Instance => instance;

    public override void _Ready()
    {
        screenFilterMaterial = (ShaderMaterial)Material;
        Material = null;
        Hide();
    }

    public void SetColourblindSetting(int index)
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
                GD.PrintErr("Invalid Colourblind Setting");
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
