using System;
using System.Collections.Generic;
using System.Text;
using Godot;

public class MembraneShapeStretcher
{
    public const float Distance = 0.01f;

    public const float DistanceSquared = Distance * Distance;

    public const float Parallel = 1e-6f;

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
        return orientation * MathF.PI * 0.3333333f;
    }

    /// <summary>
    ///   Rotates a point around the origin by the supplied angle in radians.
    /// </summary>
    public static Vector2 RotatePoint(Vector2 point, float angle)
    {
        (float sin, float cos) = MathF.SinCos(angle);
        return RotatePoint(point, sin, cos);
    }

    public static void RotateCellVertices(CellData cellData)
    {
        float orientationAngle = GetOrientationAngle(cellData.MulticellularMembraneGenerationCellData.Orientation);
        (float sin, float cos) = MathF.SinCos(orientationAngle);

        Vector2 position = cellData.MulticellularMembraneGenerationCellData.Position;

        for (int i = 0; i < cellData.VertexCount; ++i)
        {
            cellData.Vertices2D[i] = RotatePoint(cellData.Vertices2D[i], sin, cos) + position;
        }

        cellData.AverageVertex = RotatePoint(cellData.AverageVertex, sin, cos) + position;
    }

    public static void UnrotateCellVertices(CellData cellData)
    {
        float orientationAngle = GetOrientationAngle(cellData.MulticellularMembraneGenerationCellData.Orientation);
        (float sin, float cos) = MathF.SinCos(-orientationAngle);

        Vector2 position = cellData.MulticellularMembraneGenerationCellData.Position;

        for (int i = 0; i < cellData.VertexCount; ++i)
        {
            cellData.Vertices2D[i] = RotatePoint(cellData.Vertices2D[i] - position, sin, cos);
        }

        cellData.AverageVertex = RotatePoint(cellData.AverageVertex - position, sin, cos);
    }

    /// <summary>
    ///   Calculates visible intersections between cell lines and removes
    ///   duplicate intersection positions within the configured tolerance.
    ///   Intersections beyond the maximum stretching distance from the cell position are discarded.
    /// </summary>
    public static void GetIntersections(CellData cell, Vector2 averageVertex)
    {
        var lines = cell.LinesData;
        var intersections = cell.IntersectionsData;
        var position = cell.MulticellularMembraneGenerationCellData.Position;
        var maxDistance = Constants.MEMBRANE_MAX_STRETCHED_DISTANCE_SQUARED * 0.5f * 0.5f;

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
                    float averageSide = SignedDistanceToLine(clippingLine, averageVertex);
                    float intersectionSide = SignedDistanceToLine(clippingLine, intersection);

                    bool isSeparated =
                        (averageSide > Distance && intersectionSide < -Distance) ||
                        (averageSide < -Distance && intersectionSide > Distance);

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
                    if (intersections[k].DistanceSquaredTo(intersection) <= DistanceSquared)
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

        var firstSegmentLengthSquared = firstSegmentDirection.Dot(firstSegmentDirection);
        var secondSegmentLengthSquared = secondSegmentDirection.Dot(secondSegmentDirection);
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

    public static void CreateBoundaryLines(Dictionary<long, CellData> cellsData,
        StringBuilder info, List<LineData> linesData)
    {
        linesData.Clear();

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
                var isOverlappig = false;

                if (IsPointInsideConvexPolygon(middlePoint, cellA.Vertices2D) ||
                    IsPointInsideConvexPolygon(middlePoint, cellB.Vertices2D))
                {
                    closestB = cellB.AverageVertex;
                    closestA = cellA.AverageVertex;
                    isOverlappig = true;
                }

                CreateBoundaryLinesForNeighbours(closestB, closestA, cellA, cellKeyB, cellB, cellKeyA, isOverlappig);
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

                    if (Math.Abs(candidate.Line.X) < 0.00001f)
                    {
                        info.Append($"x = {candidate.MiddlePoint.X:F3}\n");
                    }
                    else
                    {
                        var a = candidate.Line.Y / candidate.Line.X;
                        var b = candidate.MiddlePoint.Y -
                            a * candidate.MiddlePoint.X;

                        info.Append($"y = {a:F3}x + {b:F3}\n");
                    }

                    info.Append($"({candidate.MiddlePoint.X:F3}, {candidate.MiddlePoint.Y:F3})\n");

                    float averageSide = SignedDistanceToLine(otherLine, averageVertex);
                    float candidateSide = SignedDistanceToLine(otherLine, candidate.MiddlePoint);

                    bool isSeparated =
                        (averageSide > Distance && candidateSide < -Distance) ||
                        (averageSide < -Distance && candidateSide > Distance);

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

    public static bool IsPointInsideConvexPolygon(Vector2 point, List<Vector2> vectices2D)
    {
        bool? sign = null;

        for (int i = 0; i < vectices2D.Count; ++i)
        {
            Vector2 a = vectices2D[i];
            Vector2 b = vectices2D[(i + 1) % vectices2D.Count];

            float cross = (b - a).Cross(point - a);

            if (Math.Abs(cross) < 0.0001f)
                continue;

            bool currentSign = cross > 0;

            if (sign.HasValue && sign.Value != currentSign)
                return false;

            sign = currentSign;
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

    private static Vector2 RotatePoint(Vector2 point, float sin, float cos)
    {
        return new Vector2(cos * point.X - sin * point.Y, sin * point.X + cos * point.Y);
    }

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
        if (directionLengthProductSquared < Parallel ||
            MathF.Abs(directionCrossProduct) < Parallel * directionLengthProductSquared)
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

    private static float SignedDistanceToLine(LineData line, Vector2 point)
    {
        var length = line.Normal.Length();

        if (length < Parallel)
            return 0.0f;

        return line.Normal.Dot(point - line.MiddlePoint) / length;
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
            if (MathF.Abs(SignedDistanceToLine(line, position)) > Distance)
                continue;

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

    private static void FindClosestPointsOnMembranes(int countA, List<Vector2> verticesA, int countB,
        List<Vector2> verticesB,
        ref float minDistance, ref Vector2 closestA, ref Vector2 closestB)
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

    private static bool TryCastMembranePoint(Vector2 membranePoint,
        Vector2 averagePoint,
        List<Vector2> boundaryPoints,
        List<BoundaryPathRange> boundaryPaths,
        out Vector2 hitPoint)
    {
        hitPoint = Vector2.Zero;

        Vector2 toPoint = membranePoint - averagePoint;
        float toPointLength = toPoint.Length();

        if (toPointLength < Distance)
            return false;

        Vector2 direction = toPoint / toPointLength;

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

                // direction jest jednostkowy, więc to jest odległość w jednostkach świata.
                float distance = (intersection - membranePoint).Dot(direction);

                if (distance < -Distance || distance >= closestDistance)
                    continue;

                closestDistance = distance;
                hitPoint = intersection;
                found = true;
            }
        }

        return found;
    }

    private static bool TryRaySegmentIntersection(Vector2 rayOrigin,
        Vector2 rayDirection,
        Vector2 segmentStart,
        Vector2 segmentEnd,
        out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        Vector2 segmentDirection = segmentEnd - segmentStart;
        float segmentLength = segmentDirection.Length();

        if (segmentLength < Distance)
            return false;

        Vector2 offset = segmentStart - rayOrigin;

        float denominator = rayDirection.Cross(segmentDirection);

        if (MathF.Abs(denominator) < Parallel * segmentLength)
            return false;

        float t = offset.Cross(segmentDirection) / denominator;
        float u = offset.Cross(rayDirection) / denominator;

        float uTolerance = Distance / segmentLength;

        if (t < -Distance || u < -uTolerance || u > 1.0f + uTolerance)
            return false;

        intersection = rayOrigin + rayDirection * t;
        return true;
    }

    private static void AddSinglePointPath(Vector2 point,
        List<Vector2> polygon,
        List<Vector2> boundaryPoints,
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

    private static bool CanConnect(Vector2 a,
        Vector2 b,
        List<Vector2> polygon)
    {
        if (SegmentEntersPolygonInterior(a, b, polygon))
            return false;

        return IsPolygonOnRightSide(a, b, polygon);
    }

    private static bool IsPolygonOnRightSide(Vector2 a,
        Vector2 b,
        List<Vector2> polygon)
    {
        foreach (Vector2 vertex in polygon)
        {
            if (SideOf(a, b, vertex) > Distance)
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
        float segmentLength = segmentDirection.Length();

        if (segmentLength < Distance)
            return false;

        float minimumSegmentParameter = 0.0f;
        float maximumSegmentParameter = 1.0f;

        const float interiorLimit = -Distance;

        for (int edgeIndex = 0; edgeIndex < polygon.Count; ++edgeIndex)
        {
            Vector2 edgeStart = polygon[edgeIndex];
            Vector2 edgeEnd = polygon[(edgeIndex + 1) % polygon.Count];
            Vector2 edgeDirection = edgeEnd - edgeStart;

            float edgeLength = edgeDirection.Length();

            // Duplicate vertex - this edge provides no useful information.
            if (edgeLength < Parallel)
                continue;

            // Signed distance from the segment start to the edge line.
            // Positive means the point is on the left side.
            float initialSignedDistance =
                Cross(edgeDirection, segmentStart - edgeStart) / edgeLength;

            // Change in signed distance over the entire segment (parameter 0 -> 1).
            float signedDistanceChange =
                Cross(edgeDirection, segmentDirection) / edgeLength;

            if (MathF.Abs(signedDistanceChange) < Parallel)
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

        // Length of the segment portion inside the polygon,
        // measured in world units rather than parameter units.
        float interiorSegmentLength =
            (maximumSegmentParameter - minimumSegmentParameter) * segmentLength;

        return interiorSegmentLength > Distance;
    }

    private static Vector2 GetIncomingTangent(Vector2 point,
        List<Vector2> polygon)
    {
        return FindTangent(point, polygon, true);
    }

    private static Vector2 GetOutgoingTangent(Vector2 point,
        List<Vector2> polygon)
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

        Vector2? bestCandidate = null;
        float bestDistance = float.MaxValue;

        foreach (Vector2 tangentPoint in polygon)
        {
            bool isValid = true;

            foreach (Vector2 vertex in polygon)
            {
                float side = incoming ?
                    SideOf(tangentPoint, point, vertex) :
                    SideOf(point, tangentPoint, vertex);

                if (side > Distance)
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
            }
        }

        if (bestCandidate == null)
        {
            GD.PrintErr($"Could not find an external tangent for point {point}.");
            return point;
        }

        return bestCandidate.Value;
    }

    private static bool IsPointInsideOrOnPolygon(Vector2 point,
        List<Vector2> polygon)
    {
        for (int i = 0; i < polygon.Count; ++i)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];

            if (SideOf(a, b, point) > Distance)
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
        const float toleranceSquared = Distance * Distance;

        int uniquePointCount = 0;

        for (int pointIndex = 0; pointIndex < points.Count; ++pointIndex)
        {
            Vector2 candidatePoint = points[pointIndex];
            bool isDuplicate = false;

            for (int existingPointIndex = 0; existingPointIndex < uniquePointCount; ++existingPointIndex)
            {
                if (points[existingPointIndex].DistanceSquaredTo(candidatePoint) <=
                    toleranceSquared)
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

    private static float SideOf(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 direction = b - a;
        float length = direction.Length();

        if (length < Parallel)
            return 0.0f;

        return Cross(direction, p - a) / length;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    /// <summary>
    ///   Sorts boundary points clockwise around a cell's centre using a reusable comparison delegate.
    /// </summary>
    public sealed class BoundaryPointComparer
    {
        public BoundaryPointComparer()
        {
            Comparison = Compare;
        }

        public Vector2 Centre { get; set; }

        public Comparison<Vector2> Comparison { get; }

        private int Compare(Vector2 firstPoint, Vector2 secondPoint)
        {
            float firstAngle = MathF.Atan2(firstPoint.Y - Centre.Y, firstPoint.X - Centre.X);
            float secondAngle = MathF.Atan2(secondPoint.Y - Centre.Y, secondPoint.X - Centre.X);
            return secondAngle.CompareTo(firstAngle);
        }
    }
}
