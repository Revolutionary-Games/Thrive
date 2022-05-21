using Godot;

public class GalleryDetailsTooltip : PanelContainer, ICustomToolTip
{
    [Export]
    public NodePath TitleLabelPath = null!;

    [Export]
    public NodePath DescriptionLabelPath = null!;

    [Export]
    public NodePath ArtistLabelPath = null!;

    private Label titleLabel = null!;
    private Label descriptionLabel = null!;
    private Label artistLabel = null!;

    private string? title;
    private string? description;
    private string? artist;

    public string DisplayName
    {
        get => title ?? TranslationServer.Translate("N_A");
        set
        {
            title = value;
            UpdateContent();
        }
    }

    public string? Description
    {
        get => description ?? TranslationServer.Translate("N_A");
        set
        {
            description = value;
            UpdateContent();
        }
    }

    public string Artist
    {
        get => artist ?? TranslationServer.Translate("N_A");
        set
        {
            artist = value;
            UpdateContent();
        }
    }

    public float DisplayDelay { get; set; } = 0.5f;

    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.LastMousePosition;

    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Fade;

    public bool HideOnMousePress { get; set; } = true;

    public Control ToolTipNode => this;

    public override void _Ready()
    {
        titleLabel = GetNode<Label>(TitleLabelPath);
        descriptionLabel = GetNode<Label>(DescriptionLabelPath);
        artistLabel = GetNode<Label>(ArtistLabelPath);

        UpdateContent();
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
