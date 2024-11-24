﻿using Godot;

/// <summary>
///   Shows various details a bout a species for the player
/// </summary>
public partial class SpeciesDetailsPanel : MarginContainer
{
    [Export]
    public NodePath? SpeciesDetailsLabelPath;

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    [Export]
    public NodePath HexPreviewPath = null!;

#pragma warning disable CA2213
    private CustomRichTextLabel? speciesDetailsLabel;
    private SpeciesPreview speciesPreview = null!;
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

        speciesDetailsLabel = GetNode<CustomRichTextLabel>(SpeciesDetailsLabelPath);
        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexesPreview = GetNode<CellHexesPreview>(HexPreviewPath);

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (SpeciesDetailsLabelPath != null)
            {
                SpeciesDetailsLabelPath.Dispose();
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
        speciesPreview.PreviewSpecies = PreviewSpecies;

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
            GD.PrintErr("Unknown species type to preview: ", PreviewSpecies);
        }

        speciesDetailsLabel!.ExtendedBbcode = PreviewSpecies?.GetDetailString();
    }

    private void OnTranslationsChanged()
    {
        if (previewSpecies != null)
            UpdateSpeciesPreview();
    }
}
