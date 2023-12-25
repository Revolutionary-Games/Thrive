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

    [Export(PropertyHint.ColorNoAlpha)]
    public Color DefaultConnectionColor = Colors.ForestGreen;

    [Export(PropertyHint.ColorNoAlpha)]
    public Color HighlightedConnectionColor = Colors.Cyan;

    [Export]
    public bool IgnoreFogOfWar;

#pragma warning disable CA2213
    [Export]
    public ShaderMaterial MonochromeMaterial = null!;
#pragma warning restore CA2213

    private readonly Dictionary<Patch, PatchMapNode> nodes = new();

    /// <summary>
    ///   The representation of connections between regions, so we won't draw the same connection multiple times
    /// </summary>
    private readonly Dictionary<Int2, Vector2[]> connections = new();

    private readonly Dictionary<Int2, Line2D> regionConnectionLines = new();

    private readonly List<Line2D> additionalConnectionLines = new();

#pragma warning disable CA2213
    private PackedScene nodeScene = null!;
#pragma warning restore CA2213

    private PatchMap map = null!;

    private bool dirty = true;

    private bool alreadyDrawn;

    private Dictionary<Patch, bool>? patchEnableStatusesToBeApplied;

    private Patch? selectedPatch;

    private Patch? playerPatch;

    [Signal]
    public delegate void OnCurrentPatchCentered(Vector2 coordinates, bool smoothed);

    public PatchMap? Map
    {
        get => map;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "setting to null not allowed");

            if (map == value)
                return;

            map = value;
            dirty = true;

            playerPatch ??= map.CurrentPatch;
        }
    }

    /// <summary>
    ///   The current patch the player is in.
    /// </summary>
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
        base._Ready();

        nodeScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/PatchMapNode.tscn");

        if (DrawDefaultMapIfEmpty && Map == null)
        {
            GD.Print("Generating and showing a new patch map for testing in PatchMapDrawer");
            Map = new GameWorld(new WorldGenerationSettings()).Map;
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        // When set to ignore fog of war automatically reveal the entire map set to us
        if (IgnoreFogOfWar && Map != null)
        {
            Map.RevealAllPatches();
            dirty = true;
            IgnoreFogOfWar = false;
        }

        CheckForDirtyNodes();

        if (dirty)
        {
            RebuildMap();
            Update();

            RectMinSize = GetRightBottomCornerPointOnMap() + new Vector2(450, 450);

            dirty = false;
        }
    }

    /// <summary>
    ///   Custom drawing, draws the lines between map nodes
    /// </summary>
    public override void _Draw()
    {
        base._Draw();

        if (Map == null)
            return;

        // Create connections between regions if they dont exist.
        if (connections.Count == 0)
        {
            CreateRegionLinks();
            RebuildRegionConnections();
        }

        DrawRegionBorders();
        DrawPatchLinks();

        // Scroll to player patch only when first drawn
        if (!alreadyDrawn)
        {
            // Just snap, it can get pretty annoying otherwise
            CenterToCurrentPatch(false);

            alreadyDrawn = true;
        }
    }

    /// <summary>
    ///   Centers the map to the coordinates of current patch.
    /// </summary>
    /// <param name="smoothed">If true, smoothly pans the view to the destination, otherwise just snaps.</param>
    public void CenterToCurrentPatch(bool smoothed = true)
    {
        EmitSignal(nameof(OnCurrentPatchCentered), PlayerPatch!.ScreenCoordinates, smoothed);
    }

    public void MarkDirty()
    {
        dirty = true;
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

    /// <summary>
    ///   If two segments parallel to axis intersect each other.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     True if intersect at endpoint. And true if the two segments are collinear and has common points.
    ///   </para>
    ///   <para>
    ///     Doesn't use `Geometry.SegmentIntersectsSegment2d()` because it isn't handling intersection at endpoint well.
    ///   </para>
    /// </remarks>
    /// <returns>True if intersect</returns>
    private static bool SegmentSegmentIntersects(Vector2 segment1Start, Vector2 segment1End,
        Vector2 segment2Start, Vector2 segment2End)
    {
        if (Math.Abs(segment1Start.x - segment1End.x) < MathUtils.EPSILON)
        {
            var segment1Greater = Math.Max(segment1Start.y, segment1End.y);
            var segment1Smaller = Math.Min(segment1Start.y, segment1End.y);

            if (Math.Abs(segment2Start.x - segment2End.x) < MathUtils.EPSILON)
            {
                var segment2Greater = Math.Max(segment2Start.y, segment2End.y);
                var segment2Smaller = Math.Min(segment2Start.y, segment2End.y);

                return (Math.Abs(segment1Start.x - segment2Start.x) < MathUtils.EPSILON) &&
                    !(Math.Max(segment1Smaller, segment2Smaller) - Math.Min(segment1Greater, segment2Greater) >
                        MathUtils.EPSILON);
            }
            else
            {
                if (!(Math.Abs(segment2Start.y - segment2End.y) < MathUtils.EPSILON))
                    throw new InvalidOperationException("Segment2 isn't parallel to axis!");

                var segment2Greater = Math.Max(segment2Start.x, segment2End.x);
                var segment2Smaller = Math.Min(segment2Start.x, segment2End.x);

                return segment1Greater - segment2Start.y > -MathUtils.EPSILON &&
                    segment2Start.y - segment1Smaller > -MathUtils.EPSILON &&
                    segment2Greater - segment1Start.x > -MathUtils.EPSILON &&
                    segment1Start.x - segment2Smaller > -MathUtils.EPSILON;
            }
        }
        else
        {
            if (!(Math.Abs(segment1Start.y - segment1End.y) < MathUtils.EPSILON))
                throw new InvalidOperationException("Segment1 isn't parallel to axis!");

            var segment1Greater = Math.Max(segment1Start.x, segment1End.x);
            var segment1Smaller = Math.Min(segment1Start.x, segment1End.x);

            if (Math.Abs(segment2Start.y - segment2End.y) < MathUtils.EPSILON)
            {
                var segment2Greater = Math.Max(segment2Start.x, segment2End.x);
                var segment2Smaller = Math.Min(segment2Start.x, segment2End.x);

                return (Math.Abs(segment1Start.y - segment2Start.y) < MathUtils.EPSILON) &&
                    !(Math.Max(segment1Smaller, segment2Smaller) - Math.Min(segment1Greater, segment2Greater) >
                        MathUtils.EPSILON);
            }
            else
            {
                if (!(Math.Abs(segment2Start.x - segment2End.x) < MathUtils.EPSILON))
                    throw new InvalidOperationException("Segment2 isn't parallel to axis!");

                var segment2Greater = Math.Max(segment2Start.y, segment2End.y);
                var segment2Smaller = Math.Min(segment2Start.y, segment2End.y);

                return segment1Greater - segment2Start.x > -MathUtils.EPSILON &&
                    segment2Start.x - segment1Smaller > -MathUtils.EPSILON &&
                    segment2Greater - segment1Start.y > -MathUtils.EPSILON &&
                    segment1Start.y - segment2Smaller > -MathUtils.EPSILON;
            }
        }
    }

    private static bool SegmentRectangleIntersects(Vector2 start, Vector2 end, Rect2 rect)
    {
        var p0 = rect.Position;
        var p1 = rect.Position + new Vector2(0, rect.Size.y);
        var p2 = rect.Position + new Vector2(rect.Size.x, 0);
        var p3 = rect.End;

        return SegmentSegmentIntersects(p0, p1, start, end) ||
            SegmentSegmentIntersects(p0, p2, start, end) ||
            SegmentSegmentIntersects(p1, p3, start, end) ||
            SegmentSegmentIntersects(p2, p3, start, end);
    }

    private static Vector2 RegionCenter(PatchRegion region)
    {
        return new Vector2(region.ScreenCoordinates.x + region.Width * 0.5f,
            region.ScreenCoordinates.y + region.Height * 0.5f);
    }

    private static Vector2 PatchCenter(Vector2 pos)
    {
        return new Vector2(pos.x + Constants.PATCH_NODE_RECT_LENGTH * 0.5f,
            pos.y + Constants.PATCH_NODE_RECT_LENGTH * 0.5f);
    }

    private Line2D CreateConnectionLine(Vector2[] points, Color connectionColor)
    {
        var link = new Line2D
        {
            DefaultColor = connectionColor,
            Points = points,
            Width = Constants.PATCH_REGION_CONNECTION_LINE_WIDTH,
        };

        // This is a rather hacky way of rendering the connection lines below the nodes
        // and could probably be improved
        var lowestNode = nodes.Values.OrderBy(n => n.GetIndex()).First();

        if (lowestNode == null)
        {
            AddChild(link);
        }
        else
        {
            AddChildBelowNode(lowestNode, link);
        }

        return link;
    }

    private void ApplyFadeToLine(Line2D line, bool reversed)
    {
        // TODO: it seems just a few gradients are used, so these should be able to be cached
        var gradient = new Gradient();
        var color = line.DefaultColor;
        Color transparent = new(color, 0);

        gradient.AddPoint(reversed ? 0.3f : 0.7f, transparent);

        gradient.SetColor(reversed ? 2 : 0, color);
        gradient.SetColor(reversed ? 0 : 2, transparent);
        line.Gradient = gradient;
    }

    private void DrawNodeLink(Vector2 center1, Vector2 center2, Color connectionColor)
    {
        DrawLine(center1, center2, connectionColor, Constants.PATCH_REGION_CONNECTION_LINE_WIDTH, true);
    }

    private PatchMapNode? GetPatchNode(Patch patch)
    {
        nodes.TryGetValue(patch, out var node);
        return node;
    }

    private bool ContainsSelectedPatch(PatchRegion region)
    {
        return region.Patches.Any(p => GetPatchNode(p)?.Selected == true);
    }

    private bool ContainsAdjacentToSelectedPatch(PatchRegion region)
    {
        return region.Patches.Any(p => GetPatchNode(p)?.AdjacentToSelectedPatch == true);
    }

    private bool CheckHighlightedAdjacency(PatchRegion region1, PatchRegion region2)
    {
        return (ContainsSelectedPatch(region1) && ContainsAdjacentToSelectedPatch(region2)) ||
            (ContainsSelectedPatch(region2) && ContainsAdjacentToSelectedPatch(region1));
    }

    private Vector2 GetRightBottomCornerPointOnMap()
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

    /// <summary>
    ///   This function creates least intersected links to adjoining regions.
    /// </summary>
    private void CreateRegionLinks()
    {
        var mapCenter = map.Center;

        // When ordered by distance to center, central regions will be linked first, which reduces intersections.
        foreach (var region in map.Regions.Values.OrderBy(r => mapCenter.DistanceSquaredTo(r.ScreenCoordinates)))
        {
            foreach (var adjacent in region.Adjacent)
            {
                var connectionKey = new Int2(region.ID, adjacent.ID);
                var reverseConnectionKey = new Int2(adjacent.ID, region.ID);

                if (connections.ContainsKey(connectionKey) || connections.ContainsKey(reverseConnectionKey))
                    continue;

                var pathToAdjacent = GetLeastIntersectingPath(region, adjacent);

                connections.Add(connectionKey, pathToAdjacent);
            }
        }

        AdjustPathEndpoints();
    }

    /// <summary>
    ///   Get the least intersecting path from start region to end region. This is achieved by first calculating all
    ///   possible paths, then figuring out which one intersects the least. If several paths are equally good, return
    ///   the one with highest priority.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Priority: Direct path > L-shape path > Z-shape path > U-shape path
    ///   </para>
    /// </remarks>
    /// <returns>Path represented in a Vector2 array</returns>
    private Vector2[] GetLeastIntersectingPath(PatchRegion start, PatchRegion end)
    {
        var startCenter = RegionCenter(start);
        var startRect = new Rect2(start.ScreenCoordinates, start.Size);
        var endCenter = RegionCenter(end);
        var endRect = new Rect2(end.ScreenCoordinates, end.Size);

        // TODO: it would be pretty nice to be able to use a buffer pool for the path points here as a ton of memory
        // is re-allocated here each time the map needs drawing
        var probablePaths = new List<(Vector2[] Path, int Priority)>();

        // Direct line, I shape, highest priority
        if (Math.Abs(startCenter.x - endCenter.x) < MathUtils.EPSILON ||
            Math.Abs(startCenter.y - endCenter.y) < MathUtils.EPSILON)
        {
            probablePaths.Add((new[] { startCenter, endCenter }, 3));
        }

        // 2-segment line, L shape
        var intermediate = new Vector2(startCenter.x, endCenter.y);
        if (!startRect.HasPoint(intermediate) && !endRect.HasPoint(intermediate))
            probablePaths.Add((new[] { startCenter, intermediate, endCenter }, 2));

        intermediate = new Vector2(endCenter.x, startCenter.y);
        if (!startRect.HasPoint(intermediate) && !endRect.HasPoint(intermediate))
            probablePaths.Add((new[] { startCenter, intermediate, endCenter }, 2));

        // 3-segment lines consider relative position
        var upper = startRect.Position.y < endRect.Position.y ? startRect : endRect;
        var lower = startRect.End.y > endRect.End.y ? startRect : endRect;
        var left = startRect.Position.x < endRect.Position.x ? startRect : endRect;
        var right = startRect.End.x > endRect.End.x ? startRect : endRect;

        // 3-segment line, Z shape
        var middlePoint = new Vector2(left.End.x + right.Position.x, upper.End.y + lower.Position.y) / 2.0f;

        var intermediate1 = new Vector2(startCenter.x, middlePoint.y);
        var intermediate2 = new Vector2(endCenter.x, middlePoint.y);
        if (!startRect.HasPoint(intermediate1) && !endRect.HasPoint(intermediate2))
            probablePaths.Add((new[] { startCenter, intermediate1, intermediate2, endCenter }, 1));

        intermediate1 = new Vector2(middlePoint.x, startCenter.y);
        intermediate2 = new Vector2(middlePoint.x, endCenter.y);
        if (!startRect.HasPoint(intermediate1) && !endRect.HasPoint(intermediate2))
            probablePaths.Add((new[] { startCenter, intermediate1, intermediate2, endCenter }, 1));

        // 3-segment line, U shape
        for (int i = 1; i <= 3; i++)
        {
            intermediate1 = new Vector2(startCenter.x, lower.End.y + i * 50);
            intermediate2 = new Vector2(endCenter.x, lower.End.y + i * 50);
            probablePaths.Add((new[] { startCenter, intermediate1, intermediate2, endCenter }, -i));

            intermediate1 = new Vector2(startCenter.x, upper.Position.y - i * 50);
            intermediate2 = new Vector2(endCenter.x, upper.Position.y - i * 50);
            probablePaths.Add((new[] { startCenter, intermediate1, intermediate2, endCenter }, -i));

            intermediate1 = new Vector2(right.End.x + i * 50, startCenter.y);
            intermediate2 = new Vector2(right.End.x + i * 50, endCenter.y);
            probablePaths.Add((new[] { startCenter, intermediate1, intermediate2, endCenter }, -i));

            intermediate1 = new Vector2(left.Position.x - i * 50, startCenter.y);
            intermediate2 = new Vector2(left.Position.x - i * 50, endCenter.y);
            probablePaths.Add((new[] { startCenter, intermediate1, intermediate2, endCenter }, -i));
        }

        // Choose a best path
        return probablePaths.Select(p => (p.Path, CalculatePathPriorityTuple(p)))
            .OrderBy(p => p.Item2.RegionIntersectionCount)
            .ThenBy(p => p.Item2.PathIntersectionCount)
            .ThenBy(p => p.Item2.StartPointOverlapCount)
            .ThenByDescending(p => p.Item2.Priority)
            .First().Path;
    }

    /// <summary>
    ///   Add a separation between each overlapped line, and adjust connection endpoint
    /// </summary>
    private void AdjustPathEndpoints()
    {
        foreach (var region in Map!.Regions)
        {
            int regionId = region.Key;
            var connectionStartHere = connections.Where(p => p.Key.x == regionId);
            var connectionEndHere = connections.Where(p => p.Key.y == regionId);

            var connectionTupleList = connectionStartHere.Select(c => (c.Value, 0, 1)).ToList();
            connectionTupleList.AddRange(
                connectionEndHere.Select(c => (c.Value, c.Value.Length - 1, c.Value.Length - 2)));

            // Separate connection by directions: 0 -> Left, 1 -> Up, 2 -> Right, 3 -> Down
            // TODO: refactor this to use an enum
            var connectionsToDirections = new List<(Vector2[] Path, int Endpoint, int Intermediate, float Distance)>[4];

            for (int i = 0; i < 4; ++i)
            {
                connectionsToDirections[i] =
                    new List<(Vector2[] Path, int Endpoint, int Intermediate, float Distance)>();
            }

            foreach (var (path, endpoint, intermediate) in connectionTupleList)
            {
                if (Math.Abs(path[endpoint].x - path[intermediate].x) < MathUtils.EPSILON)
                {
                    connectionsToDirections[path[endpoint].y > path[intermediate].y ? 1 : 3].Add((
                        path, endpoint, intermediate,
                        Math.Abs(path[endpoint].y - path[intermediate].y)));
                }
                else
                {
                    connectionsToDirections[path[endpoint].x > path[intermediate].x ? 0 : 2].Add((
                        path, endpoint, intermediate,
                        Math.Abs(path[endpoint].x - path[intermediate].x)));
                }
            }

            var halfBorderWidth = Constants.PATCH_REGION_BORDER_WIDTH / 2;

            // Endpoint position
            foreach (var (path, endpoint, _, _) in connectionsToDirections[0])
            {
                path[endpoint].x -= region.Value.Width / 2 + halfBorderWidth;
            }

            foreach (var (path, endpoint, _, _) in connectionsToDirections[1])
            {
                path[endpoint].y -= region.Value.Height / 2 + halfBorderWidth;
            }

            foreach (var (path, endpoint, _, _) in connectionsToDirections[2])
            {
                path[endpoint].x += region.Value.Width / 2 + halfBorderWidth;
            }

            foreach (var (path, endpoint, _, _) in connectionsToDirections[3])
            {
                path[endpoint].y += region.Value.Height / 2 + halfBorderWidth;
            }

            // Separation
            const float lineSeparation = 4 * Constants.PATCH_REGION_CONNECTION_LINE_WIDTH;

            for (int direction = 0; direction < 4; ++direction)
            {
                var connectionsToDirection = connectionsToDirections[direction];

                // Only when we have more than 1 connections do we need to offset them
                if (connectionsToDirection.Count <= 1)
                    continue;

                if (direction is 1 or 3)
                {
                    float right = (connectionsToDirection.Count - 1) / 2.0f;
                    float left = -right;

                    foreach (var (path, endpoint, intermediate, _) in
                             connectionsToDirection.OrderBy(t => t.Distance))
                    {
                        if (path.Length == 2 || path[2 * intermediate - endpoint].x > path[intermediate].x)
                        {
                            path[endpoint].x += lineSeparation * right;
                            path[intermediate].x += lineSeparation * right;
                            right -= 1;
                        }
                        else
                        {
                            path[endpoint].x += lineSeparation * left;
                            path[intermediate].x += lineSeparation * left;
                            left += 1;
                        }
                    }
                }
                else
                {
                    float down = (connectionsToDirection.Count - 1) / 2.0f;
                    float up = -down;

                    foreach (var (path, endpoint, intermediate, _) in
                             connectionsToDirection.OrderBy(t => t.Distance))
                    {
                        if (path.Length == 2 || path[2 * intermediate - endpoint].y > path[intermediate].y)
                        {
                            path[endpoint].y += lineSeparation * down;
                            path[intermediate].y += lineSeparation * down;
                            down -= 1;
                        }
                        else
                        {
                            path[endpoint].y += lineSeparation * up;
                            path[intermediate].y += lineSeparation * up;
                            up += 1;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    ///   Calculate priority of a path for sorting.
    /// </summary>
    private (int RegionIntersectionCount, int PathIntersectionCount, int StartPointOverlapCount, int Priority)
        CalculatePathPriorityTuple((Vector2[] Path, int Priority) pathPriorityTuple)
    {
        var (path, priority) = pathPriorityTuple;

        // Intersections with regions are considered worse than that with lines.
        // So an intersect with region adds count by 10.
        int regionIntersectionCount = 0;
        int pathIntersectionCount = 0;
        int startPointOverlapCount = 0;

        for (int i = 1; i < path.Length; ++i)
        {
            var startPoint = path[i - 1];
            var endPoint = path[i];

            foreach (var region in map.Regions.Values)
            {
                var regionRect = new Rect2(region.ScreenCoordinates, region.Size);
                if (SegmentRectangleIntersects(startPoint, endPoint, regionRect))
                {
                    ++regionIntersectionCount;
                }
            }
        }

        // Calculate line-to-line intersections
        foreach (var target in connections.Values)
        {
            for (int i = 1; i < path.Length; ++i)
            {
                var startPoint = path[i - 1];
                var endPoint = path[i];

                for (int j = 1; j < target.Length; ++j)
                {
                    if (SegmentSegmentIntersects(startPoint, endPoint, target[j - 1], target[j]))
                        ++pathIntersectionCount;
                }
            }

            // If the endpoint is the same, it is regarded as the two lines intersects but it actually isn't.
            if (path[0] == target[0])
            {
                --pathIntersectionCount;

                // And if they goes the same direction, the second segment intersects but it actually isn't either.
                if (Math.Abs((path[1] - path[0]).AngleTo(target[1] - target[0])) < MathUtils.EPSILON)
                {
                    --pathIntersectionCount;
                    ++startPointOverlapCount;
                }
            }
            else if (path[0] == target[target.Length - 1])
            {
                --pathIntersectionCount;

                if (Math.Abs((path[1] - path[0]).AngleTo(target[target.Length - 2] - target[target.Length - 1]))
                    < MathUtils.EPSILON)
                {
                    --pathIntersectionCount;
                    ++startPointOverlapCount;
                }
            }
            else if (path[path.Length - 1] == target[0])
            {
                --pathIntersectionCount;

                if (Math.Abs((path[path.Length - 2] - path[path.Length - 1]).AngleTo(target[1] - target[0]))
                    < MathUtils.EPSILON)
                {
                    --pathIntersectionCount;
                    ++startPointOverlapCount;
                }
            }
            else if (path[path.Length - 1] == target[target.Length - 1])
            {
                --pathIntersectionCount;

                if (Math.Abs((path[path.Length - 2] - path[path.Length - 1]).AngleTo(target[target.Length - 2] -
                        target[target.Length - 1])) < MathUtils.EPSILON)
                {
                    --pathIntersectionCount;
                    ++startPointOverlapCount;
                }
            }
        }

        // The highest priority has the lowest value.
        return (regionIntersectionCount, pathIntersectionCount, startPointOverlapCount, priority);
    }

    private void DrawRegionBorders()
    {
        // Don't draw a border if there's only one region
        if (map.Regions.Count == 1)
            return;

        foreach (var region in map.Regions.Values)
        {
            // Don't draw borders for hidden regions
            if (region.Visibility != MapElementVisibility.Shown)
                continue;

            DrawRect(new Rect2(region.ScreenCoordinates, region.Size),
                Colors.DarkCyan, false, Constants.PATCH_REGION_BORDER_WIDTH);
        }
    }

    private void DrawPatchLinks()
    {
        // This ends up drawing duplicates but that doesn't seem problematic ATM
        foreach (var patch in Map!.Patches.Values)
        {
            foreach (var adjacent in patch.Adjacent)
            {
                // Do not draw connections to/from hidden patches
                if (patch.Visibility == MapElementVisibility.Hidden ||
                    adjacent.Visibility == MapElementVisibility.Hidden)
                {
                    continue;
                }

                // Only draw connections if patches belong to the same region
                if (patch.Region.ID == adjacent.Region.ID)
                {
                    var start = PatchCenter(patch.ScreenCoordinates);
                    var end = PatchCenter(adjacent.ScreenCoordinates);

                    DrawNodeLink(start, end, DefaultConnectionColor);
                }
            }
        }
    }

    /// <summary>
    ///   Clears the map and rebuilds all nodes
    /// </summary>
    private void RebuildMap()
    {
        foreach (var node in nodes.Values)
            node.Free();

        nodes.Clear();

        connections.Clear();

        if (Map == null)
        {
            SelectedPatch = null;
            return;
        }

        foreach (var entry in Map.Patches)
        {
            AddPatchNode(entry.Value, entry.Value.ScreenCoordinates);
        }

        bool runNodeSelectionsUpdate = true;

        if (SelectedPatch != null)
        {
            // Unset the selected patch if it was removed from the map
            bool found = false;
            foreach (var node in nodes.Values)
            {
                if (node.Patch == SelectedPatch)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                SelectedPatch = null;

                // Changing the selected patch already updates the node selections so we skip a duplicate call with
                // this flag
                runNodeSelectionsUpdate = false;
            }
        }

        if (runNodeSelectionsUpdate)
            UpdateNodeSelections();
    }

    private void AddPatchNode(Patch patch, Vector2 position)
    {
        var node = (PatchMapNode)nodeScene.Instance();
        node.MarginLeft = position.x;
        node.MarginTop = position.y;
        node.RectSize = new Vector2(Constants.PATCH_NODE_RECT_LENGTH, Constants.PATCH_NODE_RECT_LENGTH);

        node.Patch = patch;
        node.PatchIcon = patch.BiomeTemplate.LoadedIcon;

        node.MonochromeMaterial = MonochromeMaterial;

        node.SelectCallback = clicked => { SelectedPatch = clicked.Patch; };

        node.Enabled = patchEnableStatusesToBeApplied?[patch] ?? true;

        AddChild(node);
        nodes.Add(node.Patch, node);
    }

    private void RebuildRegionConnections()
    {
        foreach (var connection in regionConnectionLines.Values)
            connection.Free();

        foreach (var connection in additionalConnectionLines)
            connection.Free();

        regionConnectionLines.Clear();
        additionalConnectionLines.Clear();

        foreach (var entry in connections)
        {
            var region1 = map.Regions[entry.Key.x];
            var region2 = map.Regions[entry.Key.y];

            var vis1 = region1.Visibility;
            var vis2 = region2.Visibility;

            if (vis1 != MapElementVisibility.Shown && vis2 != MapElementVisibility.Shown)
            {
                continue;
            }

            var points = entry.Value;
            var highlight = CheckHighlightedAdjacency(region1, region2);
            var color = highlight ? HighlightedConnectionColor : DefaultConnectionColor;

            var line = CreateConnectionLine(points, color);
            regionConnectionLines.Add(entry.Key, line);

            if (vis1 == MapElementVisibility.Hidden && vis2 == MapElementVisibility.Shown)
            {
                ApplyFadeToLine(line, true);
            }
            else if (vis1 == MapElementVisibility.Shown && vis2 == MapElementVisibility.Hidden)
            {
                ApplyFadeToLine(line, false);
            }

            if (vis1 == MapElementVisibility.Unknown)
            {
                additionalConnectionLines
                    .AddRange(BuildUnknownRegionConnections(line, region1, region2, color, false));
            }

            if (vis2 == MapElementVisibility.Unknown)
            {
                additionalConnectionLines
                    .AddRange(BuildUnknownRegionConnections(line, region2, region1, color, true));
            }
        }
    }

    private Line2D[] BuildUnknownRegionConnections(Line2D startingConnection, PatchRegion targetRegion,
        PatchRegion startRegion, Color color, bool reversed)
    {
        var startingPoint = reversed ? startingConnection.Points[startingConnection.Points.Length - 1]
            : startingConnection.Points[0];

        var adjacencies = startRegion.PatchAdjacencies[targetRegion.ID];

        // Generate a list of patches to connect to
        var patches = targetRegion.Patches
            .Where(p => p.Visibility == MapElementVisibility.Unknown)
            .Where(p => adjacencies.Contains(p));

        var connections = new Line2D[patches.Count()];

        var i = 0;
        foreach (var targetPatch in patches)
        {
            var halfNodeSize = Constants.PATCH_NODE_RECT_LENGTH / 2;
            var endingPoint = targetPatch.ScreenCoordinates + Vector2.One * halfNodeSize;

            if (endingPoint.x == startingPoint.x || endingPoint.y == startingPoint.y)
            {
                var straightPoints = new Vector2[]
                {
                    startingPoint,
                    endingPoint,
                };

                connections[i++] = CreateConnectionLine(straightPoints, color);
                continue;
            }

            var intermediate = new Vector2(endingPoint.x, startingPoint.y);

            var points = new Vector2[]
            {
                startingPoint,
                intermediate,
                endingPoint,
            };

            connections[i++] = CreateConnectionLine(points, color);
        }

        return connections;
    }

    private void UpdateNodeSelections()
    {
        foreach (var node in nodes.Values)
        {
            node.Selected = node.Patch == SelectedPatch;
            node.Marked = node.Patch == playerPatch;

            if (SelectedPatch != null)
                node.AdjacentToSelectedPatch = SelectedPatch.Adjacent.Contains(node.Patch);
        }
    }

    private void NotifySelectionChanged()
    {
        OnSelectedPatchChanged?.Invoke(this);
    }

    private void CheckForDirtyNodes()
    {
        if (nodes.Values.Any(n => n.IsDirty))
        {
            dirty = true;
        }
    }
}
