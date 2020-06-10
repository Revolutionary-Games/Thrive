using Godot;

public class IconProgressBar : ColorRect
{
    public bool Disabled;
    public int Location;
    public int ActualLocation;

    public void SetBarName(string name)
    {
        Name = name;
    }

    public void SetBarSize(Vector2 size)
    {
        RectSize = size;
        RectMinSize = size;

        // Sets icon size
        GetChild<TextureRect>(0).RectSize = new Vector2(size.y, size.y);

        // Changes icon visibility if bar is not wide enough
        GetChild<TextureRect>(0).Visible = RectSize.x >= GetChild<TextureRect>(0).RectSize.x;
    }

    public void SetBarIconTexture(Texture texture)
    {
        GetChild<TextureRect>(0).Texture = texture;
    }

    public void SetBarIconModulation(Color colour)
    {
        GetChild<TextureRect>(0).Modulate = colour;
    }
}
