namespace AutoEvo;

using System;
using System.Linq;
using Godot;

/// <summary>
///   Shows various details about a species to the player
/// </summary>
public partial class MicheDetailsPanel : MarginContainer
{
    [Export]
    public NodePath? MicheDetailsLabelPath;

    public WorldGenerationSettings? WorldSettings = null;

#pragma warning disable CA2213
    private CustomRichTextLabel? micheDetailsLabel;
#pragma warning restore CA2213

    private Miche? previewMiche;

    public Miche? PreviewMiche
    {
        get => previewMiche;
        set
        {
            if (previewMiche == value)
                return;

            previewMiche = value;

            if (previewMiche != null && micheDetailsLabel != null)
                UpdateMichePreview();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        micheDetailsLabel = GetNode<CustomRichTextLabel>(MicheDetailsLabelPath);

        if (previewMiche != null)
            UpdateMichePreview();
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
            if (MicheDetailsLabelPath != null)
            {
                MicheDetailsLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Updates displayed species information based on the set preview species.
    /// </summary>
    private void UpdateMichePreview()
    {
        if (previewMiche == null || WorldSettings == null)
        {
            micheDetailsLabel!.ExtendedBbcode = null;
            return;
        }

        var cache = new SimulationCache(WorldSettings);

        micheDetailsLabel!.ExtendedBbcode = Localization.Translate("MICHE_DETAIL_TEXT").FormatSafe(
            previewMiche.Pressure.ToString(),
            previewMiche.Pressure.GetEnergy(),
            string.Join("\n  ",
                previewMiche.GetOccupants().ToList().Distinct()
                    .Select(b => b.FormattedName + ": " + Math.Round(previewMiche.Pressure.Score(b, cache), 3))));
    }

    private void OnTranslationsChanged()
    {
        if (previewMiche != null)
            UpdateMichePreview();
    }
}
