﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Draws a PatchMap inside a control
/// </summary>
public partial class PatchMapDrawer : Control
{
    [Export]
    public bool DrawDefaultMapIfEmpty;

    [Export(PropertyHint.ColorNoAlpha)]
    public Color DefaultConnectionColor = Colors.ForestGreen;

    [Export(PropertyHint.ColorNoAlpha)]
    public Color HighlightedConnectionColor = Colors.Cyan;

    [Export]
    public NodePath? PatchNodeContainerPath;

    [Export]
    public NodePath LineContainerPath = null!;

#pragma warning disable CA2213
    [Export]
    public ShaderMaterial MonochromeMaterial = null!;
#pragma warning restore CA2213

    private readonly Dictionary<Patch, PatchMapNode> nodes = new();

    /// <summary>
    ///   The representation of connections between regions, so we won't draw the same connection multiple times
    /// </summary>
    private readonly Dictionary<Vector2I, Vector2[]> connections = new();

#pragma warning disable CA2213
    private PackedScene nodeScene = null!;
    private Control patchNodeContainer = null!;
    private Control lineContainer = null!;
#pragma warning restore CA2213

    private PatchMap map = null!;

    private bool dirty = true;

    private bool alreadyDrawn;

    private Dictionary<Patch, bool>? patchEnableStatusesToBeApplied;

    private Patch? selectedPatch;

    private Patch? playerPatch;

    [Signal]
    public delegate void OnCurrentPatchCenteredEventHandler(Vector2 coordinates, bool smoothed);

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
            MarkDirty();

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

            // Only allow selecting the patch if it is selectable
            foreach (var (patch, node) in nodes)
            {
                if (patch == value)
                {
                    if (!node.Enabled)
                    {
                        GD.Print("Not selecting map node that is not enabled");
                        return;
                    }

                    break;
                }
            }

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

        patchNodeContainer = GetNode<Control>(PatchNodeContainerPath);
        lineContainer = GetNode<Control>(LineContainerPath);

        nodeScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/PatchMapNode.tscn");

        if (DrawDefaultMapIfEmpty && Map == null)
        {
            GD.Print("Generating and showing a new patch map for testing in PatchMapDrawer");
            Map = new GameWorld(new WorldGenerationSettings()).Map;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        CheckNodeSelectionUpdate();

        if (dirty)
        {
            RebuildMap();
            QueueRedraw();

            CustomMinimumSize = GetRightBottomCornerPointOnMap() + new Vector2(450, 450);

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
        EmitSignal(SignalName.OnCurrentPatchCentered, PlayerPatch!.ScreenCoordinates, smoothed);
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

    /// <summary>
    ///   Runs a function to determine what to set as the enabled status for all patch nodes
    /// </summary>
    /// <param name="predicate">
    ///   Predicate to run on all nodes and set the result to <see cref="PatchMapNode.Enabled"/>
    /// </param>
    public void ApplyPatchNodeEnabledStatus(Func<Patch, bool> predicate)
    {
        foreach (var (patch, node) in nodes)
        {
            node.Enabled = predicate(patch);
        }
    }

    /// <summary>
    ///   Sets patch node enabled status for all nodes
    /// </summary>
    /// <param name="enabled">Value to set to <see cref="PatchMapNode.Enabled"/></param>
    public void ApplyPatchNodeEnabledStatus(bool enabled)
    {
        foreach (var (_, node) in nodes)
        {
            node.Enabled = enabled;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PatchNodeContainerPath != null)
            {
                PatchNodeContainerPath.Dispose();
                LineContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
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
        if (Math.Abs(segment1Start.X - segment1End.X) < MathUtils.EPSILON)
        {
            var segment1Greater = Math.Max(segment1Start.Y, segment1End.Y);
            var segment1Smaller = Math.Min(segment1Start.Y, segment1End.Y);

            if (Math.Abs(segment2Start.X - segment2End.X) < MathUtils.EPSILON)
            {
                var segment2Greater = Math.Max(segment2Start.Y, segment2End.Y);
                var segment2Smaller = Math.Min(segment2Start.Y, segment2End.Y);

                return Math.Abs(segment1Start.X - segment2Start.X) < MathUtils.EPSILON &&
                    !(Math.Max(segment1Smaller, segment2Smaller) - Math.Min(segment1Greater, segment2Greater) >
                        MathUtils.EPSILON);
            }
            else
            {
                if (!(Math.Abs(segment2Start.Y - segment2End.Y) < MathUtils.EPSILON))
                    throw new InvalidOperationException("Segment2 isn't parallel to axis!");

                var segment2Greater = Math.Max(segment2Start.X, segment2End.X);
                var segment2Smaller = Math.Min(segment2Start.X, segment2End.X);

                return segment1Greater - segment2Start.Y > -MathUtils.EPSILON &&
                    segment2Start.Y - segment1Smaller > -MathUtils.EPSILON &&
                    segment2Greater - segment1Start.X > -MathUtils.EPSILON &&
                    segment1Start.X - segment2Smaller > -MathUtils.EPSILON;
            }
        }
        else
        {
            if (!(Math.Abs(segment1Start.Y - segment1End.Y) < MathUtils.EPSILON))
                throw new InvalidOperationException("Segment1 isn't parallel to axis!");

            var segment1Greater = Math.Max(segment1Start.X, segment1End.X);
            var segment1Smaller = Math.Min(segment1Start.X, segment1End.X);

            if (Math.Abs(segment2Start.Y - segment2End.Y) < MathUtils.EPSILON)
            {
                var segment2Greater = Math.Max(segment2Start.X, segment2End.X);
                var segment2Smaller = Math.Min(segment2Start.X, segment2End.X);

                return Math.Abs(segment1Start.Y - segment2Start.Y) < MathUtils.EPSILON &&
                    !(Math.Max(segment1Smaller, segment2Smaller) - Math.Min(segment1Greater, segment2Greater) >
                        MathUtils.EPSILON);
            }
            else
            {
                if (!(Math.Abs(segment2Start.X - segment2End.X) < MathUtils.EPSILON))
                    throw new InvalidOperationException("Segment2 isn't parallel to axis!");

                var segment2Greater = Math.Max(segment2Start.Y, segment2End.Y);
                var segment2Smaller = Math.Min(segment2Start.Y, segment2End.Y);

                return segment1Greater - segment2Start.X > -MathUtils.EPSILON &&
                    segment2Start.X - segment1Smaller > -MathUtils.EPSILON &&
                    segment2Greater - segment1Start.Y > -MathUtils.EPSILON &&
                    segment1Start.Y - segment2Smaller > -MathUtils.EPSILON;
            }
        }
    }

    private static bool SegmentRectangleIntersects(Vector2 start, Vector2 end, Rect2 rect)
    {
        var p0 = rect.Position;
        var p1 = rect.Position + new Vector2(0, rect.Size.Y);
        var p2 = rect.Position + new Vector2(rect.Size.X, 0);
        var p3 = rect.End;

        return SegmentSegmentIntersects(p0, p1, start, end) ||
            SegmentSegmentIntersects(p0, p2, start, end) ||
            SegmentSegmentIntersects(p1, p3, start, end) ||
            SegmentSegmentIntersects(p2, p3, start, end);
    }

    private static Vector2 RegionCenter(PatchRegion region)
    {
        return new Vector2(region.ScreenCoordinates.X + region.Width * 0.5f,
            region.ScreenCoordinates.Y + region.Height * 0.5f);
    }

    private static Vector2 PatchCenter(Vector2 pos)
    {
        return new Vector2(pos.X + Constants.PATCH_NODE_RECT_LENGTH * 0.5f,
            pos.Y + Constants.PATCH_NODE_RECT_LENGTH * 0.5f);
    }

    private Line2D CreateConnectionLine(Vector2[] points, Color connectionColor)
    {
        var link = new Line2D
        {
            DefaultColor = connectionColor,
            Points = points,
            Width = Constants.PATCH_REGION_CONNECTION_LINE_WIDTH,
        };

        lineContainer.AddChild(link);

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

            point.X = Math.Max(point.X, regionEnd.X);
            point.Y = Math.Max(point.Y, regionEnd.Y);
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
                var connectionKey = new Vector2I(region.ID, adjacent.ID);
                var reverseConnectionKey = new Vector2I(adjacent.ID, region.ID);

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
        if (Math.Abs(startCenter.X - endCenter.X) < MathUtils.EPSILON ||
            Math.Abs(startCenter.Y - endCenter.Y) < MathUtils.EPSILON)
        {
            probablePaths.Add((new[] { startCenter, endCenter }, 3));
        }

        // 2-segment line, L shape
        var intermediate = new Vector2(startCenter.X, endCenter.Y);
        if (!startRect.HasPoint(intermediate) && !endRect.HasPoint(intermediate))
            probablePaths.Add((new[] { startCenter, intermediate, endCenter }, 2));

        intermediate = new Vector2(endCenter.X, startCenter.Y);
        if (!startRect.HasPoint(intermediate) && !endRect.HasPoint(intermediate))
            probablePaths.Add((new[] { startCenter, intermediate, endCenter }, 2));

        // 3-segment lines consider relative position
        var upper = startRect.Position.Y < endRect.Position.Y ? startRect : endRect;
        var lower = startRect.End.Y > endRect.End.Y ? startRect : endRect;
        var left = startRect.Position.X < endRect.Position.X ? startRect : endRect;
        var right = startRect.End.X > endRect.End.X ? startRect : endRect;

        // 3-segment line, Z shape
        var middlePoint = new Vector2(left.End.X + right.Position.X, upper.End.Y + lower.Position.Y) / 2.0f;

        var intermediate1 = new Vector2(startCenter.X, middlePoint.Y);
        var intermediate2 = new Vector2(endCenter.X, middlePoint.Y);
        if (!startRect.HasPoint(intermediate1) && !endRect.HasPoint(intermediate2))
            probablePaths.Add((new[] { startCenter, intermediate1, intermediate2, endCenter }, 1));

        intermediate1 = new Vector2(middlePoint.X, startCenter.Y);
        intermediate2 = new Vector2(middlePoint.X, endCenter.Y);
        if (!startRect.HasPoint(intermediate1) && !endRect.HasPoint(intermediate2))
            probablePaths.Add((new[] { startCenter, intermediate1, intermediate2, endCenter }, 1));

        // 3-segment line, U shape
        for (int i = 1; i <= 3; ++i)
        {
            intermediate1 = new Vector2(startCenter.X, lower.End.Y + i * 50);
            intermediate2 = new Vector2(endCenter.X, lower.End.Y + i * 50);
            probablePaths.Add((new[] { startCenter, intermediate1, intermediate2, endCenter }, -i));

            intermediate1 = new Vector2(startCenter.X, upper.Position.Y - i * 50);
            intermediate2 = new Vector2(endCenter.X, upper.Position.Y - i * 50);
            probablePaths.Add((new[] { startCenter, intermediate1, intermediate2, endCenter }, -i));

            intermediate1 = new Vector2(right.End.X + i * 50, startCenter.Y);
            intermediate2 = new Vector2(right.End.X + i * 50, endCenter.Y);
            probablePaths.Add((new[] { startCenter, intermediate1, intermediate2, endCenter }, -i));

            intermediate1 = new Vector2(left.Position.X - i * 50, startCenter.Y);
            intermediate2 = new Vector2(left.Position.X - i * 50, endCenter.Y);
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
            var connectionStartHere = connections.Where(p => p.Key.X == regionId);
            var connectionEndHere = connections.Where(p => p.Key.Y == regionId);

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
                if (Math.Abs(path[endpoint].X - path[intermediate].X) < MathUtils.EPSILON)
                {
                    connectionsToDirections[path[endpoint].Y > path[intermediate].Y ? 1 : 3].Add((
                        path, endpoint, intermediate,
                        Math.Abs(path[endpoint].Y - path[intermediate].Y)));
                }
                else
                {
                    connectionsToDirections[path[endpoint].X > path[intermediate].X ? 0 : 2].Add((
                        path, endpoint, intermediate,
                        Math.Abs(path[endpoint].X - path[intermediate].X)));
                }
            }

            var halfBorderWidth = Constants.PATCH_REGION_BORDER_WIDTH / 2;

            // Endpoint position
            foreach (var (path, endpoint, _, _) in connectionsToDirections[0])
            {
                path[endpoint].X -= region.Value.Width / 2 + halfBorderWidth;
            }

            foreach (var (path, endpoint, _, _) in connectionsToDirections[1])
            {
                path[endpoint].Y -= region.Value.Height / 2 + halfBorderWidth;
            }

            foreach (var (path, endpoint, _, _) in connectionsToDirections[2])
            {
                path[endpoint].X += region.Value.Width / 2 + halfBorderWidth;
            }

            foreach (var (path, endpoint, _, _) in connectionsToDirections[3])
            {
                path[endpoint].Y += region.Value.Height / 2 + halfBorderWidth;
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
                        if (path.Length == 2 || path[2 * intermediate - endpoint].X > path[intermediate].X)
                        {
                            path[endpoint].X += lineSeparation * right;
                            path[intermediate].X += lineSeparation * right;
                            right -= 1;
                        }
                        else
                        {
                            path[endpoint].X += lineSeparation * left;
                            path[intermediate].X += lineSeparation * left;
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
                        if (path.Length == 2 || path[2 * intermediate - endpoint].Y > path[intermediate].Y)
                        {
                            path[endpoint].Y += lineSeparation * down;
                            path[intermediate].Y += lineSeparation * down;
                            down -= 1;
                        }
                        else
                        {
                            path[endpoint].Y += lineSeparation * up;
                            path[intermediate].Y += lineSeparation * up;
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
        patchNodeContainer.FreeChildren();
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
        var node = nodeScene.Instantiate<PatchMapNode>();
        node.OffsetLeft = position.X;
        node.OffsetTop = position.Y;
        node.Size = new Vector2(Constants.PATCH_NODE_RECT_LENGTH, Constants.PATCH_NODE_RECT_LENGTH);

        node.Patch = patch;
        node.PatchIcon = patch.BiomeTemplate.LoadedIcon;

        node.MonochromeMaterial = MonochromeMaterial;

        node.SelectCallback = clicked => { SelectedPatch = clicked.Patch; };

        node.Enabled = patchEnableStatusesToBeApplied?[patch] ?? true;

        patchNodeContainer.AddChild(node);
        nodes.Add(node.Patch, node);
    }

    private void RebuildRegionConnections()
    {
        // Clear existing connection lines
        lineContainer.FreeChildren();

        foreach (var entry in connections)
        {
            var region1 = map.Regions[entry.Key.X];
            var region2 = map.Regions[entry.Key.Y];

            var visibility1 = region1.Visibility;
            var visibility2 = region2.Visibility;

            // Do not draw connections between hidden or unknown regions
            if (visibility1 != MapElementVisibility.Shown && visibility2 != MapElementVisibility.Shown)
                continue;

            // Check if connections should be highlighted
            // (Unknown patches should not highlight connections)
            var highlight = CheckHighlightedAdjacency(region1, region2) &&
                SelectedPatch?.Visibility == MapElementVisibility.Shown;

            var color = highlight ? HighlightedConnectionColor : DefaultConnectionColor;

            // Create the main connection line
            var points = entry.Value;
            var line = CreateConnectionLine(points, color);

            // Fade the connection line if need be
            ApplyFadeIfNeeded(region1, region2, line, false);
            ApplyFadeIfNeeded(region2, region1, line, true);

            // Create additional lines to connect "floating" patches in unknown regions
            if (visibility1 == MapElementVisibility.Unknown)
                BuildUnknownRegionConnections(line, region1, region2, color, false);

            if (visibility2 == MapElementVisibility.Unknown)
                BuildUnknownRegionConnections(line, region2, region1, color, true);
        }
    }

    private void BuildUnknownRegionConnections(Line2D startingConnection, PatchRegion targetRegion,
        PatchRegion startRegion, Color color, bool reversed)
    {
        var startingPoint = reversed ?
            startingConnection.Points[startingConnection.Points.Length - 1] :
            startingConnection.Points[0];

        var adjacencies = startRegion.PatchAdjacencies[targetRegion.ID];

        // Generate a list of patches to connect to
        var patches = targetRegion.Patches
            .Where(p => p.Visibility == MapElementVisibility.Unknown)
            .Where(p => adjacencies.Contains(p));

        foreach (var targetPatch in patches)
        {
            var patchSize = Vector2.One * Constants.PATCH_NODE_RECT_LENGTH;
            var endingPoint = targetPatch.ScreenCoordinates + patchSize / 2;

            // Draw a straight line if possible
            if (endingPoint.X == startingPoint.X || endingPoint.Y == startingPoint.Y)
            {
                var straightPoints = new[]
                {
                    startingPoint,
                    endingPoint,
                };

                CreateConnectionLine(straightPoints, color);
                continue;
            }

            var intermediate = new Vector2(endingPoint.X, startingPoint.Y);

            // Make sure the new point is not covered by the patch itself
            var targetPatchRect = new Rect2(targetPatch.ScreenCoordinates, patchSize);
            if (targetPatchRect.HasPoint(intermediate))
                intermediate = new Vector2(startingPoint.X, endingPoint.Y);

            var points = new[]
            {
                startingPoint,
                intermediate,
                endingPoint,
            };

            CreateConnectionLine(points, color);
        }
    }

    private void ApplyFadeIfNeeded(PatchRegion startingRegion, PatchRegion endingRegion,
        Line2D line, bool reversed)
    {
        // Do not apply fade from hidden or unknown region
        if (startingRegion.Visibility != MapElementVisibility.Shown)
            return;

        // Do not apply fade if target region is visible
        if (endingRegion.Visibility == MapElementVisibility.Shown)
            return;

        // Apply fade if target region is hidden
        if (endingRegion.Visibility == MapElementVisibility.Hidden)
        {
            ApplyFadeToLine(line, reversed);
            return;
        }

        // Apply fade only if no connecting patches are visible in the target region
        var adjacencies = startingRegion.PatchAdjacencies[endingRegion.ID];

        if (!endingRegion.Patches
                .Where(p => p.Visibility == MapElementVisibility.Unknown)
                .Any(p => adjacencies.Contains(p)))
        {
            ApplyFadeToLine(line, reversed);
        }
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

    private void CheckNodeSelectionUpdate()
    {
        bool needsUpdate = false;

        foreach (var node in nodes.Values)
        {
            if (node.SelectionDirty)
            {
                needsUpdate = true;
                break;
            }
        }

        if (needsUpdate)
        {
            UpdateNodeSelections();

            foreach (var node in nodes.Values)
            {
                if (SelectedPatch == null)
                    node.AdjacentToSelectedPatch = false;

                node.UpdateSelectionState();
            }

            // Also needs to update the lines connecting patches for those to display properly
            // TODO: would be really nice to be able to just update the line objects without redoing them all
            RebuildRegionConnections();
        }
    }
}
