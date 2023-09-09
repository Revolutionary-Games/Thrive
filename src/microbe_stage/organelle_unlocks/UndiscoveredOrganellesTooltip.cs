using Godot;

/// <summary>
///   The tooltip for undiscovered organelles
/// </summary>
public class UndiscoveredOrganellesTooltip : Control, ICustomToolTip
{
#pragma warning disable CA2213
    [Export]
    public NodePath NameLabelPath = null!;

    [Export]
    public NodePath DescriptionPath = null!;

    private Label? nameLabel;
    private CustomRichTextLabel? descriptionLabel;
#pragma warning restore CA2213

    private string? displayName;
    private string? description;

    [Export]
    public float DisplayDelay { get; set; }

    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.ControlBottomRightCorner;

    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Immediate;

    public bool HideOnMouseAction { get; set; }

    public Control ToolTipNode => this;

    public string DisplayName
    {
        get => displayName ?? "UndiscoveredOrganelleTooltip_unset";
        set
        {
            displayName = value;
            UpdateName();
        }
    }

    public string? Description
    {
        get => description;
        set
        {
            description = value;
            UpdateDescription();
        }
    }

    public override void _Ready()
    {
        descriptionLabel = GetNode<CustomRichTextLabel>(DescriptionPath);
        nameLabel = GetNode<Label>(NameLabelPath);

        UpdateName();
        UpdateDescription();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateName();
            UpdateDescription();
        }
    }

    private void UpdateName()
    {
        if (nameLabel == null)
            return;

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = nameLabel.Text;
        }
        else
        {
            nameLabel.Text = displayName;
        }
    }

    private void UpdateDescription()
    {
        if (descriptionLabel == null)
            return;

        descriptionLabel.Visible = description != null;
        if (description != null)
            descriptionLabel.ExtendedBbcode = description;
    }
}
