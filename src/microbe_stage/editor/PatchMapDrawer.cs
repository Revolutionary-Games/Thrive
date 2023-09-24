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
    public Texture? UnknownPatchTexture;

    /// <summary>
    ///   This is true when in the auto-evo exploration tool, when in freebuild
    ///   or when the "Reveal entire patch map" cheat is used
    /// </summary>
    [Export]
    public bool IgnoreFogOfWar;

#pragma warning disable CA2213
    [Export]
    public ShaderMaterial MonochromeMaterial = null!;
#pragma warning restore CA2213

    private readonly Dictionary<Patch, PatchMapNode> nodes = new();

    private readonly Dictionary<Int2, Line2D> regionLinkLines = new();
    private readonly Dictionary<int, Line2D> regionBorderLines = new();

    /// <summary>
    ///   The representation of connections between regions, so we won't draw the same connection multiple times
    /// </summary>
    private readonly Dictionary<Int2, Vector2[]> connections = new();

#pragma warning disable CA2213
    private PackedScene nodeScene = null!;
#pragma warning restore CA2213

    private PatchMap map = null!;

    private bool dirty = true;

    private bool connectionsDirty = true;

    private bool alreadyDrawn;

    private Dictionary<Patch, bool>? patchEnableStatusesToBeApplied;

    private Patch? selectedPatch;

    private Patch? playerPatch;

    [Signal]
    public delegate void OnCurrentPatchCentered(Vector2 coordinates, bool smoothed);

    private enum Direction
    {
        Left = 0,
        Up = 1,
        Right = 2,
        Down = 3,
    }

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

        CheckForDirtyNodes();

        if (dirty)
        {
            RebuildMapNodes();
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

        // Create connections between regions if they dont exist or if they need to be re-drawn
        if (connections.Count == 0 || connectionsDirty)
        {
            RebuildGraphics();
        }

        UpdateRegionLinks();
        UpdateRegionBorders();
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
        connectionsDirty = true;
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

    /// <summary>
    ///   Rebuild the region links and borders
    /// </summary>
    private void RebuildGraphics()
    {
        ClearObjects();

        CreateRegionLinks();
        CreateRegionBorders();

        connectionsDirty = false;
    }

    /// <summary>
    ///   Free all child Line2D objects and clear <see cref="connections"/>
    /// </summary>
    private void ClearObjects()
    {
        foreach (var line in regionLinkLines.Values)
            line.QueueFree();

        foreach (var line in regionBorderLines.Values)
            line.QueueFree();

        connections.Clear();
        regionLinkLines.Clear();
        regionBorderLines.Clear();
    }

    private void DrawNodeLink(Vector2 center1, Vector2 center2, Color connectionColor)
    {
        DrawLine(center1, center2, connectionColor, Constants.PATCH_REGION_CONNECTION_LINE_WIDTH, true);
    }

    private void CreateLink(Vector2[] points, Color connectionColor, Int2 id)
    {
        var link = new Line2D
        {
            DefaultColor = connectionColor,
            Points = points,
            Width = Constants.PATCH_REGION_CONNECTION_LINE_WIDTH,
        };

        AddChild(link);
        regionLinkLines.Add(id, link);
    }

    private void CreateRegionBorders()
    {
        foreach (var region in map.Regions.Values)
        {
            var pos = region.ScreenCoordinates;
            var size = region.Size;

            var points = new Vector2[]
            {
                pos,
                new(pos.x + size.x, pos.y),
                pos + size,
                new(pos.x, pos.y + size.y),
                new(pos.x, pos.y - Constants.PATCH_REGION_BORDER_WIDTH / 2),
            };

            var line = new Line2D
            {
                Points = points,
                DefaultColor = Colors.DarkCyan,
                Width = Constants.PATCH_REGION_BORDER_WIDTH,
            };

            AddChild(line);
            regionBorderLines.Add(region.ID, line);
        }
    }

    private void AddFadeToLine(Line2D line, bool reversed)
    {
        var gradient = new Gradient();
        var color = line.DefaultColor;
        Color transparent = new(color, 0);

        gradient.AddPoint(0.5f, transparent);

        gradient.SetColor(reversed ? 2 : 0, color);
        gradient.SetColor(reversed ? 0 : 2, transparent);
        line.Gradient = gradient;
    }

    private PatchMapNode? GetPatchNode(Patch patch)
    {
        nodes.TryGetValue(patch, out var node);
        return node;
    }

    private bool ContainsSelectedExploredPatch(PatchRegion region)
    {
        return region.Patches.Any(p => GetPatchNode(p)?.Selected == true &&
            GetPatchNode(p)?.VisibilityState == MapElementVisibility.Explored);
    }

    private bool ContainsAdjacentToSelectedPatch(PatchRegion region)
    {
        return region.Patches.Any(p => GetPatchNode(p)?.AdjacentToSelectedPatch == true);
    }

    private bool CheckHighlightedAdjacency(PatchRegion region1, PatchRegion region2)
    {
        return (ContainsSelectedExploredPatch(region1) && ContainsAdjacentToSelectedPatch(region2)) ||
            (ContainsSelectedExploredPatch(region2) && ContainsAdjacentToSelectedPatch(region1));
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
            foreach (var entry in region.Adjacent)
            {
                var adjacent = entry.Key;

                var connectionKey = new Int2(region.ID, adjacent.ID);
                var reverseConnectionKey = new Int2(adjacent.ID, region.ID);

                if (connections.ContainsKey(connectionKey) || connections.ContainsKey(reverseConnectionKey))
                    continue;

                var pathToAdjacent = GetLeastIntersectingPath(region, adjacent);

                connections.Add(connectionKey, pathToAdjacent);
            }
        }

        AdjustPathEndpoints();

        foreach (var entry in connections)
        {
            CreateLink(entry.Value, DefaultConnectionColor, entry.Key);
        }
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
        var startPatch = end.Adjacent[start]!;
        var endPatch = start.Adjacent[end]!;

        var startCenter = RegionCenter(start);
        var startRect = new Rect2(start.ScreenCoordinates, start.Size);
        var endCenter = RegionCenter(end);
        var endRect = new Rect2(end.ScreenCoordinates, end.Size);

        if (!start.Explored)
            startCenter = PatchCenter(startPatch.ScreenCoordinates);

        if (!end.Explored)
            endCenter = PatchCenter(endPatch.ScreenCoordinates);

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

            var connectionTupleList = connectionStartHere.Select(c => (c.Value, 0, 1, c.Key)).ToList();
            connectionTupleList.AddRange(
                connectionEndHere.Select(c => (c.Value, c.Value.Length - 1, c.Value.Length - 2, c.Key)));

            // Separate connection by directions: 0 -> Left, 1 -> Up, 2 -> Right, 3 -> Down
            var connectionsToDirections = new Dictionary<Direction, List<RegionLink>>();

            for (int i = 0; i < 4; ++i)
            {
                connectionsToDirections[(Direction)i] = new List<RegionLink>();
            }

            foreach (var (path, endpoint, intermediate, id) in connectionTupleList)
            {
                if (Math.Abs(path[endpoint].x - path[intermediate].x) < MathUtils.EPSILON)
                {
                    var link = new RegionLink(id, path);

                    connectionsToDirections[
                        path[endpoint].y > path[intermediate].y ? Direction.Up : Direction.Down].Add(link);

                    link.Endpoint = endpoint;
                    link.Intermediate = intermediate;
                    link.Distance = Math.Abs(path[endpoint].y - path[intermediate].y);
                }
                else
                {
                    var link = new RegionLink(id, path);

                    connectionsToDirections[
                        path[endpoint].x > path[intermediate].x ? Direction.Left : Direction.Right].Add(link);

                    link.Endpoint = endpoint;
                    link.Intermediate = intermediate;
                    link.Distance = Math.Abs(path[endpoint].x - path[intermediate].x);
                }
            }

            // Endpoint position
            foreach (var link in connectionsToDirections[Direction.Left])
            {
                var (path, endpoint, _, _, id) = link.ToTuple();

                var end = Map!.Regions[id.y];

                if (end.Explored)
                    path[endpoint].x -= region.Value.Width / 2;
            }

            foreach (var link in connectionsToDirections[Direction.Up])
            {
                var (path, endpoint, _, _, id) = link.ToTuple();
                var end = Map!.Regions[id.y];

                if (end.Explored)
                    path[endpoint].y -= region.Value.Height / 2;
            }

            foreach (var link in connectionsToDirections[Direction.Right])
            {
                var (path, endpoint, _, _, id) = link.ToTuple();
                var end = Map!.Regions[id.y];

                if (end.Explored)
                    path[endpoint].x += region.Value.Width / 2;
            }

            foreach (var link in connectionsToDirections[Direction.Down])
            {
                var (path, endpoint, _, _, id) = link.ToTuple();
                var end = Map!.Regions[id.y];

                if (end.Explored)
                    path[endpoint].y += region.Value.Height / 2;
            }

            // Separation
            const float lineSeparation = 4 * Constants.PATCH_REGION_CONNECTION_LINE_WIDTH;

            for (int direction = 0; direction < 4; ++direction)
            {
                var connectionsToDirection = connectionsToDirections[(Direction)direction];

                // Only when we have more than 1 connections do we need to offset them
                if (connectionsToDirection.Count <= 1)
                    continue;

                if (direction is 1 or 3)
                {
                    float right = (connectionsToDirection.Count - 1) / 2.0f;
                    float left = -right;

                    foreach (var link in connectionsToDirection.OrderBy(t => t.Distance))
                    {
                        var (path, endpoint, intermediate, _, id) = link.ToTuple();

                        var region1 = Map!.Regions[id.y];
                        if (!region1.Explored)
                            continue;

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

                    foreach (var link in connectionsToDirection.OrderBy(t => t.Distance))
                    {
                        var (path, endpoint, intermediate, _, id) = link.ToTuple();

                        var region1 = Map!.Regions[id.y];
                        if (!region1.Explored)
                            continue;

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

    private void UpdateRegionLinks()
    {
        foreach (var entry in regionLinkLines)
        {
            var start = map.Regions[entry.Key.x];
            var end = map.Regions[entry.Key.y];

            // If both regions are unexplored, don't render the line
            if (!start.Explored && !end.Explored && !IgnoreFogOfWar)
            {
                entry.Value.Visible = false;
                continue;
            }

            entry.Value.Visible = true;

            // Set the color of the line if highlighted
            if (CheckHighlightedAdjacency(start, end))
                entry.Value.DefaultColor = HighlightedConnectionColor;
            else
                entry.Value.DefaultColor = DefaultConnectionColor;

            if (IgnoreFogOfWar)
                return;

            // Add a fade to the line if its ending at an unexplored region
            if (start.VisibilityState == MapElementVisibility.Undiscovered)
                AddFadeToLine(entry.Value, true);
            else if (end.VisibilityState == MapElementVisibility.Undiscovered)
                AddFadeToLine(entry.Value, false);
            else
                entry.Value.Gradient = null;
        }
    }

    private void UpdateRegionBorders()
    {
        // Don't draw a border if there's only one region
        if (map.Regions.Count == 1)
            return;

        foreach (var region in map.Regions.Values)
        {
            var line = regionBorderLines[region.ID];

            if (!IgnoreFogOfWar)
                line.Visible = region.VisibilityState == MapElementVisibility.Explored;
            else
                line.Visible = true;
        }
    }

    private void DrawPatchLinks()
    {
        // This ends up drawing duplicates but that doesn't seem problematic ATM
        foreach (var patch in Map!.Patches.Values)
        {
            foreach (var adjacent in patch.Adjacent)
            {
                // Only draw connections if patches belong to the same region
                if (patch.Region.ID == adjacent.Region.ID)
                {
                    if (!patch.Region.Explored && !IgnoreFogOfWar)
                        continue;

                    if ((!patch.Discovered || !adjacent.Discovered) && !IgnoreFogOfWar)
                        continue;

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
    private void RebuildMapNodes()
    {
        foreach (var node in nodes.Values)
        {
            node.Free();
        }

        nodes.Clear();
        connections.Clear();

        if (Map == null)
        {
            SelectedPatch = null;
            return;
        }

        foreach (var entry in Map.Patches)
        {
            var node = (PatchMapNode)nodeScene.Instance();

            if (IgnoreFogOfWar)
                entry.Value.Explored = true;

            // This renders the patch as a question mark if the patch is discovered
            // but has not been entered by the player
            var setAsUnknown = entry.Value.VisibilityState == MapElementVisibility.Unknown;

            node.MarginLeft = entry.Value.ScreenCoordinates.x;
            node.MarginTop = entry.Value.ScreenCoordinates.y;

            node.RectSize = new Vector2(Constants.PATCH_NODE_RECT_LENGTH, Constants.PATCH_NODE_RECT_LENGTH);

            if (entry.Value.Explored)
                node.VisibilityState = MapElementVisibility.Explored;
            else if (setAsUnknown)
                node.VisibilityState = MapElementVisibility.Unknown;
            else
                node.VisibilityState = MapElementVisibility.Undiscovered;

            node.Patch = entry.Value;

            node.PatchIcon = setAsUnknown ?
                UnknownPatchTexture :
                entry.Value.BiomeTemplate.LoadedIcon;

            node.MonochromeMaterial = MonochromeMaterial;

            node.SelectCallback = clicked => { SelectedPatch = clicked.Patch; };

            node.Enabled = patchEnableStatusesToBeApplied?[entry.Value] ?? true;

            if (setAsUnknown)
                node.VisibilityState = MapElementVisibility.Unknown;

            AddChild(node);
            nodes.Add(node.Patch, node);
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

    private void UpdateNodeSelections()
    {
        foreach (var node in nodes.Values)
        {
            node.Selected = node.Patch == SelectedPatch;
            node.Marked = node.Patch == playerPatch;

            if (SelectedPatch != null)
                node.AdjacentToSelectedPatch = SelectedPatch.Adjacent.Contains(node.Patch) && SelectedPatch.Explored;
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

    /// <summary>
    ///   A link between two <see cref="PatchRegion"/>.
    ///   Used in <see cref="AdjustPathEndpoints"/>
    /// </summary>
    private class RegionLink
    {
        public Int2 Id;
        public Vector2[] Points;

        public int? Endpoint = null;
        public int? Intermediate = null;
        public float? Distance = null;

        public RegionLink(Int2 id, Vector2[] points)
        {
            Id = id;
            Points = points;
        }

        public (Vector2[] Points, int Endpoint, int Intermediate, float Distance, Int2 Id) ToTuple()
        {
            return (Points, Endpoint!.Value, Intermediate!.Value, Distance!.Value, Id);
        }
    }
}