using Godot;

/// <summary>
///   Shows various details about a species for the player
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

    private ulong speciesVisualHash;

    private Species? previewSpecies;

    public Species? PreviewSpecies
    {
        get => previewSpecies;
        set
        {
            var newHash = value?.GetVisualHashCode() ?? 0UL;

            if (newHash == speciesVisualHash)
            {
                // If the generation changes, we need to still update the text data even if the visuals is the same.
                // Because otherwise the evolutionary tree text data won't refresh if switching between visually the
                // same species.
                // TODO: should we rely on other stats as well as for AI species they don't necessarily increment the
                // generation? Though it luckily seems that species basically always mutate which means that they will
                // always have a changed visual hash.
                // TODO: https://github.com/Revolutionary-Games/Thrive/issues/3045
                if (value != null && value.Generation != previewSpecies?.Generation)
                {
                    if (speciesDetailsLabel != null)
                    {
                        previewSpecies = value;
                        UpdateDetailString();
                    }
                }

                return;
            }

            previewSpecies = value;
            speciesVisualHash = newHash;
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

        hexesPreview.PreviewSpecies = PreviewSpecies;

        UpdateDetailString();
    }

    private void OnTranslationsChanged()
    {
        if (previewSpecies != null)
            UpdateDetailString();
    }

    private void UpdateDetailString()
    {
        speciesDetailsLabel!.ExtendedBbcode = PreviewSpecies?.GetDetailString();
    }
}
