using Godot;

/// <summary>
///   Tooltip showing the art details in the art gallery
/// </summary>
public partial class GalleryDetailsTooltip : PanelContainer, ICustomToolTip
{
    [Export]
    public NodePath? TitleLabelPath;

    [Export]
    public NodePath DescriptionLabelPath = null!;

    [Export]
    public NodePath ArtistLabelPath = null!;

#pragma warning disable CA2213
    private Label? titleLabel;
    private Label? descriptionLabel;
    private Label? artistLabel;
#pragma warning restore CA2213

    private string? title;
    private string? description;
    private string? artist;

    public string DisplayName
    {
        get => title ?? Localization.Translate("N_A");
        set
        {
            title = value;
            UpdateContent();
        }
    }

    public string? Description
    {
        get => description ?? Localization.Translate("N_A");
        set
        {
            description = value;
            UpdateContent();
        }
    }

    public string? Artist
    {
        get => artist ?? Localization.Translate("N_A");
        set
        {
            artist = value;
            UpdateContent();
        }
    }

    public float DisplayDelay { get; set; } = 0.5f;

    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.LastMousePosition;

    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Fade;

    public bool HideOnMouseAction { get; set; } = true;

    public Control ToolTipNode => this;

    public override void _Ready()
    {
        titleLabel = GetNode<Label>(TitleLabelPath);
        descriptionLabel = GetNode<Label>(DescriptionLabelPath);
        artistLabel = GetNode<Label>(ArtistLabelPath);

        UpdateContent();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += UpdateContent;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= UpdateContent;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TitleLabelPath != null)
            {
                TitleLabelPath.Dispose();
                DescriptionLabelPath.Dispose();
                ArtistLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateContent()
    {
        if (titleLabel == null || descriptionLabel == null || artistLabel == null)
            return;

        titleLabel.Text = DisplayName;
        descriptionLabel.Text = Description;
        artistLabel.Text = Artist;
    }
}
