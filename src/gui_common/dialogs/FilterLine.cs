using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class FilterLine : HBoxContainer
{
    private Filter filter = null!;
    private string defaultText = "--";
    private string categorySnapshot = null!;

    private PackedScene filterArgumentPopupMenuScene =
        GD.Load<PackedScene>("res://src/gui_common/dialogs/FilterArgumentPopupMenu.tscn");
    private PackedScene filterArgumentSliderScene =
        GD.Load<PackedScene>("res://src/gui_common/dialogs/FilterArgumentSlider.tscn");

    private List<Node> arguments = new();


    /// <summary>
    ///   If redraw is needed.
    /// </summary>
    private bool dirty = true;

    public void Initialize(Filter filter)
    {
        this.filter = filter;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (filter == null)
            throw new InvalidOperationException("Node was not initialized!");

        var categoryButton = new CustomDropDown();
        categoryButton.Text = defaultText;

        foreach (var option in filter.FilterItemsNames)
        {
            categoryButton.AddItem(option, false, Colors.White);
        }

        categoryButton.CreateElements();
        categoryButton.Popup.Connect("index_pressed", this, nameof(OnNewCategorySelected));

        AddChild(categoryButton);

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

    // TODO Use a custom interface instead
    public void AddArgument(Node argumentChild)
    {
        arguments.Add(argumentChild);
        AddChild(argumentChild);
    }

    public void MakeSnapshot()
    {
        categorySnapshot = filter.FilterCategory;
        GD.Print("Making category snapshot: ", categorySnapshot);

        foreach (var argument in arguments)
        {
            // TODO SEE FOR inherited method
            if (argument is FilterArgumentPopupMenu argumentPopupMenu)
            {
                argumentPopupMenu.MakeSnapshot();
            }
            else if (argument is FilterArgumentSlider argumentSlider)
            {
                argumentSlider.MakeSnapshot();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    public void RestoreLastSnapshot()
    {
        // TODO CATEGORY
        if (categorySnapshot == filter.FilterCategory)
        {
            foreach (var argument in arguments)
            {
                // TODO SEE FOR inherited method
                if (argument is FilterArgumentPopupMenu argumentPopupMenu)
                {
                    argumentPopupMenu.RestoreLastSnapshot();
                }
                else if (argument is FilterArgumentSlider argumentSlider)
                {
                    argumentSlider.RestoreLastSnapshot();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
        else
        {
            // TODO DEAL REDRAWING IF NOT NEEDED
            filter.FilterCategory = categorySnapshot;
            GD.Print("Restoring category ", categorySnapshot);
        }
    }

    private void OnNewCategorySelected(int choiceIndex)
    {
        var categoryButton = GetChild<CustomDropDown>(0);

        var filterCategory = categoryButton.Popup.GetItemText(choiceIndex);
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
            if (filterArgument is Filter.MultipleChoiceFilterArgument multipleChoiceFilterArgument)
            {
                var filterArgumentButton = (FilterArgumentPopupMenu)filterArgumentPopupMenuScene
                    .Instance();
                filterArgumentButton.Initialize(multipleChoiceFilterArgument);
                AddArgument(filterArgumentButton);
            }
            else if (filterArgument is Filter.NumberFilterArgument numberFilterArgument)
            {
                var filterArgumentSlider = (FilterArgumentSlider)filterArgumentSliderScene.Instance();
                filterArgumentSlider.Initialize(numberFilterArgument);
                AddArgument(filterArgumentSlider);
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
        this.FreeChildren(true);

        arguments.Clear();

        dirty = true;
    }
}
