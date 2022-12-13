using System;
using System.Collections.Generic;
using Godot;

public class FilterWindow : CustomConfirmationDialog
{
    // Paths for nodes
    [Export]
    public NodePath FiltersContainerPath = null!;

    private IFilter.IFilterConjunction filters = null!;
    private IFilter.IFilterFactory filterFactory = null!;

    /// <summary>
    ///   If redraw is needed.
    /// </summary>
    private bool dirty;

    private PackedScene filterScene = null!;

    // Nodes
    private VBoxContainer filtersContainer = null!;
    private List<FilterQueryUI> filterLines = new();
    private List<FilterQueryUI> filterLinesSnapshot = null!;

    public override void _Ready()
    {
        base._Ready();

        filterScene = GD.Load<PackedScene>("res://src/gui_common/dialogs/queries/FilterQueryUI.tscn");

        filtersContainer = GetNode<VBoxContainer>(FiltersContainerPath);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (dirty)
        {
            RectSize = GetCombinedMinimumSize();
        }

        dirty = false;
    }

    public void Initialize(IFilter.IFilterFactory filterFactory, IFilter.IFilterConjunction filters)
    {
        // ClearFilters();
        if (this.filters != null)
            throw new InvalidOperationException("Node already initialized!");

        this.filters = filters;

        foreach (var filter in filters.Filters)
        {
            AddFilterLine(filter);
        }

        this.filterFactory = filterFactory;

        // TEMP
        AddFilterFromFactory();
        AddFilterFromFactory();
    }

    public void ClearFilters()
    {
        filters.Clear();
        filterLines.Clear();
        filtersContainer.FreeChildren(true);
    }

    public void AddFilterFromFactory()
    {
        var filter = filterFactory.Create();
        filters.Add(filter);
        AddFilterLine(filter);
    }

    public void AddFilterLine(IFilter filter)
    {
        var filterLine = (FilterQueryUI)filterScene.Instance();
        filterLine.Initialize(this, filter);

        filtersContainer.AddChild(filterLine);
        filterLines.Add(filterLine);

        dirty = true;
    }

    public void MakeFiltersSnapshots()
    {
        foreach (var filterLine in filterLines)
        {
            filterLine.MakeSnapshot();
        }

        filterLinesSnapshot = new(filterLines);
    }

    public void RestoreFiltersSnapshots()
    {
        // First we clean up existing filter lines
        foreach (var filterLine in filterLines)
        {
            if (filterLinesSnapshot.Contains(filterLine))
            {
                // We detach it now so we don't have to do any check afterwards and simply add everything back.
                filterLine.Detach();
            }
            else
            {
                // We free the filter lines that will no longer be registered.
                filterLine.QueueFree();
            }
        }

        // We set up to restore the lines from the snapshot, one at a time
        filterLines = new();

        // We clear the saved filters to refill them with the snapshot
        filters.Clear();

        foreach (var filterLine in filterLinesSnapshot)
        {
            filterLine.RestoreLastSnapshot();

            filtersContainer.AddChild(filterLine);
            filterLines.Add(filterLine);
            filters.Add(filterLine.Filter);
        }

        dirty = true;
    }

    public void RemoveFilterLine(FilterQueryUI filterLine)
    {
        filters.Remove(filterLine.Filter);
        filtersContainer.RemoveChild(filterLine);

        if (!filterLinesSnapshot.Contains(filterLine))
        {
            // If the line is not registered somewhere anymore we free it here to prevent memory leaks...
            filterLine.QueueFree();
        }
        else
        {
            // If was registered (i.e. in a snapshot) the player might want to restore it.
            filterLine.RestoreLastSnapshot();
        }

    }
}
