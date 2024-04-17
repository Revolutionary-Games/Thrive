using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Custom widget for microbe editor's collapsible/expandable item list box
/// </summary>
public partial class CollapsibleList : VBoxContainer
{
    private readonly NodePath minimumSizeYReference = new("custom_minimum_size:y");
    private readonly List<Control> items = new();

#pragma warning disable CA2213
    [Export]
    private Label? titleLabel;

    [Export]
    private GridContainer? itemContainer;

    [Export]
    private MarginContainer clipBox = null!;

    [Export]
    private Control? titleContainer;

    [Export]
    private BaseButton collapseButton = null!;

    [Export]
    private BaseButton expandButton = null!;
#pragma warning restore CA2213

    private string title = string.Empty;
    private int columns = 1;
    private bool collapsed;
    private bool isCollapsing;

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
        UpdateItemContainer();
        DetectExistingItems();
        UpdateTitle();

        if (!Collapsed)
            UpdateClipMinimumSize();
    }

    public void AddItem(Control item)
    {
        if (itemContainer == null)
            throw new SceneTreeAttachRequired();

        itemContainer.AddChild(item);
        items.Add(item);

        // Readjusts the clip box height
        if (!Collapsed)
            UpdateClipMinimumSize();
    }

    public T GetItem<T>(string name)
        where T : Control
    {
        return (T?)items.Find(i => i.Name == name) ?? throw new ArgumentException("No item found with name");
    }

    public void RemoveItem(string name, bool isBulkOperation = false)
    {
        var found = items.Find(i => i.Name == name);

        if (found == null)
        {
            GD.PrintErr("Cannot remove item by name from collapsible list, it was not found: ", name);
            return;
        }

        found.QueueFree();
        items.Remove(found);

        if (!isBulkOperation)
        {
            Invoke.Instance.QueueForObject(() =>
            {
                if (!Collapsed)
                    UpdateClipMinimumSize();
            }, this);
        }
    }

    public void RemoveAllOfType<T>()
        where T : Control
    {
        var found = items.FindAll(i => i is T);

        bool removed = false;

        foreach (var item in found)
        {
            item.QueueFree();
            items.Remove(item);
            removed = true;
        }

        if (removed)
        {
            Invoke.Instance.QueueForObject(() =>
            {
                if (!Collapsed)
                    UpdateClipMinimumSize();
            }, this);
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

        Invoke.Instance.QueueForObject(() =>
        {
            if (!Collapsed)
                UpdateClipMinimumSize();
        }, this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            minimumSizeYReference.Dispose();
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
    private void DetectExistingItems()
    {
        if (itemContainer == null)
            throw new SceneTreeAttachRequired();

        foreach (var item in itemContainer.GetChildren().OfType<Control>())
        {
            items.Add(item);
        }

        // Move child items from this to the clip, this makes adding things to this type without having to make child
        // nodes editable in all scenes
        foreach (var potentialItem in GetChildren().OfType<Control>())
        {
            if (potentialItem == clipBox || potentialItem == titleContainer)
                continue;

            RemoveChild(potentialItem);
            itemContainer.AddChild(potentialItem);
            items.Add(potentialItem);
        }
    }

    private void UpdateClipMinimumSize()
    {
        if (itemContainer == null)
            throw new SceneTreeAttachRequired();

        clipBox.CustomMinimumSize = new Vector2(0, itemContainer.GetMinimumSize().Y);
    }

    private void UpdateItemContainer()
    {
        if (itemContainer == null || columns < 1)
            return;

        itemContainer.Columns = columns;
        UpdateClipMinimumSize();
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

        tween.TweenProperty(clipBox, minimumSizeYReference, 0, 0.3);

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

        // TODO: there's probably a chance for a bug if an item is added while the animation is playing (this would
        // need to query the size again and again, or a separate tween needs to be started if an item is added while
        // animating)
        tween.TweenProperty(clipBox, minimumSizeYReference, itemContainer.GetMinimumSize().Y, 0.3);
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
