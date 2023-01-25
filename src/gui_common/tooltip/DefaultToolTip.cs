using Godot;

/// <summary>
///   For a more generic use and less customized tooltips, only has message text
/// </summary>
public class DefaultToolTip : Control, ICustomToolTip
{
    [Export]
    public NodePath? DescriptionLabelPath;

#pragma warning disable CA2213

    /// <summary>
    ///   TODO: Use RichTextLabel once its sizing issue is fixed
    /// </summary>
    private Label? descriptionLabel;
#pragma warning restore CA2213

    private string? description;

    /// <summary>
    ///   Only gets and sets the Node name since this tooltip only shows a message
    /// </summary>
    public string DisplayName
    {
        get => ToolTipNode.Name;
        set => ToolTipNode.Name = value;
    }

    [Export]
    public string? Description
    {
        get => description;
        set
        {
            description = value;
            UpdateDescription();
        }
    }

    [Export]
    public float DisplayDelay { get; set; } = Constants.TOOLTIP_DEFAULT_DELAY;

    [Export]
    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.LastMousePosition;

    [Export]
    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Immediate;

    [Export]
    public bool HideOnMouseAction { get; set; } = true;

    public Control ToolTipNode => this;

    public override void _Ready()
    {
        // TODO: for some reason the NodePath wouldn't set correctly if the scene is instantiated with
        // a different node name, so this use hard-coded path for now
        // See https://github.com/Revolutionary-Games/Thrive/issues/1855
        descriptionLabel = GetNode<Label>("MarginContainer/VBoxContainer/Description");

        UpdateDescription();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DescriptionLabelPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateDescription()
    {
        if (descriptionLabel == null)
            return;

        if (string.IsNullOrEmpty(Description))
        {
            description = descriptionLabel.Text;
        }
        else
        {
            descriptionLabel.Text = Description;
        }
    }
}
