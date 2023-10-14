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

    /// <summary>
    ///   The representation of connections between regions, so we won't draw the same connection multiple times
    /// </summary>
    private readonly List<RegionLink> connections = new();

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

    public enum Direction
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
    ///   Rebuild the region links
    /// </summary>
    private void RebuildGraphics()
    {
        ClearObjects();
        CreateRegionLinks();

        connectionsDirty = false;
    }

    /// <summary>
    ///   Free all child Line2D objects and clear <see cref="connections"/>
    /// </summary>
    private void ClearObjects()
    {
        foreach (var child in GetChildren())
        {
            if (child is Line2D line)
                line.Free();
        }

        connections.Clear();
    }

    private void DrawNodeLink(Vector2 center1, Vector2 center2, Color connectionColor)
    {
        DrawLine(center1, center2, connectionColor, Constants.PATCH_REGION_CONNECTION_LINE_WIDTH, true);
    }

    private Line2D CreateLink(Vector2[] points, Color connectionColor)
    {
        var link = new Line2D
        {
            DefaultColor = connectionColor,
            Points = points,
            Width = Constants.PATCH_REGION_CONNECTION_LINE_WIDTH,
        };

        AddChild(link);
        return link;
    }

    private void AddFadeToLine(Line2D line, bool reversed)
    {
        var gradient = new Gradient();
        var color = line.DefaultColor;
        Color transparent = new(color, 0);

        gradient.AddPoint(reversed ? 0.7f : 0.3f, transparent);

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

        var createdLinkIds = new HashSet<Int2>();

        // When ordered by distance to center, central regions will be linked first, which reduces intersections.
        foreach (var region in map.Regions.Values.OrderBy(r => mapCenter.DistanceSquaredTo(r.ScreenCoordinates)))
        {
            foreach (var adjacent in region.Adjacent)
            {
                var connectionKey = new Int2(region.ID, adjacent.ID);
                var reverseConnectionKey = new Int2(adjacent.ID, region.ID);

                if (createdLinkIds.Contains(connectionKey) || createdLinkIds.Contains(reverseConnectionKey))
                    continue;

                createdLinkIds.Add(connectionKey);

                var connectionTuples = region.ConnectingPatches[adjacent.ID];

                for (int i = 0; i < connectionTuples.Count; i++)
                {
                    var (to, from) = connectionTuples[i];
                    var pathToAdjacent = GetLeastIntersectingPath(region, adjacent, i);
                    var line = CreateLink(pathToAdjacent, DefaultConnectionColor);
                    var regionLink = new RegionLink(connectionKey, pathToAdjacent, line, to, from);

                    connections.Add(regionLink);
                    break;
                }
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
    private Vector2[] GetLeastIntersectingPath(PatchRegion start, PatchRegion end, int connectionIndex)
    {
        var (endPatch, startPatch) = start.ConnectingPatches[end.ID][connectionIndex];

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

        // Discard all paths intersecting patches in partially discovered regions
        var pathsToDiscard = new List<Vector2[]>();

        if (!start.Explored && start.DiscoveredPatches > 1)
        {
            foreach (var entry in probablePaths)
            {
                var point1 = entry.Path[0];
                var point2 = entry.Path[1];

                foreach (var patch in start.Patches)
                {
                    if (patch.VisibilityState == MapElementVisibility.Undiscovered)
                        continue;

                    if (patch == startPatch)
                        continue;

                    var rect = new Rect2(patch.ScreenCoordinates,
                        Constants.PATCH_NODE_RECT_LENGTH, Constants.PATCH_NODE_RECT_LENGTH);

                    if (SegmentRectangleIntersects(point1, point2, rect))
                    {
                        pathsToDiscard.Add(entry.Path);
                        break;
                    }
                }
            }
        }

        if (!end.Explored && end.DiscoveredPatches > 1)
        {
            foreach (var entry in probablePaths)
            {
                var length = entry.Path.Length;
                var point1 = entry.Path[length - 1];
                var point2 = entry.Path[length - 2];

                foreach (var patch in end.Patches)
                {
                    if (patch.VisibilityState == MapElementVisibility.Undiscovered)
                        continue;

                    if (patch == endPatch)
                        continue;

                    var rect = new Rect2(patch.ScreenCoordinates,
                        Constants.PATCH_NODE_RECT_LENGTH, Constants.PATCH_NODE_RECT_LENGTH);

                    if (SegmentRectangleIntersects(point1, point2, rect))
                    {
                        pathsToDiscard.Add(entry.Path);
                        break;
                    }
                }
            }
        }

        // TODO: There is probably a better way to do this
        probablePaths.RemoveAll(e => pathsToDiscard.Contains(e.Path));

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
        const float lineSeparation = 4 * Constants.PATCH_REGION_CONNECTION_LINE_WIDTH;

        foreach (var link in connections)
            link.AdjustEndpoints();

        foreach (var region in Map!.Regions)
        {
            var connectionsFromRegion = connections.Where(g => g.From.ID == region.Key);
            var connectionsToRegion = connections.Where(g => g.To.ID == region.Key);

            // Separation
            for (int i = 0; i < 4; ++i)
            {
                var direction = (Direction)i;
                var connectionsToDirection = connectionsFromRegion
                    .Where(l => l.StartDirection == direction)
                    .ToList();

                connectionsToDirection.AddRange(connectionsToRegion.Where(l => l.EndDirection == direction));

                // Only when we have more than 1 connections do we need to offset them
                if (connectionsToDirection.Count <= 1)
                    continue;

                GD.Print($"Separating connections in region {region.Value.Name} ({direction})");

                if (direction is Direction.Up or Direction.Down)
                {
                    float right = (connectionsToDirection.Count - 1) / 2.0f;
                    float left = -right;

                    foreach (var link in connectionsToDirection.OrderBy(l => l.Distance))
                    {
                        var start = link.From.Region.ID == region.Key;
                        var path = link.Points;
                        var endpoint = start ? 0 : path.Length - 1;
                        var intermediate = start ? 1 : path.Length - 2;

                        if (path.Length == 2 || path[2 * intermediate - endpoint].x > path[intermediate].x)
                        {
                            link.Points[endpoint].x += lineSeparation * right;
                            link.Points[intermediate].x += lineSeparation * right;
                            right -= 1;
                        }
                        else
                        {
                            link.Points[endpoint].x += lineSeparation * left;
                            link.Points[intermediate].x += lineSeparation * left;
                            left += 1;
                        }

                        link.UpdateGraphics();
                    }
                }
                else
                {
                    float down = (connectionsToDirection.Count - 1) / 2.0f;
                    float up = -down;

                    foreach (var link in connectionsToDirection.OrderBy(t => t.Distance))
                    {
                        var start = link.From.Region.ID == region.Key;

                        var path = link.Points;
                        var endpoint = start ? 0 : path.Length - 1;
                        var intermediate = start ? 1 : path.Length - 2;

                        if (path.Length == 2 || path[2 * intermediate - endpoint].y > path[intermediate].y)
                        {
                            link.Points[endpoint].y += lineSeparation * down;
                            link.Points[intermediate].y += lineSeparation * down;
                            down -= 1;
                        }
                        else
                        {
                            link.Points[endpoint].y += lineSeparation * up;
                            link.Points[intermediate].y += lineSeparation * up;
                            up += 1;
                        }

                        link.UpdateGraphics();
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

        foreach (var link in connections)
        {
            var target = link.Points;

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
        foreach (var entry in connections)
        {
            var start = map.Regions[entry.ID.x];
            var end = map.Regions[entry.ID.y];

            // If both regions are unexplored, don't render the line
            if (!start.Explored && !end.Explored && !IgnoreFogOfWar)
            {
                entry.Line.Visible = false;
                continue;
            }

            entry.Line.Visible = true;

            // Set the color of the line if highlighted
            if (CheckHighlightedAdjacency(start, end))
            {
                entry.Line.DefaultColor = HighlightedConnectionColor;
            }
            else
            {
                entry.Line.DefaultColor = DefaultConnectionColor;
            }

            if (!IgnoreFogOfWar)
            {
                // Add a fade to the line if it's ending at an unexplored region
                if (start.VisibilityState == MapElementVisibility.Undiscovered)
                {
                    AddFadeToLine(entry.Line, true);
                }
                else if (end.VisibilityState == MapElementVisibility.Undiscovered)
                {
                    AddFadeToLine(entry.Line, false);
                }
                else
                {
                    entry.Line.Gradient = null;
                }
            }
        }
    }

    private void DrawRegionBorders()
    {
        // Don't draw a border if there's only one region
        if (map.Regions.Count == 1)
            return;

        foreach (var region in map.Regions.Values)
        {
            if (!region.Explored && !IgnoreFogOfWar)
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
                // Only draw connections if patches belong to the same region
                if (patch.Region.ID == adjacent.Region.ID)
                {
                    if (patch.Region.DiscoveredPatches == 0 && !IgnoreFogOfWar)
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
            var setAsUnknown = entry.Value.VisibilityState == MapElementVisibility.Unexplored;

            node.MarginLeft = entry.Value.ScreenCoordinates.x;
            node.MarginTop = entry.Value.ScreenCoordinates.y;

            node.RectSize = new Vector2(Constants.PATCH_NODE_RECT_LENGTH, Constants.PATCH_NODE_RECT_LENGTH);

            node.VisibilityState = entry.Value.VisibilityState;

            node.Patch = entry.Value;

            node.PatchIcon = setAsUnknown ?
                UnknownPatchTexture :
                entry.Value.BiomeTemplate.LoadedIcon;

            node.MonochromeMaterial = MonochromeMaterial;

            node.SelectCallback = clicked => { SelectedPatch = clicked.Patch; };

            node.Enabled = patchEnableStatusesToBeApplied?[entry.Value] ?? true;

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
    ///   A link between two <see cref="PatchRegion"/>s
    /// </summary>
    private class RegionLink
    {
        public Int2 ID;
        public Vector2[] Points;
        public Line2D Line;

        public Patch To;
        public Patch From;

        public Direction StartDirection;
        public Direction EndDirection;

        public float Distance;

        private readonly Vector2 patchSize = new(Constants.PATCH_NODE_TEXTURE_RECT_LENGTH,
            Constants.PATCH_NODE_TEXTURE_RECT_LENGTH);

        private readonly Vector2 borderVector = new Vector2(Constants.PATCH_REGION_BORDER_WIDTH,
            Constants.PATCH_REGION_BORDER_WIDTH);

        public RegionLink(Int2 id, Vector2[] points, Line2D line, Patch to, Patch from)
        {
            ID = id;
            Points = points;
            Line = line;
            To = to;
            From = from;
        }

        public void AdjustEndpoints()
        {
            StartDirection = AdjustEndpoint(true);
            EndDirection = AdjustEndpoint(false);
        }

        public Direction AdjustEndpoint(bool start)
        {
            var targetRegion = start ? From.Region : To.Region;
            var endpointIndex = start ? 0 : Points.Length - 1;

            var regionSize = targetRegion.Size + borderVector;
            var size = targetRegion.Explored ? regionSize : patchSize;

            var endpoint = Points[endpointIndex];
            var intermediate = Points[start ? 1 : Points.Length - 2];

            Direction direction;
            if (Math.Abs(endpoint.x - intermediate.x) < MathUtils.EPSILON)
            {
                direction = endpoint.y > intermediate.y ? Direction.Up : Direction.Down;
                Distance = Math.Abs(endpoint.y - intermediate.y);
            }
            else
            {
                direction = endpoint.x > intermediate.x ? Direction.Left : Direction.Right;
                Distance = Math.Abs(endpoint.x - intermediate.x);
            }

            ShiftPoint(endpointIndex, size, direction);
            UpdateGraphics();

            return direction;
        }

        /// <summary>
        ///   Updates <see cref="Line"/> to match <see cref="Points"/>
        /// </summary>
        public void UpdateGraphics()
        {
            Line.Points = Points;
        }

        private void ShiftPoint(int index, Vector2 size, Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    Points[index].x -= size.x / 2;
                    break;
                case Direction.Up:
                    Points[index].y -= size.y / 2;
                    break;
                case Direction.Right:
                    Points[index].x += size.x / 2;
                    break;
                case Direction.Down:
                    Points[index].y += size.y / 2;
                    break;
            }
        }
    }
}
