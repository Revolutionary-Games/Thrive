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
    private readonly NodePath sizeYReference = new("size:y");
    private readonly List<Control> items = new();

#pragma warning disable CA2213
    [Export]
    private Label? titleLabel;

    [Export]
    private GridContainer? itemContainer;

    [Export]
    private Control clipBox = null!;

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
            UpdateItemContainerColumnCount();
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
        // Direct update, to not trigger a resize yet
        if (columns > 0)
            itemContainer!.Columns = columns;

        DetectExistingItems();
        UpdateTitle();

        AvailableWidthChanged();

        if (!Collapsed)
        {
            UpdateClipMinimumSize();
        }
        else
        {
            clipBox.Size = new Vector2(0, 0);
            itemContainer!.Hide();
        }
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

    public T GetItem<T>(StringName name)
        where T : Control
    {
        return (T?)items.Find(i => i.Name == name) ?? throw new ArgumentException("No item found with name");
    }

    public void RemoveItem(StringName name)
    {
        var found = items.Find(i => i.Name == name);

        if (found == null)
        {
            GD.PrintErr("Cannot remove item by name from collapsible list, it was not found: ", name);
            return;
        }

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
            minimumSizeYReference.Dispose();
            sizeYReference.Dispose();
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
        if (itemContainer == null || titleContainer == null)
            throw new SceneTreeAttachRequired();

        var size = itemContainer.GetMinimumSize();

        if (size.X < titleContainer.Size.X)
            size.X = titleContainer.Size.X;

        clipBox.CustomMinimumSize = size;
    }

    private void UpdateItemContainerColumnCount()
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

        tween.Parallel();

        tween.TweenProperty(clipBox, minimumSizeYReference, 0, 0.3);

        // Need to tween also size to make the animation work
        tween.TweenProperty(clipBox, sizeYReference, 0, 0.3);

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

    /// <summary>
    ///   When the minimum size of the content inside the real container changes and its minimum size does, this makes
    ///   sure the clip control is big enough to display all the info.
    /// </summary>
    private void OnContainerMinimumSizeChanged()
    {
        if (!isCollapsing && !Collapsed)
        {
            UpdateClipMinimumSize();
        }
    }

    /// <summary>
    ///   Updates width of the content container as the parent node of it is not a container, so it doesn't update that
    ///   automatically
    /// </summary>
    private void AvailableWidthChanged()
    {
        if (itemContainer == null)
            return;

        var wantedWidth = clipBox.Size.X;

        // Height is reset to 0 to make sure the container doesn't take extra height
        itemContainer.Size = new Vector2(wantedWidth, 0);
    }
}
