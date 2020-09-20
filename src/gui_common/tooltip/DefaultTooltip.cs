using Godot;

/// <summary>
///   For a more generic use and less customized tooltips
/// </summary>
public class DefaultTooltip : Control, ICustomTooltip
{
    [Export]
    public NodePath DescriptionLabelPath;

    /// <summary>
    ///   TODO: Use RichTextLabel
    /// </summary>
    private Label descriptionLabel;

    private string tooltipDescription;

    public Vector2 Position
    {
        get => RectPosition;
        set => RectPosition = value;
    }

    public Vector2 Size
    {
        get => RectSize;
        set => RectSize = value;
    }

    public string TooltipName
    {
        get => Name;
        set => Name = value;
    }

    public string TooltipDescription
    {
        get => tooltipDescription;
        set
        {
            tooltipDescription = value;
            UpdateDescription();
        }
    }

    public float DisplayDelay { get; set; } = Constants.TOOLTIP_DEFAULT_DELAY;

    public bool TooltipVisible
    {
        get => Visible;
        set => Visible = value;
    }

    public Node TooltipNode => this;

    public override void _Ready()
    {
        descriptionLabel = GetNode<Label>(DescriptionLabelPath);
    }

    private void UpdateDescription()
    {
        if (descriptionLabel == null)
            return;

        descriptionLabel.Text = TooltipDescription;
    }
}
