using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Custom widget for microbe editor's collapsible/expandable item list box
/// </summary>
public partial class CollapsibleList : VBoxContainer
{
    [Export]
    public NodePath? TitleLabelPath;

    [Export]
    public NodePath CollapseButtonPath = null!;

    [Export]
    public NodePath ExpandButtonPath = null!;

    [Export]
    public NodePath ClipBoxPath = null!;

    [Export]
    public NodePath ItemContainerPath = null!;

    private readonly List<Control> items = new();

    private string title = string.Empty;
    private int columns;
    private bool collapsed;
    private bool isCollapsing;

#pragma warning disable CA2213
    private Label? titleLabel;
    private GridContainer? itemContainer;
    private MarginContainer clipBox = null!;
    private TextureButton collapseButton = null!;
    private TextureButton expandButton = null!;
#pragma warning restore CA2213

    private int cachedTopMarginValue;

    /// <summary>
    ///   The title for the collapsible list.
    /// </summary>
    [Export]
    public string DisplayName
    {
        get => title;
        set
        {
            title = value;
            UpdateTitle();
        }
    }

    [Export]
    public int Columns
    {
        get => columns;
        set
        {
            columns = value;
            UpdateItemContainer();
        }
    }

    [Export]
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

        cachedTopMarginValue = clipBox.GetThemeConstant("margin_top");

        UpdateItemContainer();
        UpdateTitle();
        UpdateLists();
    }

    public void AddItem(Control item)
    {
        if (itemContainer == null)
            throw new SceneTreeAttachRequired();

        itemContainer.AddChild(item);
        items.Add(item);

        // Readjusts the clip box height
        if (Collapsed)
            clipBox.AddThemeConstantOverride("margin_top", -(int)itemContainer.Size.Y);
    }

    public T GetItem<T>(string name)
        where T : Control
    {
        return (T)items.Find(i => i.Name == name);
    }

    public void RemoveItem(string name)
    {
        var found = items.Find(i => i.Name == name);
        found.QueueFree();
        items.Remove(found);
    }

    public void RemoveAllOfType<T>()
        where T : Control
    {
        var found = items.FindAll(i => i is T);

        foreach (var item in found)
        {
            item.QueueFree();
            items.Remove(item);
        }
    }

    public void ClearItems()
    {
        if (items.Count == 0)
            return;

        var intermediateList = new List<Control>(items);

        foreach (var item in intermediateList)
        {
            RemoveItem(item.Name);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TitleLabelPath != null)
            {
                TitleLabelPath.Dispose();
                CollapseButtonPath.Dispose();
                ExpandButtonPath.Dispose();
                ClipBoxPath.Dispose();
                ItemContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateTitle()
    {
        if (titleLabel == null)
            return;

        if (string.IsNullOrEmpty(title))
        {
            // Use the label text
            title = titleLabel.Text;
        }
        else
        {
            titleLabel.Text = title;
        }
    }

    private void UpdateResizing()
    {
        if (itemContainer == null)
            throw new SceneTreeAttachRequired();

        if (Collapsed)
        {
            Collapse();
        }
        else
        {
            if (!isCollapsing)
                Expand();
        }
    }

    /// <summary>
    ///   Add all the already existing children into the item list
    /// </summary>
    private void UpdateLists()
    {
        foreach (Control item in itemContainer!.GetChildren())
        {
            items.Add(item);
        }
    }

    private void UpdateItemContainer()
    {
        if (itemContainer == null || columns < 1)
            return;

        itemContainer.Columns = columns;
    }

    private void Collapse()
    {
        if (isCollapsing)
            GD.PrintErr("Duplicate collapsible list collapse call");

        collapseButton.Hide();
        expandButton.Show();

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenProperty(clipBox, "custom_constants/margin_top", -clipBox.Size.Y, 0.3).From(cachedTopMarginValue);

        tween.TweenCallback(new Callable(this, nameof(OnCollapsingFinished)));

        isCollapsing = true;
    }

    private void Expand()
    {
        if (itemContainer == null)
            throw new InvalidOperationException("Not initialized");

        if (itemContainer.Visible)
            GD.PrintErr("Collapsible list expanded while it was already visible");

        collapseButton.Show();
        expandButton.Hide();

        itemContainer.Show();

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenProperty(clipBox, "custom_constants/margin_top", cachedTopMarginValue, 0.3);
    }

    // GUI Callbacks

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
        itemContainer!.Hide();

        isCollapsing = false;
    }
}
