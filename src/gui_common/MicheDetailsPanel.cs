namespace AutoEvo;

using Godot;

/// <summary>
///   Shows various details a bout a species for the player
/// </summary>
public partial class MicheDetailsPanel : MarginContainer
{
    [Export]
    public NodePath? MicheDetailsLabelPath;

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
        micheDetailsLabel!.ExtendedBbcode = PreviewMiche?.GetDetailString();
    }

    private void OnTranslationsChanged()
    {
        if (previewMiche != null)
            UpdateMichePreview();
    }
}
