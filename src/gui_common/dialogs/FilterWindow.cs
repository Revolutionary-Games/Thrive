using System;
using System.Collections.Generic;
using Godot;

public class FilterWindow : CustomConfirmationDialog
{
    // Paths for nodes
    [Export]
    public NodePath FiltersContainerPath = null!;

    private IFilter.IFilterGroup filters = null!;
    private IFilter.IFilterFactory filterFactory = null!;

    /// <summary>
    ///   If redraw is needed.
    /// </summary>
    private bool dirty;

    private PackedScene filterScene = null!;

    // Nodes
    private VBoxContainer filtersContainer = null!;
    private List<FilterLine> filterLines = new();

    public override void _Ready()
    {
        base._Ready();

        filterScene = GD.Load<PackedScene>("res://src/gui_common/dialogs/FilterLine.tscn");

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

    public void Initialize(IFilter.IFilterFactory filterFactory, IFilter.IFilterGroup filters)
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
        var filterLine = (FilterLine)filterScene.Instance();
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
    }

    public void RestoreFiltersSnapshots()
    {
        foreach (var filterLine in filterLines)
        {
            filterLine.RestoreLastSnapshot();
        }
    }

    public void RemoveFilterLine(FilterLine filterLine)
    {
        filters.Remove(filterLine.Filter);
        filtersContainer.RemoveChild(filterLine);

        // IF YOU DO THAT YOU CAN NOT RESTORE SNAPSHOT!
        filterLine.QueueFree();
    }
}
