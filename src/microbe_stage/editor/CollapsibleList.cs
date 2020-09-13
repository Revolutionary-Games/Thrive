using Godot;
using System.Collections.Generic;

/// <summary>
///   Custom widget for microbe editor's collapsible/expandable item list box
/// </summary>
public class CollapsibleList : VBoxContainer
{
    [Export]
    public NodePath TitleLabelPath;

    [Export]
    public NodePath CollapseButtonPath;

    [Export]
    public NodePath ExpandButtonPath;

    [Export]
    public NodePath ClipBoxPath;

    [Export]
    public NodePath ItemContainerPath;

    [Export]
    public NodePath TweenPath;

    private bool collapsed;
    private bool isCollapsing;

    private Label titleLabel;
    private GridContainer itemContainer;
    private MarginContainer clipBox;
    private TextureButton collapseButton;
    private TextureButton expandButton;
    private Tween tween;

    private List<Control> Items = new List<Control>();

    private int cachedTopMarginValue;

    public bool Collapsed
    {
        get => collapsed;
        set
        {
            collapsed = value;
            UpdateResizing();
        }
    }

    public override void _Ready()
    {
        titleLabel = GetNode<Label>(TitleLabelPath);
        itemContainer = GetNode<GridContainer>(ItemContainerPath);
        clipBox = GetNode<MarginContainer>(ClipBoxPath);
        collapseButton = GetNode<TextureButton>(CollapseButtonPath);
        expandButton = GetNode<TextureButton>(ExpandButtonPath);
        tween = GetNode<Tween>(TweenPath);

        cachedTopMarginValue = clipBox.GetConstant("margin_top");
    }

    public void AddItem(Control item)
    {
        itemContainer.AddChild(item);
        Items.Add(item);

        // Readjusts the clip box height
        if (Collapsed)
            clipBox.AddConstantOverride("margin_top", -(int)itemContainer.RectSize.y);
    }

    public void RemoveItem(Control item)
    {
        Items.Find(x => x == item).QueueFree();
        Items.Remove(item);
    }

    public void ClearItems()
    {
        if (Items == null || Items.Count == 0)
            return;

        var intermediateList = new List<Control>(Items);

        foreach (var item in intermediateList)
        {
            item.QueueFree();
            Items.Remove(item);
        }
    }

    private void UpdateResizing()
    {
        if (Collapsed)
        {
            Collapse();
        }
        else
        {
            Expand();
        }
    }

    private void Collapse()
    {
        collapseButton.Hide();
        expandButton.Show();

        tween.InterpolateProperty(clipBox, "custom_constants/margin_top", cachedTopMarginValue,
            -clipBox.RectSize.y, 0.3f, Tween.TransitionType.Sine, Tween.EaseType.Out);
        tween.Start();

        tween.Connect("tween_all_completed", this, nameof(OnCollapsingFinished), null,
            (int)ConnectFlags.Oneshot);

        isCollapsing = true;
    }

    private void Expand()
    {
        if (isCollapsing)
            return;

        collapseButton.Show();
        expandButton.Hide();

        itemContainer.Show();

        tween.InterpolateProperty(clipBox, "custom_constants/margin_top", null, cachedTopMarginValue, 0.3f,
            Tween.TransitionType.Sine, Tween.EaseType.Out);
        tween.Start();
    }

    /*
        GUI Callbacks
    */

    private void CollapseButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        Collapsed = true;
    }

    private void ExpandButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        Collapsed = false;
    }

    private void OnCollapsingFinished()
    {
        itemContainer.Hide();

        isCollapsing = false;
    }
}
