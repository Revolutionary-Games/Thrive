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

    private bool highlighted;

    private TextureRect icon = null!;

    private ColorRect highlight = null!;

    public Vector2 BarSize
    {
        get => RectSize;
        set
        {
            RectMinSize = value;
            RectSize = value;

            highlight.RectSize = value;

            // Sets icon size
            icon.RectSize = new Vector2(value.y, value.y);

            // Changes icon visibility if bar is not wide enough
            icon.Visible = RectSize.x >= icon.RectSize.x;
        }
    }

    public Texture? IconTexture
    {
        get => icon.Texture;
        set => icon.Texture = value;
    }

    public Color IconModulation
    {
        get => icon.Modulate;
        set => icon.Modulate = value;
    }

    public Color HighlightColor
    {
        get => highlight.Modulate;
        set => highlight.Modulate = value;
    }

    public bool Highlight
    {
        get => highlighted;
        set
        {
            highlighted = value;
            highlight.Visible = value;
        }
    }

    public override void _Ready()
    {
        icon = GetChild<TextureRect>(0);
        highlight = GetChild<ColorRect>(1);
    }
}
