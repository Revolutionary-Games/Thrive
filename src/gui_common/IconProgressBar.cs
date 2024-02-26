using Godot;

/// <summary>
///   A progress bar that supports showing an icon.
/// </summary>
public partial class IconProgressBar : ColorRect
{
    /// <summary>
    ///   Special property used by SegmentedBar
    /// </summary>
    public bool Disabled;

    private bool highlighted;

#pragma warning disable CA2213
    private TextureRect icon = null!;

    private ColorRect highlight = null!;
#pragma warning restore CA2213

    public Vector2 BarSize
    {
        get => Size;
        set
        {
            CustomMinimumSize = value;
            Size = value;

            highlight.Size = value;

            // Sets icon size
            icon.Size = new Vector2(value.Y, value.Y);

            // Changes icon visibility if bar is not wide enough
            icon.Visible = Size.X >= icon.Size.X;
        }
    }

    public Texture2D? IconTexture
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
