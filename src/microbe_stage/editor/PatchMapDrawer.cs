using System;
using System.Collections.Generic;
using System.Linq;
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
    public float PatchMargin = 6.0f;

    [Export(PropertyHint.ColorNoAlpha)]
    public Color DefaultConnectionColor = Colors.ForestGreen;

    [Export(PropertyHint.ColorNoAlpha)]
    public Color HighlightedConnectionColor = Colors.Cyan;

    [Export]
    public float RegionLineWidth = 4.0f;

    [Export]
    public ShaderMaterial MonochromeMaterial = null!;

    private readonly Dictionary<Patch, PatchMapNode> nodes = new();

    /// <summary>
    ///   The representation of connections between regions, so we won't draw the same connection multiple times
    /// </summary>
    private readonly Dictionary<Int2, Vector2[]> connections = new();

    private PackedScene nodeScene = null!;

    private PatchMap map = null!;

    private bool dirty = true;

    private bool alreadyDrawn;

    private Dictionary<Patch, bool>? patchEnableStatusesToBeApplied;

    private Patch? selectedPatch;

    private Patch? playerPatch;

    [Signal]
    public delegate void OnCurrentPatchCentered(Vector2 coordinates);

    public PatchMap? Map
    {
        get => map;
        set
        {
            map = value ?? throw new ArgumentNullException(nameof(value), "setting to null not allowed");
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
        CheckForDirtyNodes();

        if (dirty)
        {
            RebuildMapNodes();
            Update();

            RectMinSize = GetRightCornerPointOnMap() + new Vector2(450, 450);

            dirty = false;
        }
    }

    /// <summary>
    ///   Custom drawing, draws the lines between map nodes
    /// </summary>
    public override void _Draw()
    {
        if (Map == null)
            return;

        // Create connections between regions if they dont exist.
        if (connections.Count == 0)
            CreateRegionLinks();

        DrawRegionLinks();
        DrawRegions();

        // This ends up drawing duplicates but that doesn't seem problematic ATM
        foreach (var entry in Map.Patches)
        {
            foreach (var adjacent in entry.Value.Adjacent)
            {
                // Only draw connections if patches belong to the same region
                if (entry.Value.Region.ID == adjacent.Region.ID)
                {
                    var start = Center(entry.Value.ScreenCoordinates);
                    var end = Center(adjacent.ScreenCoordinates);

                    DrawNodeLink(start, end, DefaultConnectionColor);
                }
            }
        }

        // Scroll to player patch only when first drawn
        if (!alreadyDrawn)
        {
            CenterScroll();
            alreadyDrawn = true;
        }
    }

    public void CenterScroll()
    {
        EmitSignal(nameof(OnCurrentPatchCentered), PlayerPatch!.ScreenCoordinates);
    }

    /// <summary>
    ///   Stores patch node status values that will be applied when creating the patch nodes
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that this only works *before* the patch nodes are created, this doesn't apply retroactively
    ///   </para>
    /// </remarks>
    /// <param name="statuses">The enabled status values to store</param>
    public void SetPatchEnabledStatuses(Dictionary<Patch, bool> statuses)
    {
        patchEnableStatusesToBeApplied = statuses;
    }

    public void SetPatchEnabledStatuses(IEnumerable<Patch> patches, Func<Patch, bool> predicate)
    {
        SetPatchEnabledStatuses(patches.ToDictionary(x => x, predicate));
    }

    private static Vector2 ClosestPoint(Vector2 comparisonPoint, Vector2 point1, Vector2 point2)
    {
        return point1.DistanceSquaredTo(comparisonPoint) > point2.DistanceSquaredTo(comparisonPoint) ? point2 : point1;
    }

    private static Vector2 SegmentSegmentIntersection(Vector2 segment1Start, Vector2 segment1End, Vector2 segment2Start,
        Vector2 segment2End)
    {
        return (Vector2?)Geometry.SegmentIntersectsSegment2d(segment1Start, segment1End, segment2Start,
            segment2End) ?? Vector2.Inf;
    }

    private static Vector2 ClosestSegmentRectangleIntersection(Vector2 start, Vector2 end, Rect2 rect)
    {
        var intersection = -Vector2.Inf;
        var p0 = rect.Position;
        var p1 = rect.Position + new Vector2(0, rect.Size.y);
        var p2 = rect.Position + new Vector2(rect.Size.x, 0);
        var p3 = rect.End;

        var intersection1 = SegmentSegmentIntersection(p0, p1, start, end);
        var intersection2 = SegmentSegmentIntersection(p0, p2, start, end);
        var intersection3 = SegmentSegmentIntersection(p1, p3, start, end);
        var intersection4 = SegmentSegmentIntersection(p2, p3, start, end);

        intersection = ClosestPoint(end, intersection, intersection1);
        intersection = ClosestPoint(end, intersection, intersection2);
        intersection = ClosestPoint(end, intersection, intersection3);
        intersection = ClosestPoint(end, intersection, intersection4);

        return intersection;
    }

    private static (Vector2 Start, Vector2 Intermediate, Vector2 End) ConnectionIntersectionWithRegions(Vector2 start,
        Vector2 end, Vector2 intermediate, PatchRegion region1, PatchRegion region2)
    {
        var regionRect = new Rect2(region1.ScreenCoordinates, region1.Size);
        var adjacentRect = new Rect2(region2.ScreenCoordinates, region2.Size);

        if (regionRect.HasPoint(intermediate))
        {
            start = intermediate;
            intermediate = start * 0.5f + end * 0.5f;
        }

        if (adjacentRect.HasPoint(intermediate))
        {
            end = intermediate;
            intermediate = start * 0.5f + end * 0.5f;
        }

        var newStart = ClosestSegmentRectangleIntersection(start, intermediate, regionRect);
        var newEnd = ClosestSegmentRectangleIntersection(end, intermediate, adjacentRect);

        if (newStart != -Vector2.Inf)
            start = newStart;

        if (newEnd != -Vector2.Inf)
            end = newEnd;

        return (start, intermediate, end);
    }

    private Vector2 RegionCenter(PatchRegion region)
    {
        return new Vector2(region.ScreenCoordinates.x + region.Width * 0.5f,
            region.ScreenCoordinates.y + region.Height * 0.5f);
    }

    private Vector2 Center(Vector2 pos)
    {
        return new Vector2(pos.x + PatchNodeWidth * 0.5f, pos.y + PatchNodeHeight * 0.5f);
    }

    private void DrawNodeLink(Vector2 center1, Vector2 center2, Color connectionColor)
    {
        DrawLine(center1, center2, connectionColor, ConnectionLineWidth, true);
    }

    private PatchMapNode? GetPatchNode(Patch patch)
    {
        nodes.TryGetValue(patch, out var node);
        return node;
    }

    private bool ContainsSelected(PatchRegion region)
    {
        return region.Patches.Any(p => GetPatchNode(p)?.Selected == true);
    }

    private bool ContainsAdjacentToSelected(PatchRegion region)
    {
        return region.Patches.Any(p => GetPatchNode(p)?.AdjacentToSelectedPatch == true);
    }

    private bool CheckHighlightedAdjacency(PatchRegion region1, PatchRegion region2)
    {
        return (ContainsSelected(region1) && ContainsAdjacentToSelected(region2)) ||
            (ContainsSelected(region2) && ContainsAdjacentToSelected(region1));
    }

    private Vector2 GetRightCornerPointOnMap()
    {
        var point = Vector2.Zero;

        foreach (var region in map.Regions)
        {
            var regionEnd = region.Value.ScreenCoordinates + region.Value.Size;

            point.x = Math.Max(point.x, regionEnd.x);
            point.y = Math.Max(point.y, regionEnd.y);
        }

        return point;
    }

    private Vector2 GetLeastIntersectionIntermediate(PatchRegion region1, PatchRegion region2)
    {
        var start = RegionCenter(region1);
        var end = RegionCenter(region2);
        var intermediate1 = new Vector2(start.x, end.y);
        var intermediate2 = new Vector2(end.x, start.y);
        var firstIntermediateIntersections = 0;
        var secondIntermediateIntersections = 0;

        // Calculate the number of intersecting regions for each possible line path
        foreach (var entry in map.Regions)
        {
            var value = entry.Value;

            if (value != region1 && value != region2)
            {
                var regionRect = new Rect2(value.ScreenCoordinates, value.Size);
                if (ClosestSegmentRectangleIntersection(start, intermediate1, regionRect) != -Vector2.Inf ||
                    ClosestSegmentRectangleIntersection(intermediate1, end, regionRect) != -Vector2.Inf)
                {
                    ++firstIntermediateIntersections;
                }

                if (ClosestSegmentRectangleIntersection(start, intermediate2, regionRect) != -Vector2.Inf ||
                    ClosestSegmentRectangleIntersection(intermediate2, end, regionRect) != -Vector2.Inf)
                {
                    ++secondIntermediateIntersections;
                }
            }
        }

        return firstIntermediateIntersections > secondIntermediateIntersections ? intermediate2 : intermediate1;
    }

    /// <summary>
    ///   This function associates a connection the points the connection line has to go through
    /// </summary>
    private void CreateRegionLinks()
    {
        foreach (var entry in map.Regions)
        {
            var region = entry.Value;

            foreach (var adjacent in region.Adjacent)
            {
                var connectionKey = new Int2(region.ID, adjacent.ID);
                var reverseConnectionKey = new Int2(adjacent.ID, region.ID);

                if (connections.ContainsKey(connectionKey) || connections.ContainsKey(reverseConnectionKey))
                    continue;

                var start = RegionCenter(region);
                var end = RegionCenter(adjacent);

                var intermediate = GetLeastIntersectionIntermediate(region, adjacent);

                (start, intermediate, end) =
                    ConnectionIntersectionWithRegions(start, end, intermediate, region, adjacent);

                connections.Add(connectionKey, new[] { start, intermediate, end });
            }
        }
    }

    private void DrawRegionLinks()
    {
        var highlightedConnections = new List<Vector2[]>();

        // We first draw the normal connections between regions
        foreach (var entry in connections)
        {
            var region1 = map.Regions[entry.Key.x];
            var region2 = map.Regions[entry.Key.y];

            var points = entry.Value;
            for (var i = 1; i < points.Length; i++)
            {
                DrawNodeLink(points[i - 1], points[i], DefaultConnectionColor);
            }

            if (CheckHighlightedAdjacency(region1, region2))
                highlightedConnections.Add(entry.Value);
        }

        // Then we draw the the adjacent connections to the patch we selected
        // Those connections have to be drawn over the normal connections so they're second
        foreach (var points in highlightedConnections)
        {
            for (var i = 1; i < points.Length; i++)
            {
                DrawNodeLink(points[i - 1], points[i], HighlightedConnectionColor);
            }
        }
    }

    private void DrawRegions()
    {
        // Don't draw a border if there's only one region
        if (map.Regions.Count == 1)
            return;

        foreach (var entry in map.Regions)
        {
            var region = entry.Value;
            DrawRect(new Rect2(region.ScreenCoordinates, new Vector2(region.Width, region.Height)),
                new Color(0.0f, 0.7f, 0.5f, 0.7f), false, RegionLineWidth);
        }
    }

    private void RebuildMapNodes()
    {
        foreach (var node in nodes.Values)
        {
            node.Free();
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

            node.MonochromeMaterial = MonochromeMaterial;

            node.SelectCallback = clicked => { SelectedPatch = clicked.Patch; };

            node.Enabled = patchEnableStatusesToBeApplied?[entry.Value] ?? true;

            AddChild(node);
            nodes.Add(node.Patch, node);
        }

        UpdateNodeSelections();
        NotifySelectionChanged();
    }

    private void UpdateNodeSelections()
    {
        foreach (var node in nodes.Values)
        {
            node.Selected = node.Patch == selectedPatch;
            node.Marked = node.Patch == playerPatch;

            if (selectedPatch != null)
                node.AdjacentToSelectedPatch = selectedPatch.Adjacent.Contains(node.Patch);
        }
    }

    private void NotifySelectionChanged()
    {
        OnSelectedPatchChanged?.Invoke(this);
    }

    private void CheckForDirtyNodes()
    {
        foreach (var node in nodes.Values)
        {
            if (node.IsDirty)
            {
                dirty = true;
                break;
            }
        }
    }
}
