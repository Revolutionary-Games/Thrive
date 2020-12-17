﻿using System.Collections.Generic;
using Godot;

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

    private string title;
    private int columns;
    private bool collapsed;
    private bool isCollapsing;

    private Label titleLabel;
    private GridContainer itemContainer;
    private MarginContainer clipBox;
    private TextureButton collapseButton;
    private TextureButton expandButton;
    private Tween tween;

    private List<Control> items = new List<Control>();

    private int cachedTopMarginValue;

    [Export]
    public string Title
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
        tween = GetNode<Tween>(TweenPath);

        cachedTopMarginValue = clipBox.GetConstant("margin_top");

        UpdateTitle();
        UpdateLists();
    }

    public void AddItem(Control item)
    {
        itemContainer.AddChild(item);
        items.Add(item);

        // Readjusts the clip box height
        if (Collapsed)
            clipBox.AddConstantOverride("margin_top", -(int)itemContainer.RectSize.y);
    }

    public Control GetItem(string name)
    {
        return items.Find(match => match.Name == name);
    }

    public void RemoveItem(string name)
    {
        var found = items.Find(item => item.Name == name);
        found.QueueFree();
        items.Remove(found);
    }

    public void ClearItems()
    {
        if (items == null || items.Count == 0)
            return;

        var intermediateList = new List<Control>(items);

        foreach (var item in intermediateList)
        {
            RemoveItem(item.Name);
        }
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
    ///   Add all the already existing childrens into the item list
    /// </summary>
    private void UpdateLists()
    {
        foreach (Control item in itemContainer.GetChildren())
        {
            items.Add(item);
        }
    }

    private void UpdateItemContainer()
    {
        if (itemContainer == null)
            return;

        itemContainer.Columns = columns;
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
