using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class FilterQueryUI : HBoxContainer, ISnapshotable
{
    [Export]
    public NodePath LeftValueQueryPath = null!;

    [Export]
    public NodePath HeadArgumentButtonPath = null!;

    [Export]
    public NodePath RightValueQueryPath = null!;

    private FilterWindow parentWindow = null!;

    /// <summary>
    ///   The filter that is implemented by the line.
    /// </summary>
    private IFilter filter = null!;

    private ValueQueryUI leftValueQueryUI = null!;
    private CustomDropDown headArgumentButton = null!;
    private ValueQueryUI rightValueQueryUI = null!;

    /// <summary>
    ///   If redraw is needed.
    /// </summary>
    private bool dirty = true;

    public IFilter Filter => filter;

    public void Initialize(FilterWindow parentWindow, IFilter filter)
    {
        this.parentWindow = parentWindow;
        this.filter = filter;

        rightValueQueryUI = GetNode<ValueQueryUI>(RightValueQueryPath);
        leftValueQueryUI = GetNode<ValueQueryUI>(LeftValueQueryPath);

        leftValueQueryUI.Initialize(filter.LeftComparand);
        rightValueQueryUI.Initialize(filter.RightComparand);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (filter == null)
            throw new InvalidOperationException("Node was not initialized!");

        headArgumentButton = GetNode<CustomDropDown>(HeadArgumentButtonPath);

        foreach (var option in filter.HeadArgument.Options)
        {
            headArgumentButton.AddItem(option, false, Colors.White);
        }

        headArgumentButton.CreateElements();

        headArgumentButton.Text = filter.HeadArgument.Value;
        headArgumentButton.Popup.Connect("index_pressed", this, nameof(OnNewCategorySelected));
        //TEMP
        headArgumentButton.Popup.Connect("index_pressed", this, nameof(UpdateRightValueQuery));

        dirty = true;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        base._Process(delta);

        if (dirty)
        {
            Update();
            dirty = false;
        }
    }

    // TO MATCH LEFT QUERY
    // TODO FORMAT
    //TODO REMOVE ARGE (only for connect to popup)
    public void UpdateRightValueQuery(int c)
    {
        rightValueQueryUI.UpdateButtonItems(c => c == leftValueQueryUI.CategoryName || c == ValueQueryUI.NUMBER_FIELD);
    }

    public void MakeSnapshot()
    {
        leftValueQueryUI.MakeSnapshot();
        rightValueQueryUI.MakeSnapshot();

        // TODO CLEAN THIS LOGIC
        filter.HeadArgument.Value = headArgumentButton.Text;
    }

    public void RestoreLastSnapshot()
    {
        leftValueQueryUI.RestoreLastSnapshot();
        headArgumentButton.Text = filter.HeadArgument.Value;
        rightValueQueryUI.RestoreLastSnapshot();
    }

    public void Delete()
    {
        parentWindow.RemoveFilterLine(this);
    }

    // TODO RENAME AS IT NOW FALLS ONTO COMPARISON
    private void OnNewCategorySelected(int choiceIndex)
    {
        var filterCategory = headArgumentButton.Popup.GetItemText(choiceIndex);

        // Do nothing if no change actually happened
        if (filterCategory == headArgumentButton.Text)
            return;

        headArgumentButton.Text = filterCategory;

        dirty = true;
    }
}
