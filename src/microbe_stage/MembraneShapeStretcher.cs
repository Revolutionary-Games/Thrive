using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Stretches colony cell membranes towards shared boundaries.
/// </summary>
public static class MembraneShapeStretcher
{
    /// <summary>
    ///   Smooths every vertex by averaging it with its two neighbouring vertices.
    ///   The polygon is treated as closed, so the first and last vertices are neighbours.
    /// </summary>
    public static void SmoothVertexRegion(CellData cellData)
    {
        var vertexCount = cellData.VertexCount;

        if (vertexCount == 0)
            return;

        var firstPoint = cellData.Vertices2D[0];
        var previousPoint = cellData.Vertices2D[vertexCount - 1];

        // Preserve original neighbours while writing the smoothed positions in place.
        for (int i = 0; i < vertexCount; ++i)
        {
            var currentPoint = cellData.Vertices2D[i];
            var nextPoint = i + 1 < vertexCount ? cellData.Vertices2D[i + 1] : firstPoint;

            cellData.Vertices2D[i] = (previousPoint + currentPoint * 3 + nextPoint) * 0.2f;
            previousPoint = currentPoint;
        }
    }

    /// <summary>
    ///   Converts the discrete cell orientation into a rotation angle in radians.
    /// </summary>
    public static float GetOrientationAngle(int orientation)
    {
        return orientation * MathUtils.SIXTY_DEGREES_ANGLE;
    }

    /// <summary>
    ///   Transforms membrane vertices and their average from cell-local coordinates into colony coordinates.
    /// </summary>
    public static void RotateCellVertices(CellData cellData)
    {
        float orientationAngle = GetOrientationAngle(cellData.MulticellularMembraneGenerationCellData.Orientation);
        (float sin, float cos) = MathF.SinCos(orientationAngle);

        Vector2 position = cellData.MulticellularMembraneGenerationCellData.Position;

        for (int i = 0; i < cellData.VertexCount; ++i)
        {
            cellData.Vertices2D[i] = MathUtils.RotatePoint(cellData.Vertices2D[i], sin, cos) + position;
        }

        cellData.AverageVertex = MathUtils.RotatePoint(cellData.AverageVertex, sin, cos) + position;
    }

    /// <summary>
    ///   Transforms membrane vertices and their average from colony coordinates back into cell-local coordinates.
    /// </summary>
    public static void UnrotateCellVertices(CellData cellData)
    {
        float orientationAngle = GetOrientationAngle(cellData.MulticellularMembraneGenerationCellData.Orientation);
        (float sin, float cos) = MathF.SinCos(-orientationAngle);

        Vector2 position = cellData.MulticellularMembraneGenerationCellData.Position;

        for (int i = 0; i < cellData.VertexCount; ++i)
        {
            cellData.Vertices2D[i] = MathUtils.RotatePoint(cellData.Vertices2D[i] - position, sin, cos);
        }

        cellData.AverageVertex = MathUtils.RotatePoint(cellData.AverageVertex - position, sin, cos);
    }

    /// <summary>
    ///   Calculates visible intersections between cell lines and removes
    ///   duplicate intersection positions within the configured tolerance.
    ///   Intersections beyond the maximum stretching distance from the cell position are discarded.
    /// </summary>
    public static void GetIntersections(CellData cell)
    {
        var lines = cell.LinesData;
        var intersections = cell.IntersectionsData;
        var position = cell.MulticellularMembraneGenerationCellData.Position;
        var maxDistance = Constants.MEMBRANE_MAX_STRETCHED_DISTANCE_SQUARED * 0.5f * 0.5f;
        var averageVertex = cell.AverageVertex;

        intersections.Clear();

        for (int i = 0; i < lines.Count; ++i)
        {
            LineData firstLine = lines[i];

            for (int j = i + 1; j < lines.Count; ++j)
            {
                LineData secondLine = lines[j];

                if (!TryGetIntersection(firstLine, secondLine, out Vector2 intersection))
                    continue;

                if (position.DistanceSquaredTo(intersection) > maxDistance)
                    continue;

                bool isVisible = true;

                foreach (LineData clippingLine in lines)
                {
                    float averageSide = SignedSquaredDistanceToLine(clippingLine, averageVertex);
                    float intersectionSide = SignedSquaredDistanceToLine(clippingLine, intersection);

                    bool isSeparated =
                        (averageSide > Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED &&
                            intersectionSide < -Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED) ||
                        (averageSide < -Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED &&
                            intersectionSide > Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED);

                    if (isSeparated)
                    {
                        isVisible = false;
                        break;
                    }
                }

                if (!isVisible)
                    continue;

                bool alreadyExists = false;

                for (int k = 0; k < intersections.Count; ++k)
                {
                    if (intersections[k].DistanceSquaredTo(intersection) <=
                        Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (alreadyExists)
                    continue;

                intersections.Add(intersection);
            }
        }
    }

    /// <summary>
    ///   Removes intersections that the neighbouring cells do not agree on.
    ///   A corner of cell A built from lines to B and C is kept only when every pair of
    ///   cells meeting at that point (B, C) is mutually connected by lines.
    ///   Requires CreateBoundaryLines to have run, so that line relations are symmetric.
    /// </summary>
    public static void FilterInconsistentIntersections(Dictionary<long, CellData> cellsData,
        List<CellData> neighbouringCells)
    {
        neighbouringCells.Clear();

        foreach (CellData cell in cellsData.Values)
        {
            var intersections = cell.IntersectionsData;
            int consistentCount = 0;

            for (int i = 0; i < intersections.Count; ++i)
            {
                var intersection = intersections[i];
                if (IsIntersectionConsistent(cell, intersection, cellsData, neighbouringCells))
                    intersections[consistentCount++] = intersection;
            }

            if (consistentCount < intersections.Count)
                intersections.RemoveRange(consistentCount, intersections.Count - consistentCount);
        }
    }

    /// <summary>
    ///   Finds the closest points on two finite line segments and returns their
    ///   squared distance. The output points lie on their respective segments.
    /// </summary>
    public static float ClosestPointsOnSegments(Vector2 firstSegmentStart, Vector2 firstSegmentEnd,
        Vector2 secondSegmentStart, Vector2 secondSegmentEnd, out Vector2 closestPointOnFirstSegment,
        out Vector2 closestPointOnSecondSegment)
    {
        var firstSegmentDirection = firstSegmentEnd - firstSegmentStart;
        var secondSegmentDirection = secondSegmentEnd - secondSegmentStart;
        var startPointOffset = firstSegmentStart - secondSegmentStart;

        var firstSegmentLengthSquared = firstSegmentDirection.LengthSquared();
        var secondSegmentLengthSquared = secondSegmentDirection.LengthSquared();
        var secondDirectionOffsetDot = secondSegmentDirection.Dot(startPointOffset);

        float firstSegmentParameter;
        float secondSegmentParameter;

        if (firstSegmentLengthSquared <= MathUtils.EPSILON && secondSegmentLengthSquared <= MathUtils.EPSILON)
        {
            closestPointOnFirstSegment = firstSegmentStart;
            closestPointOnSecondSegment = secondSegmentStart;
            return closestPointOnFirstSegment.DistanceSquaredTo(closestPointOnSecondSegment);
        }

        if (firstSegmentLengthSquared <= MathUtils.EPSILON)
        {
            firstSegmentParameter = 0.0f;
            secondSegmentParameter = Math.Clamp(secondDirectionOffsetDot / secondSegmentLengthSquared, 0.0f, 1.0f);
        }
        else
        {
            var firstDirectionOffsetDot = firstSegmentDirection.Dot(startPointOffset);

            if (secondSegmentLengthSquared <= MathUtils.EPSILON)
            {
                secondSegmentParameter = 0.0f;
                firstSegmentParameter = Math.Clamp(-firstDirectionOffsetDot / firstSegmentLengthSquared, 0.0f, 1.0f);
            }
            else
            {
                var directionDotProduct = firstSegmentDirection.Dot(secondSegmentDirection);
                var denominator = firstSegmentLengthSquared * secondSegmentLengthSquared -
                    directionDotProduct * directionDotProduct;

                firstSegmentParameter = denominator > MathUtils.EPSILON ?
                    Math.Clamp((directionDotProduct * secondDirectionOffsetDot -
                        firstDirectionOffsetDot * secondSegmentLengthSquared) / denominator, 0.0f, 1.0f) :
                    0.0f;

                secondSegmentParameter = (directionDotProduct * firstSegmentParameter + secondDirectionOffsetDot) /
                    secondSegmentLengthSquared;

                if (secondSegmentParameter < 0.0f)
                {
                    secondSegmentParameter = 0.0f;
                    firstSegmentParameter =
                        Math.Clamp(-firstDirectionOffsetDot / firstSegmentLengthSquared, 0.0f, 1.0f);
                }
                else if (secondSegmentParameter > 1.0f)
                {
                    secondSegmentParameter = 1.0f;
                    firstSegmentParameter =
                        Math.Clamp((directionDotProduct - firstDirectionOffsetDot) / firstSegmentLengthSquared, 0.0f,
                            1.0f);
                }
            }
        }

        closestPointOnFirstSegment = firstSegmentStart + firstSegmentDirection * firstSegmentParameter;
        closestPointOnSecondSegment = secondSegmentStart + secondSegmentDirection * secondSegmentParameter;

        return closestPointOnFirstSegment.DistanceSquaredTo(closestPointOnSecondSegment);
    }

    /// <summary>
    ///   Creates shared boundary lines between nearby cells, retains mutually visible neighbours,
    ///   and removes boundaries for overlapping cells.
    /// </summary>
    public static void CreateBoundaryLines(Dictionary<long, CellData> cellsData)
    {
        var maxDistanceSquared = Constants.MEMBRANE_MAX_STRETCHED_DISTANCE_SQUARED;

        foreach (var (cellKeyA, cellA) in cellsData)
        {
            var countA = cellA.VertexCount;
            if (countA < 2)
                continue;

            var positionA = cellA.MulticellularMembraneGenerationCellData.Position;
            var verticesA = cellA.Vertices2D;

            foreach (var (cellKeyB, cellB) in cellsData)
            {
                // Makes sure we don't process the same pair twice, and avoids self-comparison.
                if (cellKeyA >= cellKeyB)
                    continue;

                var positionB = cellB.MulticellularMembraneGenerationCellData.Position;

                if (positionA.DistanceSquaredTo(positionB) > maxDistanceSquared)
                {
                    continue;
                }

                var countB = cellB.VertexCount;
                if (countB < 2)
                    continue;

                var verticesB = cellB.Vertices2D;

                float minDistance = float.MaxValue;

                Vector2 closestA = Vector2.Zero;
                Vector2 closestB = Vector2.Zero;

                FindClosestPointsOnMembranes(countA, verticesA, countB, verticesB, ref minDistance, ref closestA,
                    ref closestB);

                if (minDistance == float.MaxValue)
                    continue;

                var middlePoint = (closestA + closestB) * 0.5f;
                var isOverlapping = false;

                if (IsPointInsideConvexPolygon(middlePoint, cellA.Vertices2D) ||
                    IsPointInsideConvexPolygon(middlePoint, cellB.Vertices2D))
                {
                    closestB = cellB.AverageVertex;
                    closestA = cellA.AverageVertex;
                    isOverlapping = true;
                }

                CreateBoundaryLinesForNeighbours(closestB, closestA, cellA, cellKeyB, cellB, cellKeyA, isOverlapping);
            }
        }

        foreach (var (cellKey, cell) in cellsData)
        {
            List<LineData> candidates = cell.LinesData;

            Vector2 averageVertex = cell.AverageVertex;

            for (int candidateIndex = 0;
                 candidateIndex < candidates.Count;
                 candidateIndex++)
            {
                LineData candidate = candidates[candidateIndex];

                bool isVisible = true;

                for (int otherIndex = 0;
                     otherIndex < candidates.Count;
                     otherIndex++)
                {
                    if (candidateIndex == otherIndex)
                        continue;

                    LineData otherLine = candidates[otherIndex];

                    float averageSide = SignedSquaredDistanceToLine(otherLine, averageVertex);
                    float candidateSide = SignedSquaredDistanceToLine(otherLine, candidate.MiddlePoint);

                    bool isSeparated =
                        (averageSide > Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED &&
                            candidateSide < -Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED) ||
                        (averageSide < -Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED &&
                            candidateSide > Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED);

                    if (isSeparated)
                    {
                        isVisible = false;
                        break;
                    }
                }

                // Remove only when the candidate is not visible to both cells
                candidate.ShouldRemove &= !isVisible;
                candidates[candidateIndex] = candidate;

                if (!isVisible)
                    continue;

                if (cellsData.TryGetValue(candidate.OtherCellKey, out CellData? neighbour))
                {
                    List<LineData> neighbourLines = neighbour.LinesData;

                    for (int i = 0; i < neighbourLines.Count; ++i)
                    {
                        LineData neighbourLine = neighbourLines[i];

                        if (neighbourLine.OtherCellKey == cellKey)
                        {
                            neighbourLine.ShouldRemove = false;
                            neighbourLines[i] = neighbourLine;
                            break;
                        }
                    }
                }
            }
        }

        foreach (var cellData in cellsData.Values)
        {
            List<LineData> cellLines = cellData.LinesData;
            int retainedCount = 0;

            for (int i = 0; i < cellLines.Count; ++i)
            {
                LineData line = cellLines[i];

                if (!line.ShouldRemove && !line.IsOverLapping)
                    cellLines[retainedCount++] = line;
            }

            if (retainedCount < cellLines.Count)
                cellLines.RemoveRange(retainedCount, cellLines.Count - retainedCount);
        }
    }

    /// <summary>
    ///   Checks whether a point lies inside or on the boundary of a convex polygon, regardless of winding order.
    /// </summary>
    public static bool IsPointInsideConvexPolygon(Vector2 point, List<Vector2> vectices2D)
    {
        bool sign = false;
        bool hasSign = false;

        for (int i = 0; i < vectices2D.Count; ++i)
        {
            Vector2 a = vectices2D[i];
            Vector2 b = vectices2D[(i + 1) % vectices2D.Count];

            float cross = (b - a).Cross(point - a);

            if (Math.Abs(cross) < 0.0001f)
                continue;

            bool currentSign = cross > 0;

            if (hasSign && sign != currentSign)
                return false;

            sign = currentSign;
            hasSign = true;
        }

        return true;
    }

    /// <summary>
    ///   Builds the visible boundary paths from the cell polygon, line midpoints,
    ///   and accepted intersections.
    /// </summary>
    public static void BuildBoundaryPaths(CellData cell, List<Vector2> points, List<bool> validEdges,
        List<Vector2> currentPath, List<Vector2> boundaryPoints, List<BoundaryPathRange> boundaryPaths,
        BoundaryPointComparer pointComparer)
    {
        if (cell.Vertices2D.Count < 3)
            throw new InvalidOperationException("Membrane needs at least 3 vertices.");

        points.Clear();
        validEdges.Clear();
        currentPath.Clear();
        boundaryPoints.Clear();
        boundaryPaths.Clear();

        points.AddRange(cell.IntersectionsData);

        foreach (LineData line in cell.LinesData)
        {
            points.Add(line.MiddlePoint);
        }

        points = RemoveDuplicatePoints(points);

        if (points.Count == 0)
            return;

        pointComparer.Centre = cell.AverageVertex;
        points.Sort(pointComparer.Comparison);

        // One point -> two tangents
        if (points.Count == 1)
        {
            AddSinglePointPath(points[0], cell.Vertices2D, boundaryPoints, boundaryPaths);
            return;
        }

        for (int i = 0; i < points.Count; ++i)
        {
            int next = (i + 1) % points.Count;

            validEdges.Add(CanConnect(points[i], points[next], cell.Vertices2D));
        }

        if (points.Count == 2 && validEdges[0] && validEdges[1])
        {
            AddOpenPath(points, cell.Vertices2D, boundaryPoints, boundaryPaths);
            return;
        }

        // Edge is not valid if it crosses the membrane or if the membrane is not
        // entirely on the right side of the edge.
        bool allEdgesValid = true;

        for (int edgeIndex = 0; edgeIndex < validEdges.Count; ++edgeIndex)
        {
            if (validEdges[edgeIndex])
                continue;

            allEdgesValid = false;
            break;
        }

        if (allEdgesValid)
        {
            boundaryPoints.AddRange(points);
            boundaryPoints.Add(points[0]);
            boundaryPaths.Add(new BoundaryPathRange(0, boundaryPoints.Count));
            return;
        }

        int firstInvalidEdge = validEdges.FindIndex(x => !x);
        int startIndex = (firstInvalidEdge + 1) % points.Count;

        for (int i = 0; i < points.Count; ++i)
        {
            int currentIndex = (startIndex + i) % points.Count;

            currentPath.Add(points[currentIndex]);

            if (i == points.Count - 1)
            {
                AddOpenPath(currentPath, cell.Vertices2D, boundaryPoints, boundaryPaths);
                break;
            }

            if (!validEdges[currentIndex])
            {
                AddOpenPath(currentPath, cell.Vertices2D, boundaryPoints, boundaryPaths);
                currentPath.Clear();
            }
        }
    }

    /// <summary>
    ///   Projects membrane vertices onto the nearest valid boundary-path intersection
    ///   along the ray starting at the cell's average vertex.
    /// </summary>
    public static void CastMembranePoints(CellData cell, List<Vector2> boundaryPoints,
        List<BoundaryPathRange> boundaryPaths)
    {
        for (int i = 0; i < cell.VertexCount; ++i)
        {
            Vector2 membranePoint = cell.Vertices2D[i];

            if (!TryCastMembranePoint(membranePoint, cell.AverageVertex, boundaryPoints, boundaryPaths,
                    out Vector2 hitPoint))
            {
                continue;
            }

            cell.Vertices2D[i] = hitPoint;
        }
    }

    /// <summary>
    ///   Finds the intersection of two infinite boundary lines, rejecting degenerate or nearly parallel lines.
    /// </summary>
    private static bool TryGetIntersection(LineData firstLine, LineData secondLine, out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        var firstLineMiddlePoint = firstLine.MiddlePoint;
        var secondLineMiddlePoint = secondLine.MiddlePoint;

        var firstLineDirection = firstLine.Line;
        var secondLineDirection = secondLine.Line;

        var directionCrossProduct =
            firstLineDirection.X * secondLineDirection.Y - firstLineDirection.Y * secondLineDirection.X;

        // Parallel or almost parallel lines
        var directionLengthProductSquared = firstLineDirection.LengthSquared() * secondLineDirection.LengthSquared();
        if (directionLengthProductSquared < MathUtils.EPSILON ||
            MathF.Abs(directionCrossProduct) < MathUtils.EPSILON * directionLengthProductSquared)
        {
            return false;
        }

        var middlePointsDifference = secondLineMiddlePoint - firstLineMiddlePoint;

        var intersectionParameter =
            (middlePointsDifference.X * secondLineDirection.Y - middlePointsDifference.Y * secondLineDirection.X) /
            directionCrossProduct;

        intersection = firstLineMiddlePoint + intersectionParameter * firstLineDirection;
        return true;
    }

    /// <summary>
    ///   Calculates squared perpendicular distance to a boundary line, retaining the sign defined by its normal.
    /// </summary>
    private static float SignedSquaredDistanceToLine(LineData line, Vector2 point)
    {
        var lengthSquared = line.Normal.LengthSquared();

        if (lengthSquared < MathUtils.EPSILON * MathUtils.EPSILON)
            return 0.0f;

        float projection = line.Normal.Dot(point - line.MiddlePoint);
        return projection * MathF.Abs(projection) / lengthSquared;
    }

    /// <summary>
    ///   Checks whether all cells meeting at <paramref name="position"/> are neighbours of each other.
    /// </summary>
    private static bool IsIntersectionConsistent(CellData cell, Vector2 position,
        Dictionary<long, CellData> cellsData, List<CellData> meetingCells)
    {
        meetingCells.Clear();

        foreach (LineData line in cell.LinesData)
        {
            if (MathF.Abs(SignedSquaredDistanceToLine(line, position)) >
                Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED)
            {
                continue;
            }

            if (!cellsData.TryGetValue(line.OtherCellKey, out CellData? other))
                return false;

            meetingCells.Add(other);
        }

        if (meetingCells.Count < 2)
            return false;

        for (int i = 0; i < meetingCells.Count; ++i)
        {
            for (int j = i + 1; j < meetingCells.Count; ++j)
            {
                if (!HasLineTo(meetingCells[i], meetingCells[j].CellKey) ||
                    !HasLineTo(meetingCells[j], meetingCells[i].CellKey))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///   Checks whether a cell has a boundary line associated with the specified neighbouring cell.
    /// </summary>
    private static bool HasLineTo(CellData cell, long otherCellKey)
    {
        List<LineData> lines = cell.LinesData;

        for (int i = 0; i < lines.Count; ++i)
        {
            if (lines[i].OtherCellKey == otherCellKey)
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Adds the same perpendicular bisector to both neighbouring cells and records their overlap status.
    /// </summary>
    private static void CreateBoundaryLinesForNeighbours(Vector2 pointB, Vector2 pointA, CellData cellA, long cellKeyB,
        CellData cellB, long cellKeyA, bool isOverlapping)
    {
        Vector2 normal = pointB - pointA;

        if (normal.LengthSquared() < MathUtils.EPSILON)
            return;

        Vector2 middlePoint = (pointA + pointB) * 0.5f;

        cellA.LinesData.Add(new LineData(middlePoint, normal, cellKeyB, isOverlapping));
        cellB.LinesData.Add(new LineData(middlePoint, normal, cellKeyA, isOverlapping));
    }

    /// <summary>
    ///   Searches all pairs of membrane edges and updates the closest points and minimum squared distance.
    /// </summary>
    private static void FindClosestPointsOnMembranes(int countA, List<Vector2> verticesA, int countB,
        List<Vector2> verticesB, ref float minDistance, ref Vector2 closestA, ref Vector2 closestB)
    {
        for (int i = 0; i < countA; ++i)
        {
            Vector2 a0 = verticesA[i];
            Vector2 a1 = verticesA[(i + 1) % countA];

            for (int j = 0; j < countB; ++j)
            {
                Vector2 b0 = verticesB[j];
                Vector2 b1 = verticesB[(j + 1) % countB];

                float distance = ClosestPointsOnSegments(a0, a1, b0, b1, out Vector2 pointA, out Vector2 pointB);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestA = pointA;
                    closestB = pointB;
                }
            }
        }
    }

    /// <summary>
    ///   Finds the nearest boundary hit from a membrane point in the direction away from the cell average.
    /// </summary>
    private static bool TryCastMembranePoint(Vector2 membranePoint, Vector2 averagePoint, List<Vector2> boundaryPoints,
        List<BoundaryPathRange> boundaryPaths, out Vector2 hitPoint)
    {
        hitPoint = Vector2.Zero;

        Vector2 toPoint = membranePoint - averagePoint;
        float toPointLengthSquared = toPoint.LengthSquared();

        if (toPointLengthSquared < Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED)
            return false;

        Vector2 direction = toPoint / MathF.Sqrt(toPointLengthSquared);

        float closestDistance = float.MaxValue;
        bool found = false;

        foreach (var path in boundaryPaths)
        {
            int end = path.Start + path.Count - 1;
            for (int i = path.Start; i < end; ++i)
            {
                Vector2 a = boundaryPoints[i];
                Vector2 b = boundaryPoints[i + 1];

                if (!TryRaySegmentIntersection(membranePoint, direction, a, b, out Vector2 intersection))
                {
                    continue;
                }

                // The direction is normalised, so the projection is a signed distance in world units.
                float distance = (intersection - membranePoint).Dot(direction);

                if (distance < -Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE || distance >= closestDistance)
                    continue;

                closestDistance = distance;
                hitPoint = intersection;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    ///   Finds a ray intersection with a finite segment, allowing endpoint and origin deviations within tolerance.
    /// </summary>
    private static bool TryRaySegmentIntersection(Vector2 rayOrigin, Vector2 rayDirection, Vector2 segmentStart,
        Vector2 segmentEnd, out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        Vector2 segmentDirection = segmentEnd - segmentStart;
        float segmentLengthSquared = segmentDirection.LengthSquared();

        if (segmentLengthSquared < Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED)
            return false;

        Vector2 offset = segmentStart - rayOrigin;

        float denominator = rayDirection.Cross(segmentDirection);

        if (denominator * denominator < MathUtils.EPSILON * MathUtils.EPSILON * segmentLengthSquared)
            return false;

        float t = offset.Cross(segmentDirection) / denominator;
        float u = offset.Cross(rayDirection) / denominator;

        if (t < -Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE)
            return false;

        // Measure endpoint overshoot in squared world units without normalising the segment.
        float endpointOffset = u < 0.0f ? u : MathF.Max(u - 1.0f, 0.0f);
        if (endpointOffset * endpointOffset * segmentLengthSquared >
            Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED)
        {
            return false;
        }

        intersection = rayOrigin + rayDirection * t;
        return true;
    }

    /// <summary>
    ///   Appends a boundary path through one external point, joined to the polygon by incoming and outgoing tangents.
    /// </summary>
    private static void AddSinglePointPath(Vector2 point, List<Vector2> polygon, List<Vector2> boundaryPoints,
        List<BoundaryPathRange> boundaryPaths)
    {
        Vector2 incomingTangent = GetIncomingTangent(point, polygon);
        Vector2 outgoingTangent = GetOutgoingTangent(point, polygon);

        int start = boundaryPoints.Count;
        boundaryPoints.Add(incomingTangent);
        boundaryPoints.Add(point);
        boundaryPoints.Add(outgoingTangent);
        boundaryPaths.Add(new BoundaryPathRange(start, 3));
    }

    /// <summary>
    ///   Appends an open boundary path with tangent connections to the polygon at both ends.
    /// </summary>
    private static void AddOpenPath(List<Vector2> points, List<Vector2> polygon, List<Vector2> boundaryPoints,
        List<BoundaryPathRange> result)
    {
        if (points.Count == 0)
            return;

        Vector2 incomingTangent = GetIncomingTangent(points[0], polygon);
        Vector2 outgoingTangent = GetOutgoingTangent(points[^1], polygon);

        int start = boundaryPoints.Count;
        boundaryPoints.Add(incomingTangent);
        boundaryPoints.AddRange(points);
        boundaryPoints.Add(outgoingTangent);

        result.Add(new BoundaryPathRange(start, boundaryPoints.Count - start));
    }

    /// <summary>
    ///   Checks whether a directed boundary edge avoids the polygon interior and keeps the polygon on its right.
    /// </summary>
    private static bool CanConnect(Vector2 a, Vector2 b, List<Vector2> polygon)
    {
        if (SegmentEntersPolygonInterior(a, b, polygon))
            return false;

        return IsPolygonOnRightSide(a, b, polygon);
    }

    /// <summary>
    ///   Checks whether all polygon vertices lie on or to the right of a directed line, within tolerance.
    /// </summary>
    private static bool IsPolygonOnRightSide(Vector2 a, Vector2 b, List<Vector2> polygon)
    {
        foreach (Vector2 vertex in polygon)
        {
            if (SignedSquaredDistanceToDirectedLine(a, b, vertex) > Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED)
                return false;
        }

        return true;
    }

    /// <summary>
    ///   Determines whether a segment passes through the interior of a convex polygon
    ///   using parametric clipping.
    /// </summary>
    private static bool SegmentEntersPolygonInterior(Vector2 segmentStart,
        Vector2 segmentEnd,
        List<Vector2> polygon)
    {
        Vector2 segmentDirection = segmentEnd - segmentStart;
        float segmentLengthSquared = segmentDirection.LengthSquared();

        if (segmentLengthSquared < Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED)
            return false;

        float minimumSegmentParameter = 0.0f;
        float maximumSegmentParameter = 1.0f;

        const float interiorLimit = -Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE;

        for (int edgeIndex = 0; edgeIndex < polygon.Count; ++edgeIndex)
        {
            Vector2 edgeStart = polygon[edgeIndex];
            Vector2 edgeEnd = polygon[(edgeIndex + 1) % polygon.Count];
            Vector2 edgeDirection = edgeEnd - edgeStart;

            float edgeLengthSquared = edgeDirection.LengthSquared();

            // Duplicate vertex - this edge provides no useful information.
            if (edgeLengthSquared < MathUtils.EPSILON * MathUtils.EPSILON)
                continue;

            float edgeLength = MathF.Sqrt(edgeLengthSquared);

            // Signed distance from the segment start to the edge line.
            // Positive means the point is on the left side.
            float initialSignedDistance =
                MathUtils.Cross(edgeDirection, segmentStart - edgeStart) / edgeLength;

            // Change in signed distance over the entire segment (parameter 0 -> 1).
            float signedDistanceChange =
                MathUtils.Cross(edgeDirection, segmentDirection) / edgeLength;

            if (MathF.Abs(signedDistanceChange) < MathUtils.EPSILON)
            {
                if (initialSignedDistance > interiorLimit)
                    return false;

                continue;
            }

            float intersectionParameter =
                (interiorLimit - initialSignedDistance) / signedDistanceChange;

            if (signedDistanceChange > 0)
            {
                maximumSegmentParameter = MathF.Min(maximumSegmentParameter,
                    intersectionParameter);
            }
            else
            {
                minimumSegmentParameter = MathF.Max(minimumSegmentParameter,
                    intersectionParameter);
            }

            if (minimumSegmentParameter >= maximumSegmentParameter)
                return false;
        }

        // Squared length of the segment portion inside the polygon.
        float interiorParameterRange = maximumSegmentParameter - minimumSegmentParameter;
        float interiorSegmentLengthSquared = interiorParameterRange * interiorParameterRange * segmentLengthSquared;

        return interiorSegmentLengthSquared > Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED;
    }

    /// <summary>
    ///   Finds the polygon vertex forming an incoming tangent to the supplied external point.
    /// </summary>
    private static Vector2 GetIncomingTangent(Vector2 point, List<Vector2> polygon)
    {
        return FindTangent(point, polygon, true);
    }

    /// <summary>
    ///   Finds the polygon vertex forming an outgoing tangent from the supplied external point.
    /// </summary>
    private static Vector2 GetOutgoingTangent(Vector2 point, List<Vector2> polygon)
    {
        return FindTangent(point, polygon, false);
    }

    /// <summary>
    ///   Finds the nearest polygon vertex that forms a valid incoming or outgoing
    ///   tangent from a point outside the polygon.
    /// </summary>
    private static Vector2 FindTangent(Vector2 point, List<Vector2> polygon, bool incoming)
    {
#if DEBUG
        if (IsPointInsideOrOnPolygon(point, polygon))
        {
            GD.PrintErr($"Point {point} is inside or on the polygon. " +
                "Cannot find an external tangent.");
        }
#endif

        Vector2 bestCandidate = default(Vector2);
        bool hasBestCandidate = false;
        float bestDistance = float.MaxValue;

        foreach (Vector2 tangentPoint in polygon)
        {
            bool isValid = true;

            foreach (Vector2 vertex in polygon)
            {
                float side = incoming ?
                    SignedSquaredDistanceToDirectedLine(tangentPoint, point, vertex) :
                    SignedSquaredDistanceToDirectedLine(point, tangentPoint, vertex);

                if (side > Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED)
                {
                    isValid = false;
                    break;
                }
            }

            if (!isValid)
                continue;

            float distance = point.DistanceSquaredTo(tangentPoint);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCandidate = tangentPoint;
                hasBestCandidate = true;
            }
        }

        if (!hasBestCandidate)
        {
#if DEBUG
            GD.PrintErr($"Could not find an external tangent for point {point}.");
#endif
            return point;
        }

        return bestCandidate;
    }

    /// <summary>
    ///   Checks whether a point lies inside or on a clockwise convex polygon, within the geometric tolerance.
    /// </summary>
    private static bool IsPointInsideOrOnPolygon(Vector2 point,
        List<Vector2> polygon)
    {
        for (int i = 0; i < polygon.Count; ++i)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];

            if (SignedSquaredDistanceToDirectedLine(a, b, point) > Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED)
                return false;
        }

        return true;
    }

    /// <summary>
    ///   Removes points that are closer to an already accepted point
    ///   than the configured geometric tolerance.
    /// </summary>
    private static List<Vector2> RemoveDuplicatePoints(List<Vector2> points)
    {
        int uniquePointCount = 0;

        for (int pointIndex = 0; pointIndex < points.Count; ++pointIndex)
        {
            Vector2 candidatePoint = points[pointIndex];
            bool isDuplicate = false;

            for (int existingPointIndex = 0; existingPointIndex < uniquePointCount; ++existingPointIndex)
            {
                if (points[existingPointIndex].DistanceSquaredTo(candidatePoint) <=
                    Constants.MEMBRANE_POINT_DISTANCE_TOLERANCE_SQUARED)
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (isDuplicate)
                continue;

            points[uniquePointCount] = candidatePoint;
            ++uniquePointCount;
        }

        if (uniquePointCount < points.Count)
            points.RemoveRange(uniquePointCount, points.Count - uniquePointCount);

        return points;
    }

    /// <summary>
    ///   Calculates squared perpendicular distance to a directed line, retaining a positive sign on its left side.
    /// </summary>
    private static float SignedSquaredDistanceToDirectedLine(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 direction = b - a;
        float lengthSquared = direction.LengthSquared();

        if (lengthSquared < MathUtils.EPSILON * MathUtils.EPSILON)
            return 0.0f;

        float cross = MathUtils.Cross(direction, p - a);
        return cross * MathF.Abs(cross) / lengthSquared;
    }

    /// <summary>
    ///   Sorts boundary points clockwise around a cell's centre.
    /// </summary>
    public class BoundaryPointComparer
    {
        public BoundaryPointComparer()
        {
            Comparison = Compare;
        }

        public Vector2 Centre { get; set; }

        public Comparison<Vector2> Comparison { get; }

        /// <summary>
        ///   Compares polar angles in descending order to sort points clockwise around the configured centre.
        /// </summary>
        private int Compare(Vector2 firstPoint, Vector2 secondPoint)
        {
            float firstAngle = MathF.Atan2(firstPoint.Y - Centre.Y, firstPoint.X - Centre.X);
            float secondAngle = MathF.Atan2(secondPoint.Y - Centre.Y, secondPoint.X - Centre.X);
            return secondAngle.CompareTo(firstAngle);
        }
    }

    /// <summary>
    ///   Stores the generated geometry and related data for one cell.
    /// </summary>
    public sealed class CellData
    {
        public readonly List<Vector2> Vertices2D;
        public readonly List<Vector2> IntersectionsData;
        public readonly List<LineData> LinesData;

        public Vector2 AverageVertex;

        public CellData(long cellKey,
            MembranePointData membranePointData,
            MulticellularMembraneGenerationCellData generationData)
        {
            Vertices2D = new List<Vector2>(membranePointData.VertexCount);
            LinesData = new List<LineData>();
            IntersectionsData = new List<Vector2>();

            Initialise(cellKey, membranePointData, generationData);
        }

        public MembranePointData OriginalPointData { get; private set; } = null!;

        public MulticellularMembraneGenerationCellData MulticellularMembraneGenerationCellData { get; private set; }

        public long CellKey { get; private set; }

        public int VertexCount { get; private set; }

        /// <summary>
        ///   Reuses the working buffers for a cell. Must be called before using an instance returned to the pool.
        /// </summary>
        public void Initialise(long cellKey, MembranePointData membranePointData,
            MulticellularMembraneGenerationCellData generationData)
        {
            CellKey = cellKey;
            OriginalPointData = membranePointData;
            MulticellularMembraneGenerationCellData = generationData;
            VertexCount = membranePointData.VertexCount;

            Vertices2D.Clear();
            LinesData.Clear();
            IntersectionsData.Clear();
            Vertices2D.EnsureCapacity(VertexCount);

            for (int i = 0; i < VertexCount; ++i)
            {
                Vertices2D.Add(membranePointData.Vertices2D[i]);
            }

            AverageVertex = MembranePointData.GetAverageVertex(Vertices2D, VertexCount);
        }

        /// <summary>
        ///   Releases input references when returning to the generator's pool, retaining buffer capacity.
        /// </summary>
        public void Clear()
        {
            OriginalPointData = null!;
            MulticellularMembraneGenerationCellData = default;
            CellKey = 0;
            VertexCount = 0;
            AverageVertex = default;
            Vertices2D.Clear();
            LinesData.Clear();
            IntersectionsData.Clear();
        }
    }

    // Added warning disable as SA1201 requires a struct to be before a class,
    // but format_test doesn't like that order...
#pragma warning disable SA1201

    /// <summary>
    ///   Input shared between generation threads, without mutable working geometry.
    /// </summary>
    public readonly struct CellGenerationData : IEquatable<CellGenerationData>
    {
        public readonly MembranePointData OriginalPointData;
        public readonly MulticellularMembraneGenerationCellData GenerationData;

        public CellGenerationData(MembranePointData originalPointData,
            MulticellularMembraneGenerationCellData generationData)
        {
            OriginalPointData = originalPointData;
            GenerationData = generationData;
        }

        public static bool operator ==(CellGenerationData left, CellGenerationData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CellGenerationData left, CellGenerationData right)
        {
            return !left.Equals(right);
        }

        public bool Equals(CellGenerationData other)
        {
            return OriginalPointData.Equals(other.OriginalPointData) && GenerationData.Equals(other.GenerationData);
        }

        public override bool Equals(object? obj)
        {
            return obj is CellGenerationData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OriginalPointData, GenerationData);
        }
    }

    /// <summary>
    ///   Identifies one continuous section in the generator's boundary point buffer.
    /// </summary>
    public readonly struct BoundaryPathRange : IEquatable<BoundaryPathRange>
    {
        public readonly int Start;
        public readonly int Count;

        public BoundaryPathRange(int start, int count)
        {
            Start = start;
            Count = count;
        }

        public static bool operator ==(BoundaryPathRange left, BoundaryPathRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoundaryPathRange left, BoundaryPathRange right)
        {
            return !left.Equals(right);
        }

        public bool Equals(BoundaryPathRange other)
        {
            return Start == other.Start && Count == other.Count;
        }

        public override bool Equals(object? obj)
        {
            return obj is BoundaryPathRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, Count);
        }
    }

    /// <summary>
    ///   Stores the separating line between two cells.
    /// </summary>
    public struct LineData : IEquatable<LineData>
    {
        public readonly long OtherCellKey;
        public readonly bool IsOverLapping;

        public Vector2 MiddlePoint;
        public Vector2 Line;
        public Vector2 Normal;

        public bool ShouldRemove = true;

        public LineData(Vector2 middlePoint, Vector2 normal, long otherCellKey, bool isOverlapping = false)
        {
            MiddlePoint = middlePoint;
            Normal = normal;
            Line = new Vector2(normal.Y, -normal.X);

            OtherCellKey = otherCellKey;
            IsOverLapping = isOverlapping;
        }

        public static bool operator ==(LineData left, LineData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LineData left, LineData right)
        {
            return !left.Equals(right);
        }

        public bool Equals(LineData other)
        {
            return OtherCellKey == other.OtherCellKey && IsOverLapping == other.IsOverLapping &&
                MiddlePoint.Equals(other.MiddlePoint) && Line.Equals(other.Line) && Normal.Equals(other.Normal) &&
                ShouldRemove == other.ShouldRemove;
        }

        public override bool Equals(object? obj)
        {
            return obj is LineData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OtherCellKey, IsOverLapping, MiddlePoint, Line, Normal, ShouldRemove);
        }
    }
#pragma warning restore SA1201
}
