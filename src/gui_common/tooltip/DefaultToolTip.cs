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
    ///   Only get and set the node name since this tooltip only shows a message
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
        descriptionLabel = GetNode<Label>(DescriptionLabelPath);
        tween = GetNode<Tween>("Tween");
        tween.Connect("tween_started", this, nameof(OnFadeInStarted));

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

    private void OnFadeInStarted(Object obj, NodePath key)
    {
        _ = obj;
        _ = key;

        Show();
    }
}
