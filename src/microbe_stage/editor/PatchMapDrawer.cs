using System;
using System.Linq;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Draws a PatchMap inside a control
/// </summary>
public class PatchMapDrawer : Control
{
    [Export]
    public bool DrawDefaultMapIfEmpty;

    [Export]
    public float ConnectionLineWidth = 2.0f;

    [Export]
    public float PatchNodeWidth = 64.0f;

    [Export]
    public float PatchNodeHeight = 64.0f;

    [Export]
    public float PatchMargin = 6f;

    [Export]
    public Color ConnectionColour = new Color(0f, 0.6f, 0.2f, 1f);

    [Export]
    public Color DefaultConnectionColor = new Color(0f, 0.6f, 0.2f, 1f);

    [Export]
    public Color HighlightedConnectionColor = new Color(0f, 1f, 1f, 1f);

    [Export]
    public float RegionLineWidth = 4f;

    private readonly List<PatchMapNode> nodes = new();

    // The representation of connections between regions 
    // Made so we wont draw the same line multiple times
    private List<Tuple<int,int>> connections = new();
    private PatchMap? map;
    private bool dirty = true;

    private PackedScene nodeScene = null!;

    private Patch? selectedPatch;

    private Patch? playerPatch;

    public PatchMap? Map
    {
        get => map;
        set
        {
            map = value ?? throw new ArgumentNullException();
            dirty = true;

            playerPatch ??= map.CurrentPatch;
        }
    }

    public Patch? PlayerPatch
    {
        get => playerPatch;
        set
        {
            if (playerPatch == value)
                return;

            playerPatch = value;
            UpdateNodeSelections();
            NotifySelectionChanged();
        }
    }

    public Patch? SelectedPatch
    {
        get => selectedPatch;
        set
        {
            if (selectedPatch == value)
                return;

            selectedPatch = value;
            UpdateNodeSelections();
            NotifySelectionChanged();
        }
    }

    /// <summary>
    ///   Called when the currently shown patch properties should be looked up again
    /// </summary>
    public Action<PatchMapDrawer>? OnSelectedPatchChanged { get; set; }

    public override void _Ready()
    {
        nodeScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/PatchMapNode.tscn");

        if (DrawDefaultMapIfEmpty && Map == null)
        {
            GD.Print("Generating and showing a new patch map for testing in PatchMapDrawer");
            Map = new GameWorld(new WorldGenerationSettings()).Map;
        }

        
    }

    public override void _Process(float delta)
    {
        if (dirty)
        {
            RebuildMapNodes();
            Update();
            dirty = false;
            RectMinSize = GetRightCornerPointOnMap() + new Vector2(100, 100);
        }
    }

    /// <summary>
    ///   Custom drawing, draws the lines between map nodes
    /// </summary>
    public override void _Draw()
    {
        if (Map == null)
            return;
        
        connections = new();
        foreach (var entry in Map.Regions)
        {
            var region = entry.Value;
            DrawRect(new Rect2(region.ScreenCoordinates, new Vector2(region.Width, region.Height)), new Color(0f, 0.7f, 0.5f, 0.7f), 
                false, RegionLineWidth);

            foreach (var adjacent in region.Adjacent)
            {
                if (adjacent.ID >= 0)
                {
                    var tuple = Tuple.Create(region.ID, adjacent.ID);
                    var complementTuple = Tuple.Create(adjacent.ID, region.ID);

                    if (!connections.Contains(tuple) && !connections.Contains(complementTuple))
                    {
                        var start = RegionCenter(region);
                        var end = RegionCenter(adjacent);
                        var intermediate1 = new Vector2(start.x, end.y);
                        var intermediate2 = new Vector2(end.x, start.y);

                        if (CheckHighlightedAdjency(region, adjacent))
                            ConnectionColour = HighlightedConnectionColor;
                        else
                            ConnectionColour = DefaultConnectionColor;

                        DrawNodeLink(start, intermediate1); 
                        DrawNodeLink(intermediate1, end);

                        connections.Add(tuple);
                    }
                }
            }
        }

        // For special regions draw the connection between them and the normal region.
        foreach (var entry in Map.SpecialRegions)
        {
            var region = entry.Value;
            var patch = region.Patches.First();
            var adjacent = patch.Adjacent.First();
            var start = Center(patch.ScreenCoordinates);
            var end = Center(adjacent.ScreenCoordinates);
            DrawNodeLink(start, end);
            DrawRect(new Rect2(region.ScreenCoordinates, new Vector2(region.Width, region.Height)), new Color(0f, 0.7f, 0.5f, 0.7f), 
                false, RegionLineWidth);

        }
        ConnectionColour = DefaultConnectionColor;
        // This ends up drawing duplicates but that doesn't seem problematic ATM
        foreach (var entry in Map.Patches)
        {
            foreach (var adjacent in entry.Value.Adjacent)
            {

                // Only draw connections if patches belong to the same region
                if (entry.Value.Region.Name == adjacent.Region.Name)
                {
                    var start = Center(entry.Value.ScreenCoordinates);
                    var end = Center(adjacent.ScreenCoordinates);

                    DrawNodeLink(start, end);
                }
            }
        }
    }

    public void MarkDirty()
    {
        dirty = true;
    }

    private Vector2 RegionCenter(PatchRegion region)
    {
        return new Vector2(region.ScreenCoordinates.x + region.Width / 2, region.ScreenCoordinates.y + region.Height / 2);
    }

    private Vector2 Center(Vector2 pos)
    {
        return new Vector2(pos.x + PatchNodeWidth / 2, pos.y + PatchNodeHeight / 2);
    }

    private void DrawNodeLink(Vector2 center1, Vector2 center2)
    {
        DrawLine(center1, center2, ConnectionColour, ConnectionLineWidth, true);
    }
    private PatchMapNode GetNode(Patch patch)
    {
        foreach (var node in nodes)
            if (node.Patch == patch)
                return node;
        return new();
    }
    private bool ContainsSelected(PatchRegion region)
    {
        foreach (var patch in region.Patches)
        {
            var node = GetNode(patch);
            if (node.Selected)
                return true;
        }

        return false;
    }

    private bool ContainsAdjacentToSelected(PatchRegion region)
    {
        foreach (var patch in region.Patches)
        {
            var node = GetNode(patch);
            if (node.SelectionAdjacent)
                return true;
        }

        return false;
    }
    private bool CheckHighlightedAdjency(PatchRegion region1, PatchRegion region2)
    {
        if ((ContainsSelected(region1) && ContainsAdjacentToSelected(region2)) || 
            (ContainsSelected(region2) && ContainsAdjacentToSelected(region1)))
        {
            return true;
        }

        return false;
    }

    private Vector2 GetRightCornerPointOnMap()
    {
        var point = Vector2.Zero;
        foreach (var region in Map!.Regions)
        {
            var regionEnd = region.Value.ScreenCoordinates + region.Value.GetSize();
            
            point.x = Math.Max(point.x, regionEnd.x);
            point.y = Math.Max(point.y, regionEnd.y);
        }

        foreach (var region in Map!.SpecialRegions)
        {
            var regionEnd = region.Value.ScreenCoordinates + region.Value.GetSize();
            
            point.x = Math.Max(point.x, regionEnd.x);
            point.y = Math.Max(point.y, regionEnd.y);
        }
        return point;
    }

    private Vector2 ClosestPoint(Vector2 p, Vector2 q1, Vector2 q2)
    {
        if (q1 == null)
            return new();
        if (q2 == null)
            return new();
        if (q1.DistanceTo(p) > q2.DistanceTo(p))
            return q2;
        else
            return q1;
    }
    private Vector2 LineLineIntersection(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        var dirp = p2 - p1;
        var dirq = q2 - q2;
        return (Vector2)Geometry.LineIntersectsLine2d(p1, dirp, q1, dirq);
    }
    private Vector2 LineRectangleIntersection(Vector2 l1, Vector2 l2, Rect2 rect)
    {
        var intersection = Vector2.Zero;
        var p0 = rect.Position;
        var p1 = rect.Position + new Vector2(0, rect.Size.y);
        var p2 = rect.Position + new Vector2(rect.Size.x, 0);
        var p3 = rect.End;
        
        var int1 = LineLineIntersection(p0, p1, l1, l1);
        var int2 = LineLineIntersection(p0, p2, l1, l1);
        var int3 = LineLineIntersection(p1, p3, l1, l1);
        var int4 = LineLineIntersection(p2, p3, l1, l1);

        intersection = ClosestPoint(l1, intersection, int1);
        intersection = ClosestPoint(l1, intersection, int2);
        intersection = ClosestPoint(l1, intersection, int3);
        intersection = ClosestPoint(l1, intersection, int4);
        
        return intersection;
    }
    private void RebuildMapNodes()
    {
        foreach (var node in nodes)
        {
            node.DetachAndFree();
        }

        nodes.Clear();

        if (Map == null)
            return;

        foreach (var entry in Map.Patches)
        {
            var node = (PatchMapNode)nodeScene.Instance();
            node.MarginLeft = entry.Value.ScreenCoordinates.x;
            node.MarginTop = entry.Value.ScreenCoordinates.y;
            node.RectSize = new Vector2(PatchNodeWidth, PatchNodeHeight);

            node.Patch = entry.Value;
            node.PatchIcon = entry.Value.BiomeTemplate.LoadedIcon;

            node.SelectCallback = clicked => { SelectedPatch = clicked.Patch; };

            AddChild(node);
            nodes.Add(node);
        }

        UpdateNodeSelections();
        NotifySelectionChanged();
    }

    private void UpdateNodeSelections()
    {
        foreach (var node in nodes)
        {
            node.Selected = node.Patch == selectedPatch;
            node.Marked = node.Patch == playerPatch;

            if (selectedPatch != null)
                node.SelectionAdjacent =  selectedPatch.Adjacent.Contains(node.Patch);
        }
    }

    private void NotifySelectionChanged()
    {
        OnSelectedPatchChanged?.Invoke(this);
    }
}
