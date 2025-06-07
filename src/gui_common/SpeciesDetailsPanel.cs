using Godot;

/// <summary>
///   Shows various details a bout a species for the player
/// </summary>
public partial class SpeciesDetailsPanel : MarginContainer
{
#pragma warning disable CA2213
    [Export]
    public SpeciesPreview SpeciesPreview = null!;

    [Export]
    private CustomRichTextLabel? speciesDetailsLabel;

    [Export]
    private CellHexesPreview hexesPreview = null!;
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

            if (speciesDetailsLabel != null)
                UpdateSpeciesPreview();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (previewSpecies != null)
            UpdateSpeciesPreview();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;
    }

    /// <summary>
    ///   Updates displayed species information based on the set preview species.
    /// </summary>
    private void UpdateSpeciesPreview()
    {
        SpeciesPreview.PreviewSpecies = PreviewSpecies;

        if (PreviewSpecies == null)
        {
            hexesPreview.PreviewSpecies = null;
        }
        else if (PreviewSpecies is MicrobeSpecies microbeSpecies)
        {
            hexesPreview.PreviewSpecies = microbeSpecies;
        }
        else
        {
            GD.PrintErr("Unknown species type to preview: ", PreviewSpecies, " (", PreviewSpecies.GetType().Name, ")");
        }

        speciesDetailsLabel!.ExtendedBbcode = PreviewSpecies?.GetDetailString();
    }

    private void OnTranslationsChanged()
    {
        if (previewSpecies != null)
            UpdateSpeciesPreview();
    }
}
