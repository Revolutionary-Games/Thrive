using Godot;

/// <summary>
///   Tooltip that shows a species preview image
/// </summary>
public partial class SpeciesPreviewTooltip : PanelContainer, ICustomToolTip
{
#pragma warning disable CA2213
    [Export]
    private SpeciesPreview? speciesPreview;

    [Export]
    private CellHexesPreview? hexesPreview;
#pragma warning restore CA2213

    private ulong speciesVisualHash;

    private Species? previewSpecies;

    public Species? PreviewSpecies
    {
        get => previewSpecies;
        set
        {
            var newHash = value?.GetVisualHashCode() ?? 0UL;

            if (newHash == speciesVisualHash)
                return;

            previewSpecies = value;
            speciesVisualHash = newHash;
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

        if (previewSpecies != null)
            UpdateSpeciesPreview();
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

        hexesPreview.PreviewSpecies = PreviewSpecies;
    }
}
