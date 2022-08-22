using System.Collections.Generic;
using Godot;

public class FilterWindow : CustomConfirmationDialog
{
    // Paths for nodes
    [Export]
    public NodePath FiltersContainerPath = null!;

    private readonly List<Filter> filters = new();
    /// <summary>
    ///   If redraw is needed.
    /// </summary>
    private bool dirty;

    private PackedScene filterScene = GD.Load<PackedScene>("res://src/gui_common/dialogs/FilterLine.tscn");

    // Nodes
    private VBoxContainer filtersContainer = null!;
    private List<FilterLine> filterLines = new();

    public override void _Ready()
    {
        base._Ready();

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

    public void Initialize(Filter filter)
    {
        ClearFilters();
        SetupFilter(filter);
    }

    public void ClearFilters()
    {
        filters.Clear();
        filterLines.Clear();
        filtersContainer.FreeChildren(true);
    }

    public void SetupFilter(Filter filter)
    {
        var filterLine = (FilterLine)filterScene.Instance();
        filterLine.Initialize(filter);

        filtersContainer.AddChild(filterLine);
        filterLines.Add(filterLine);
        filters.Add(filter);

        dirty = true;
    }

    public void MakeFilterSnapshots()
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
}
