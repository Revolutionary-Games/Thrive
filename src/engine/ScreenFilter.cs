using Godot;

/// <summary>
///   Handles what colorblind filter is show
///   Turns off filter if set to "normal_color"
/// </summary>
public class ScreenFilter : TextureRect
{
    private ShaderMaterial screenFilterMaterial;
    public override void _Ready()
    {
        screenFilterMaterial = (ShaderMaterial)Material;
        Material = null;
        Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("normal_color"))
        {
            Material = null;
            Hide();
        }

        if (@event.IsActionPressed("red_green"))
        {
            Material = screenFilterMaterial;
            screenFilterMaterial.SetShaderParam("mode", 1);
            Show();
        }

        if (@event.IsActionPressed("blue_yellow"))
        {
            Material = screenFilterMaterial;
            screenFilterMaterial.SetShaderParam("mode", 2);
            Show();
        }
    }
}
