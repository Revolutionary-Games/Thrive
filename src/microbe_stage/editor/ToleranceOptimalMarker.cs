using Godot;

/// <summary>
///   Displays additional tolerance slider info, such as the optimal value and value labels.
/// </summary>
public partial class ToleranceOptimalMarker : Control
{
#pragma warning disable CA2213
    [Export]
    [ExportCategory("Visuals")]
    private Texture2D? markerTextureOverride;

    [Export]
    private float padding = 8.0f;

    [Export]
    [ExportCategory("Internal")]
    private Label startValue = null!;

    [Export]
    private Label endValue = null!;

    [Export]
    private TextureRect optimalValueMarker = null!;
#pragma warning restore CA2213

    private bool showMarker = true;

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

    public override void _Ready()
    {
        base._Ready();

        if (markerTextureOverride != null)
            optimalValueMarker.Texture = markerTextureOverride;
    }

    public void UpdateBoundaryLabels(string start, string end)
    {
        startValue.Text = start;
        endValue.Text = end;
    }

    public void UpdateMarker(float optimalValue)
    {
        optimalValueMarker.OffsetLeft = padding + (Size.X - 2.0f * padding) * optimalValue
            - optimalValueMarker.Size.X * 0.5f;
    }
}
