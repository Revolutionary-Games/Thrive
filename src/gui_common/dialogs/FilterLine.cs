using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class FilterLine : HBoxContainer
{
    [Export]
    public NodePath CategoryButtonPath = null!;

    [Export]
    public NodePath ArgumentsContainerPath = null!;

    private FilterWindow parentWindow = null!;

    private IFilter filter = null!;
    private string defaultText = "--";
    private string categorySnapshot = null!;

    private PackedScene filterArgumentPopupMenuScene = null!;
    private PackedScene filterArgumentSliderScene = null!;

    private CustomDropDown categoryButton = null!;
    private HBoxContainer argumentsContainer = null!;
    private List<IFilterArgumentNode> arguments = new();

    /// <summary>
    ///   If redraw is needed.
    /// </summary>
    private bool dirty = true;

    public IFilter Filter => filter;

    public void Initialize(FilterWindow parentWindow, IFilter filter)
    {
        this.parentWindow = parentWindow;
        this.filter = filter;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (filter == null)
            throw new InvalidOperationException("Node was not initialized!");

        filterArgumentPopupMenuScene =
            GD.Load<PackedScene>("res://src/gui_common/dialogs/FilterArgumentPopupMenu.tscn");
        filterArgumentSliderScene = GD.Load<PackedScene>("res://src/gui_common/dialogs/FilterArgumentSlider.tscn");

        categoryButton = GetNode<CustomDropDown>(CategoryButtonPath);
        argumentsContainer = GetNode<HBoxContainer>(ArgumentsContainerPath);

        categoryButton.Text = defaultText;

        foreach (var option in filter.FilterItemsNames)
        {
            categoryButton.AddItem(option, false, Colors.White);
        }

        categoryButton.CreateElements();
        categoryButton.Popup.Connect("index_pressed", this, nameof(OnNewCategorySelected));

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
        categorySnapshot = filter.FilterCategory;
        GD.Print("Making category snapshot: ", categorySnapshot);

        foreach (var argument in arguments)
        {
            argument.MakeSnapshot();
        }
    }

    public void RestoreLastSnapshot()
    {
        if (categorySnapshot == filter.FilterCategory)
        {
            foreach (var argument in arguments)
            {
                argument.RestoreLastSnapshot();
            }
        }
        else
        {
            // TODO DEAL REDRAWING IF NOT NEEDED
            filter.FilterCategory = categorySnapshot;
            GD.Print("Restoring category ", categorySnapshot);
        }
    }

    public void Delete()
    {
        parentWindow.RemoveFilterLine(this);
    }

    private void OnNewCategorySelected(int choiceIndex)
    {
        var categoryButton = GetChild<CustomDropDown>(0);

        var filterCategory = categoryButton.Popup.GetItemText(choiceIndex);

        // Do nothing if no change actually happened
        if (filterCategory == categoryButton.Text)
            return;

        categoryButton.Text = filterCategory;

        filter.FilterCategory = filterCategory;

        UpdateArguments(filterCategory);

        dirty = true;
    }

    private void UpdateArguments(string filterCategory)
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
    }
}
