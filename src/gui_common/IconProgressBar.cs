using Godot;

/// <summary>
///   A progress bar that supports showing an icon.
/// </summary>
public class IconProgressBar : ColorRect
{
    /// <summary>
    ///   Special property used by SegmentedBar
    /// </summary>
    public bool Disabled;

    private TextureRect icon;

    public Vector2 BarSize
    {
        get => RectSize;
        set
        {
            RectSize = value;
            RectMinSize = value;

            // Sets icon size
            icon.RectSize = new Vector2(value.y, value.y);

            // Changes icon visibility if bar is not wide enough
            icon.Visible = RectSize.x >= icon.RectSize.x;
        }
    }

    public Texture IconTexture
    {
        get => icon.Texture;
        set => icon.Texture = value;
    }

    public Color IconModulation
    {
        get => icon.Modulate;
        set => icon.Modulate = value;
    }

    public override void _Ready()
    {
        icon = GetChild<TextureRect>(0);
    }
}
