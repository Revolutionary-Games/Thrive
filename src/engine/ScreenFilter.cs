using Godot;

public class ScreenFilter : TextureRect
{
    private TextureRect screenFilter;
    private ShaderMaterial screenFilterMaterial;
    public override void _Ready()
    {
        screenFilter = (TextureRect)this;
        screenFilterMaterial = (ShaderMaterial)screenFilter.Material;
        screenFilter.Material = null;
        screenFilter.Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("normal_color"))
        {
            screenFilter.Material = null;
            screenFilter.Hide();
        }

        if (@event.IsActionPressed("red_green"))
        {
            screenFilter.Material = screenFilterMaterial;
            screenFilterMaterial.SetShaderParam("mode", 1);
            screenFilter.Show();
        }

        if (@event.IsActionPressed("blue_yellow"))
        {
            screenFilter.Material = screenFilterMaterial;
            screenFilterMaterial.SetShaderParam("mode", 2);
            screenFilter.Show();
        }
    }
}
