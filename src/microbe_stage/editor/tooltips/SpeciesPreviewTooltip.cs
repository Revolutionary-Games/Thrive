using Godot;

/// <summary>
///   Tooltip that shows a species preview image
/// </summary>
public partial class SpeciesPreviewTooltip : PanelContainer, ICustomToolTip
{
    [Export]
    public NodePath? SpeciesPreviewPath;

    [Export]
    public NodePath HexPreviewPath = null!;

#pragma warning disable CA2213
    private SpeciesPreview? speciesPreview;
    private CellHexesPreview? hexesPreview;
#pragma warning restore CA2213

    private Species? previewSpecies;

    public Species? PreviewSpecies
    {
        get => previewSpecies;
        set
        {
            if (previewSpecies == value)
                return;

            previewSpecies = value;
            UpdateSpeciesPreview();
        }
    }

    public string DisplayName { get; set; } = "SpeciesDetailsTooltip_unset";
    public string? Description { get; set; }

    public float DisplayDelay { get; set; } = 0.1f;
    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.LastMousePosition;
    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Immediate;
    public bool HideOnMouseAction { get; set; }

    public Control ToolTipNode => this;

    public override void _Ready()
    {
        base._Ready();

        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexesPreview = GetNode<CellHexesPreview>(HexPreviewPath);

        if (previewSpecies != null)
            UpdateSpeciesPreview();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (SpeciesPreviewPath != null)
            {
                SpeciesPreviewPath.Dispose();
                HexPreviewPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Updates displayed species information based on the set preview species.
    /// </summary>
    private void UpdateSpeciesPreview()
    {
        if (speciesPreview == null || hexesPreview == null)
            return;

        speciesPreview.PreviewSpecies = PreviewSpecies;

        if (PreviewSpecies == null)
        {
            hexesPreview.PreviewSpecies = null;
            return;
        }

        DisplayName = PreviewSpecies.FormattedName;
        Description = PreviewSpecies.FormattedName;

        if (PreviewSpecies is MicrobeSpecies microbeSpecies)
        {
            hexesPreview.PreviewSpecies = microbeSpecies;
        }
        else
        {
            GD.PrintErr("Unknown species type to preview: ", PreviewSpecies);
            hexesPreview.PreviewSpecies = null;
        }
    }
}
