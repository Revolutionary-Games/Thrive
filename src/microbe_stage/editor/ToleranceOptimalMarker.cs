using Godot;

/// <summary>
///   Displays additional tolerance slider info, such as the optimal value and value labels.
/// </summary>
public partial class ToleranceOptimalMarker : Control
{
#pragma warning disable CA2213
    [Export]
    private Label startValue = null!;

    [Export]
    private Label endValue = null!;

    [Export]
    private TextureRect optimalValueMarker = null!;

    [Export]
    private Texture2D markerTexture = null!;
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

        optimalValueMarker.Texture = markerTexture;
    }

    public void UpdateBoundaryLabels(string start, string end)
    {
        startValue.Text = start;
        endValue.Text = end;
    }

    public void UpdateMarker(float optimalValue)
    {
        optimalValueMarker.AnchorLeft = optimalValue;
        optimalValueMarker.AnchorRight = optimalValue;
    }
}
