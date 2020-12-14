using Godot;

/// <summary>
///   For a more generic use and less customized tooltips, only has message text
/// </summary>
public class DefaultToolTip : Control, ICustomToolTip
{
    [Export]
    public NodePath DescriptionLabelPath;

    /// <summary>
    ///   TODO: Use RichTextLabel once its sizing issue is fixed
    /// </summary>
    private Label descriptionLabel;

    private Tween tween;

    private string description;

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

    /// <summary>
    ///   Only get and sets node name since this tooltip only shows a message
    /// </summary>
    public string DisplayName
    {
        get => ToolTipNode.Name;
        set => ToolTipNode.Name = value;
    }

    [Export]
    public string Description
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

    public bool ToolTipVisible
    {
        get => Visible;
        set => Visible = value;
    }

    public Node ToolTipNode => this;

    public override void _Ready()
    {
        // TODO: for some reason the NodePath wouldn't set correctly if the scene is instantiated with
        // a different node name, so this use hard-coded path for now
        // See https://github.com/Revolutionary-Games/Thrive/issues/1855
        descriptionLabel = GetNode<Label>("MarginContainer/VBoxContainer/Description");

        tween = GetNode<Tween>("Tween");

        UpdateDescription();
    }

    public void OnDisplay()
    {
        ToolTipHelper.TooltipFadeIn(tween, this);
    }

    public void OnHide()
    {
        Hide();
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
