using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class FilterQueryUI : HBoxContainer, ISnapshotable
{
    // TODO SET IN SCENE!
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

        GD.Print(leftValueQueryUI);
        GD.Print(filter.LeftComparand);
        // TODO CHECK THIS (NUllREFERENCE)
        leftValueQueryUI.Initialize(filter.LeftComparand);
        rightValueQueryUI.Initialize(filter.RightComparand);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (filter == null)
            throw new InvalidOperationException("Node was not initialized!");

        headArgumentButton = GetNode<CustomDropDown>(HeadArgumentButtonPath);
        rightValueQueryUI = GetNode<ValueQueryUI>(RightValueQueryPath);
        leftValueQueryUI = GetNode<ValueQueryUI>(LeftValueQueryPath);

        foreach (var option in ((FilterArgument.MultipleChoiceFilterArgument)filter.HeadArgument).Options)
        {
            headArgumentButton.AddItem(option, false, Colors.White);
        }

        headArgumentButton.CreateElements();
        //headArgumentButton.Popup.Connect("index_pressed", this, nameof(OnNewCategorySelected))
        headArgumentButton.Popup.Connect("index_pressed", this, nameof(OnNewCategorySelected));

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

    public void MakeSnapshot()
    {
        //categorySnapshot = filter.FilterCategory;
        //GD.Print("Making category snapshot: ", categorySnapshot);

        // TODO SAVE COMPARATOR
        leftValueQueryUI.MakeSnapshot();
        rightValueQueryUI.MakeSnapshot();
    }

    public void RestoreLastSnapshot()
    {
        
    }

    public void Delete()
    {
        parentWindow.RemoveFilterLine(this);
    }

    private void OnNewCategorySelected(int choiceIndex)
    {
        var filterCategory = headArgumentButton.Popup.GetItemText(choiceIndex);

        // Do nothing if no change actually happened
        if (filterCategory == headArgumentButton.Text)
            return;

        headArgumentButton.Text = filterCategory;

        dirty = true;
    }

/*    private void UpdateArguments(string filterCategory)
    {
        ClearArguments();

        if (!filter.FilterItems.TryGetValue(filterCategory, out var filterItem))
            throw new KeyNotFoundException($"Invalid filter category: {filterCategory}");

        foreach (var filterArgument in filterItem.FilterArguments)
        {
            if (filterArgument is FilterArgument.MultipleChoiceFilterArgument multipleChoiceFilterArgument)
            {
                var filterArgumentButton = (FilterArgumentPopupMenu)filterArgumentPopupMenuScene
                    .Instance();
                filterArgumentButton.Initialize(multipleChoiceFilterArgument);
                arguments.Add(filterArgumentButton);
                argumentsContainer.AddChild(filterArgumentButton);
            }
            else if (filterArgument is FilterArgument.NumberFilterArgument numberFilterArgument)
            {
                var filterArgumentSlider = (FilterArgumentSlider)filterArgumentSliderScene.Instance();
                filterArgumentSlider.Initialize(numberFilterArgument);
                arguments.Add(filterArgumentSlider);
                argumentsContainer.AddChild(filterArgumentSlider);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        dirty = true;
    }

    /// <summary>
    ///   Removes all arguments from a filter, but keeps the category.
    /// </summary>
    private void ClearArguments()
    {
        argumentsContainer.FreeChildren(true);

        arguments.Clear();

        dirty = true;
    }*/
}
