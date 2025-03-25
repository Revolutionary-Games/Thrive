using Godot;

/// <summary>
///   Displays the optimal tolerance value marker
/// </summary>
public partial class ToleranceOptimalMarker : Control
{
#pragma warning disable CA2213
    [Export]
    private Texture2D? markerTextureOverride;

    [Export]
    private TextureRect optimalValueMarker = null!;
#pragma warning restore CA2213

    private bool showMarker = true;

    private float optimalValue;

    public bool ShowMarker
    {
        get => showMarker;
        set
        {
            if (value == showMarker)
                return;

            showMarker = value;

            optimalValueMarker.Visible = value;
        }
    }

    /// <summary>
    ///   The optimal tolerance value as a fraction (0-1) between the min and max values.
    /// </summary>
    public float OptimalValue
    {
        get => optimalValue;
        set
        {
            optimalValue = value;

            UpdateMarker();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (markerTextureOverride != null)
            optimalValueMarker.Texture = markerTextureOverride;
    }

    /// <summary>
    ///   Updates the marker's position, taking into account any rect changes
    /// </summary>
    public void UpdateMarker()
    {
        optimalValueMarker.Position = new Vector2((Size.X - optimalValueMarker.Size.X + 0.5f) * optimalValue - 0.5f,
            optimalValueMarker.Position.Y);
    }
}
