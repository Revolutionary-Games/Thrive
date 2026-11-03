using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;


public class MembraneShapeStretcher
{
    /// <summary>
    ///   Modifies already generated membrane to make it stretch towards other cells. Also applies membrane
    ///   waviness as it is normally done in single cellular membrane
    /// </summary>
    public static Dictionary<long, MembranePointData> GenerateMulticellularMembrane(
        ConcurrentDictionary<long, CellData> cellsData)
    {
        var result = new Dictionary<long, MembranePointData>(cellsData.Count);

        var maxDistance = 1500.0f;
        var info = "";

        foreach (var (key, data) in cellsData)
        {
            info += string.Join(", ",
                data.Vertices2D.ToList().Slice(0, data.VertexCount).Select(v => $"({v.X:F3}, {v.Y:F3})"));
            info += "\n";
        }

        // Reuse the candidate buffer for every cell to avoid one List allocation per cell.
        var candidates = new List<LineData>();

        foreach (var (cellKey, cellData) in cellsData)
        {
            candidates.Clear();

            var cellPosition =
                cellData.MulticellularMembraneGenerationCellData.Position;

            var averageVertex = cellData.AverageVertex;

            var verticesA = cellData.Vertices2D;
            var countA = cellData.VertexCount;

            if (countA < 2)
                continue;

            foreach (var neighbourData in cellsData)
            {
                if (neighbourData.Key == cellKey)
                    continue;

                var neighbourCell = neighbourData.Value;

                var neighbourPosition =
                    neighbourCell.MulticellularMembraneGenerationCellData.Position;

                var distanceCells =
                    cellPosition.DistanceSquaredTo(neighbourPosition);

                if (distanceCells > maxDistance * maxDistance)
                    continue;

                var verticesB = neighbourCell.Vertices2D;
                var countB = neighbourCell.VertexCount;

                if (countB < 2)
                    continue;

                var minDistance = float.MaxValue;

                var closestA = Vector2.Zero;
                var closestB = Vector2.Zero;

                // Porównywanie wszystkich odcinków obu membran.
                for (var i = 0; i < countA; i++)
                {
                    var a0 = verticesA[i];
                    var a1 = verticesA[(i + 1) % countA];

                    for (var j = 0; j < countB; j++)
                    {
                        var b0 = verticesB[j];
                        var b1 = verticesB[(j + 1) % countB];

                        var distance = ClosestPointsOnSegments(a0,
                            a1,
                            b0,
                            b1,
                            out var pointA,
                            out var pointB);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestA = pointA;
                            closestB = pointB;
                        }
                    }
                }

                if (minDistance == float.MaxValue)
                    continue;

                var normal = closestB - closestA;

                // Pomijamy przypadek przecięcia membran.
                if (normal.LengthSquared() < 1e-8f)
                    continue;

                var middlePoint = (closestA + closestB) / 2f;

                candidates.Add(new LineData(middlePoint,
                    normal,
                    cellKey,
                    neighbourCell.CellKey));
            }

            info = GetOnlyCloseLines(
                cellData,
                candidates,
                averageVertex,
                info);
        }

        // RemoveAsymmetricLines(tracker);
        CopyLinesToNeighbours(cellsData);

        foreach (var (cellKey, cellData) in cellsData)
        {
            var averageVertex = cellData.AverageVertex;
            GetIntersections(cellData, averageVertex);
        }

        // Filter incomplete triples
        FilterInconsistentIntersections(cellsData);

        // Print only intersections that survived filtering
        foreach (var (cellKey, cellData) in cellsData)
        {
            CellBoundaryBuilder.BuildBoundaryPaths(cellData);
            CellBoundaryBuilder.CastMembranePoints(cellData);

            // SmoothVertexRegion(cellData);

            foreach (var path in cellData.BoundaryPaths)
            {
                foreach (var point in path.Points)
                {
                    info += $"({point.X:F3}, " +
                        $"{point.Y:F3}),";
                }
            }

            info = info.TrimEnd(',');

            info += "\n";
        }

        foreach (var (key, data) in cellsData)
        {
            info += string.Join(", ",
                data.Vertices2D.ToList().Slice(0, data.VertexCount).Select(v => $"({v.X:F3}, {v.Y:F3})"));
            info += "\n";
        }

        if (cellsData.Count >= 3)
        {
            GD.Print();
            GD.Print($"{info}");
        }

        foreach (var (key, cellData) in cellsData)
        {
            var pos = cellData.MulticellularMembraneGenerationCellData.Position;
            var ang = GetOrientationAngle(cellData.MulticellularMembraneGenerationCellData
                .Orientation);

            for (int i = 0; i < cellData.VertexCount; ++i)
            {
                cellData.Vertices2D[i] = RotatePoint(cellData.Vertices2D[i] - pos, -ang);
            }
        }

        foreach (var (key, data) in cellsData)
        {
            int hexCount = data.OriginalPointData.HexPositionCount;
            var hexCopy = ArrayPool<Vector2>.Shared.Rent(hexCount);
            data.OriginalPointData.HexPositions.AsSpan(0, hexCount).CopyTo(hexCopy);
            var membraneData = new MembranePointData(hexCopy, hexCount, data.OriginalPointData.Type, data.Vertices2D);

            result.Add(key, membraneData);
        }

        return result;
    }

    /// <summary>
    /// Smooths every vertex by averaging it with its two neighbouring vertices.
    /// The polygon is treated as closed, so the first and last vertices are neighbours.
    /// </summary>
    private static void SmoothVertexRegion(CellData cellData)
    {
        var vertexCount = cellData.VertexCount;

        for (int i = 0; i <= vertexCount; ++i)
        {
            int index = i % vertexCount;
            int previousPointIndex = (i + vertexCount - 1) % vertexCount;
            int nextPointIndex = (i + 1) % vertexCount;

            cellData.Vertices2D[index] = (cellData.Vertices2D[previousPointIndex] + cellData.Vertices2D[index] * 3 +
                cellData.Vertices2D[nextPointIndex]) * 0.2f;
        }
    }

    /// <summary>
    /// Converts the discrete cell orientation into a rotation angle in radians.
    /// </summary>
    public static float GetOrientationAngle(int orientation)
    {
        return orientation * MathF.PI * 0.3333333f;
    }

    /// <summary>
    /// Rotates a point around the origin by the supplied angle in radians.
    /// </summary>
    public static Vector2 RotatePoint(Vector2 point, float angle)
    {
        (float sin, float cos) = MathF.SinCos(angle);
        return new Vector2(cos * point.X - sin * point.Y, sin * point.X + cos * point.Y);
    }

    private static bool TryGetIntersection(LineData line1,
        LineData line2,
        out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        var p = line1.MiddlePoint;
        var q = line2.MiddlePoint;

        var r = line1.Line;
        var s = line2.Line;

        var denominator = r.X * s.Y - r.Y * s.X;

        // Parallel or almost parallel lines
        var scale = MathF.Sqrt(r.LengthSquared() * s.LengthSquared());

        if (scale < 0.000001f ||
            MathF.Abs(denominator) < 0.000001f * scale)
            return false;

        var qp = q - p;

        var t = (qp.X * s.Y - qp.Y * s.X) / denominator;

        intersection = p + t * r;

        return true;
    }

    /// <summary>
    ///   Odległość ze znakiem punktu od prostej (dodatnia = ta sama strona, w którą wskazuje Normal).
    ///   Normal nie musi być znormalizowany - dzielimy przez jego długość, więc wynik jest
    ///   w jednostkach świata i można go porównywać z jedną wspólną tolerancją.
    /// </summary>
    private static float SignedDistanceToLine(LineData line, Vector2 point)
    {
        var length = line.Normal.Length();

        if (length < GeometryTolerance.Parallel)
            return 0f;

        return line.Normal.Dot(point - line.MiddlePoint) / length;
    }

    /// <summary>
    /// Keeps only lines that do not separate their midpoint from the cell center
    /// with respect to any other candidate line.
    /// </summary>
    private static string GetOnlyCloseLines(CellData cell,
        List<LineData> candidates,
        Vector2 averageVertex,
        string info)
    {
        const float tolerance = GeometryTolerance.Distance;

        Dictionary<long, LineData> acceptedLines = cell.LinesData;

        acceptedLines.Clear();

        for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            LineData candidate = candidates[candidateIndex];

            bool isVisible = true;

            for (int otherIndex = 0; otherIndex < candidates.Count; otherIndex++)
            {
                if (candidateIndex == otherIndex)
                    continue;

                LineData otherLine = candidates[otherIndex];

                if (Math.Abs(candidate.Line.X) < 0.00001f)
                {
                    info += $"x = {candidate.MiddlePoint.X:F3}\n";
                }
                else
                {
                    var a = candidate.Line.Y / candidate.Line.X;
                    var b = candidate.MiddlePoint.Y - a * candidate.MiddlePoint.X;

                    info += $"y = {a:F3}x + {b:F3}\n";
                }

                info += $"({candidate.MiddlePoint.X:F3}, {candidate.MiddlePoint.Y:F3})\n";

                float averageSide = SignedDistanceToLine(otherLine,
                    averageVertex);

                float candidateSide = SignedDistanceToLine(otherLine,
                    candidate.MiddlePoint);

                bool isSeparated =
                    (averageSide > tolerance && candidateSide < -tolerance) ||
                    (averageSide < -tolerance && candidateSide > tolerance);

                if (isSeparated)
                {
                    isVisible = false;
                    break;
                }
            }

            if (isVisible)
                acceptedLines[candidate.LineKey] = candidate;
        }

        return info;
    }

    /// <summary>
    /// Calculates visible intersections between cell lines and removes
    /// duplicate intersection positions within the configured tolerance.
    /// </summary>
    private static void GetIntersections(CellData cell,
        Vector2 averageVertex)
    {
        List<LineData> lines = new(cell.LinesData.Values);
        Dictionary<long, IntersetionData> intersections =
            cell.IntersectionsData;

        intersections.Clear();

        const float tolerance = GeometryTolerance.Distance;
        const float toleranceSquared = tolerance * tolerance;

        var intersectionPositions = new List<Vector2>();

        for (int firstLineIndex = 0; firstLineIndex < lines.Count; firstLineIndex++)
        {
            LineData firstLine = lines[firstLineIndex];

            for (int secondLineIndex = firstLineIndex + 1;
                 secondLineIndex < lines.Count;
                 secondLineIndex++)
            {
                LineData secondLine = lines[secondLineIndex];

                if (!TryGetIntersection(firstLine, secondLine, out Vector2 intersection))
                    continue;

                bool isVisible = true;

                foreach (LineData clippingLine in lines)
                {
                    float averageSide = SignedDistanceToLine(clippingLine,
                        averageVertex);

                    float intersectionSide = SignedDistanceToLine(clippingLine,
                        intersection);

                    bool isSeparated =
                        (averageSide > tolerance && intersectionSide < -tolerance) ||
                        (averageSide < -tolerance && intersectionSide > tolerance);

                    if (isSeparated)
                    {
                        isVisible = false;
                        break;
                    }
                }

                if (!isVisible)
                    continue;

                bool alreadyExists = false;

                for (int existingIndex = 0;
                     existingIndex < intersectionPositions.Count;
                     existingIndex++)
                {
                    if (intersectionPositions[existingIndex]
                            .DistanceSquaredTo(intersection) <= toleranceSquared)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (alreadyExists)
                    continue;

                intersectionPositions.Add(intersection);

                var intersectionData = new IntersetionData(intersection);

                intersections[intersectionData.IntersectionId] = intersectionData;
            }
        }
    }

    /// <summary>
    ///   Sygnatura przecięcia = posortowany zbiór kluczy komórek, które schodzą się w tym punkcie:
    ///   ta komórka + właściciele WSZYSTKICH jej linii, które przechodzą przez punkt (w tolerancji).
    ///   Dla zwykłego styku 3 komórek to {A, B, C}, dla punktu wspólnego 4 komórek {A, B, C, D}.
    ///   Nie zależy od tego, która para linii została zachowana po deduplikacji.
    /// </summary>
    private static long[] GetIntersectionSignature(CellData cell, Vector2 position)
    {
        var keys = new HashSet<long> { cell.CellKey };

        foreach (var line in cell.LinesData.Values)
        {
            if (MathF.Abs(SignedDistanceToLine(line, position)) <= GeometryTolerance.Distance)
                keys.Add(line.OtherCellKey);
        }

        var result = keys.ToArray();
        Array.Sort(result);

        return result;
    }

    private static string SignatureKey(long[] signature)
    {
        return string.Join(",", signature);
    }

    /// <summary>
    /// Removes intersections whose cell-owner signature is not shared by every cell
    /// participating in that intersection.
    /// </summary>
    private static void FilterInconsistentIntersections(ConcurrentDictionary<long, CellData> cellsData)
    {
        // (komórka, przecięcie) -> sygnatura
        var signatures = new Dictionary<(long Cell, long Intersection), long[]>();

        // sygnatura -> komórki, które mają przecięcie o dokładnie tej sygnaturze
        var owners = new Dictionary<string, HashSet<long>>();

        // First pass: policz sygnatury wszystkich przecięć
        foreach (var (cellKey, cell) in cellsData)
        {
            foreach (var intersection in cell.IntersectionsData.Values)
            {
                var signature = GetIntersectionSignature(cell, intersection.Position);
                signatures[(cellKey, intersection.IntersectionId)] = signature;

                var key = SignatureKey(signature);

                if (!owners.TryGetValue(key, out var set))
                {
                    set = new HashSet<long>();
                    owners.Add(key, set);
                }

                set.Add(cellKey);
            }
        }

        // Second pass: przecięcie jest poprawne tylko wtedy, gdy KAŻDA komórka z sygnatury
        // ma przecięcie z identyczną sygnaturą. Właściciele zawsze należą do sygnatury
        // (komórka zawsze dodaje samą siebie), więc wystarczy porównać liczności.
        foreach (var (cellKey, cell) in cellsData)
        {
            var deletedIntersections = new List<long>();

            foreach (var intersection in cell.IntersectionsData.Values)
            {
                var signature = signatures[(cellKey, intersection.IntersectionId)];

                if (signature.Length < 3 ||
                    owners[SignatureKey(signature)].Count != signature.Length)
                {
                    deletedIntersections.Add(intersection.IntersectionId);
                }
            }

            foreach (var intersectionId in deletedIntersections)
            {
                cell.IntersectionsData.Remove(intersectionId);
            }
        }
    }

    /// <summary>
    /// Finds the closest points on two finite line segments and returns their
    /// squared distance. The output points lie on their respective segments.
    /// </summary>
    private static float ClosestPointsOnSegments(Vector2 firstSegmentStart,
        Vector2 firstSegmentEnd,
        Vector2 secondSegmentStart,
        Vector2 secondSegmentEnd,
        out Vector2 closestPointOnFirstSegment,
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

        const float epsilon = 1e-8f;

        if (firstSegmentLengthSquared <= epsilon && secondSegmentLengthSquared <= epsilon)
        {
            closestPointOnFirstSegment = firstSegmentStart;
            closestPointOnSecondSegment = secondSegmentStart;
            return closestPointOnFirstSegment.DistanceSquaredTo(closestPointOnSecondSegment);
        }

        if (firstSegmentLengthSquared <= epsilon)
        {
            firstSegmentParameter = 0f;
            secondSegmentParameter = Math.Clamp(secondDirectionOffsetDot / secondSegmentLengthSquared, 0f, 1f);
        }
        else
        {
            var firstDirectionOffsetDot = firstSegmentDirection.Dot(startPointOffset);

            if (secondSegmentLengthSquared <= epsilon)
            {
                secondSegmentParameter = 0f;
                firstSegmentParameter = Math.Clamp(-firstDirectionOffsetDot / firstSegmentLengthSquared, 0f, 1f);
            }
            else
            {
                var directionDotProduct = firstSegmentDirection.Dot(secondSegmentDirection);
                var denominator = firstSegmentLengthSquared * secondSegmentLengthSquared - directionDotProduct * directionDotProduct;

                firstSegmentParameter = denominator > epsilon ? Math.Clamp((directionDotProduct * secondDirectionOffsetDot - firstDirectionOffsetDot * secondSegmentLengthSquared) / denominator, 0f, 1f) : 0f;

                secondSegmentParameter = (directionDotProduct * firstSegmentParameter + secondDirectionOffsetDot) / secondSegmentLengthSquared;

                if (secondSegmentParameter < 0f)
                {
                    secondSegmentParameter = 0f;
                    firstSegmentParameter = Math.Clamp(-firstDirectionOffsetDot / firstSegmentLengthSquared, 0f, 1f);
                }
                else if (secondSegmentParameter > 1f)
                {
                    secondSegmentParameter = 1f;
                    firstSegmentParameter = Math.Clamp((directionDotProduct - firstDirectionOffsetDot) / firstSegmentLengthSquared, 0f, 1f);
                }
            }
        }

        closestPointOnFirstSegment = firstSegmentStart + firstSegmentDirection * firstSegmentParameter;
        closestPointOnSecondSegment = secondSegmentStart + secondSegmentDirection * secondSegmentParameter;

        return closestPointOnFirstSegment.DistanceSquaredTo(closestPointOnSecondSegment);
    }

    /// <summary>
    ///   Linia komórki A -> B jest zachowana tylko wtedy, gdy komórka B zachowała linię B -> A.
    ///   Filtrowanie w GetOnlyCloseLines jest liczone osobno dla każdej komórki (inny averageVertex),
    ///   więc wynik może być asymetryczny - tutaj go symetryzujemy.
    /// </summary>
    // private static void RemoveAsymmetricLines(ConcurrentDictionary<long, CellData>cellsData)
    // {
    //     // Snapshot: które pary (komórka, sąsiad) przetrwały filtrowanie.
    //     var surviving = new HashSet<(long Cell, long Other)>();
    //
    //     foreach (var (_, cell) in cellsData)
    //     {
    //         foreach (var line in cell.LinesData.Values)
    //             surviving.Add((cell.CellKey, line.OtherCellKey));
    //     }
    //
    //     foreach (var (_, cell) in cellsData)
    //     {
    //         var toRemove = cell.LinesData
    //             .Where(pair => !surviving.Contains((pair.Value.OtherCellKey, cell.CellKey)))
    //             .Select(pair => pair.Key)
    //             .ToList();
    //
    //         foreach (var lineKey in toRemove)
    //             cell.LinesData.Remove(lineKey);
    //     }
    // }

    /// <summary>
    ///   Jeśli komórka A ma linię do B, a B nie ma linii do A, kopiujemy linię do B
    ///   (ten sam punkt środkowy, odwrócony Normal).
    /// </summary>
    /// <summary>
    /// Copies missing neighbour lines while preserving the original midpoint
    /// and reversing the normal direction.
    /// </summary>
    private static void CopyLinesToNeighbours(ConcurrentDictionary<long, CellData> cellsData)
    {
        var existingConnections = new HashSet<(long CellKey, long OtherCellKey)>();

        foreach (CellData cell in cellsData.Values)
        {
            foreach (LineData line in cell.LinesData.Values)
            {
                existingConnections.Add((cell.CellKey, line.OtherCellKey));
            }
        }

        var pendingLines = new List<(CellData Target, LineData Line)>();

        foreach (CellData cell in cellsData.Values)
        {
            foreach (LineData line in cell.LinesData.Values)
            {
                if (!cellsData.TryGetValue(line.OtherCellKey, out CellData neighbour))
                    continue;

                var reverseConnection = (neighbour.CellKey, cell.CellKey);

                if (!existingConnections.Add(reverseConnection))
                    continue;

                var copiedLine = new LineData(line.MiddlePoint,
                    -line.Normal,
                    neighbour.CellKey,
                    cell.CellKey);

                pendingLines.Add((neighbour, copiedLine));
            }
        }

        foreach (var pendingLine in pendingLines)
        {
            pendingLine.Target.LinesData[pendingLine.Line.LineKey] = pendingLine.Line;
        }
    }

    /// <summary>
    /// Copies missing neighbour lines while preserving the original midpoint
    /// and reversing the normal direction.
    /// </summary>
    private static void SmoothVertexRegion(List<Vector2> vertices, int indexStart, int indexEnd)
    {
        var vertexCount = vertices.Count;

        for (int i = indexStart; i < indexEnd; ++i)
        {
            int index = i % vertexCount;
            int previousPointIndex = (i + vertexCount - 1) % vertexCount;
            int nextPointIndex = (i + 1) % vertexCount;

            vertices[index] = (vertices[previousPointIndex] + vertices[index] + vertices[nextPointIndex]) * 0.333333f;
        }
    }
}

/// <summary>
///   Wspólne tolerancje geometryczne (w jednostkach świata, NIE zależne od długości wektorów).
///   Jeśli Twoje jednostki są dużo mniejsze/większe, zmień tylko tutaj.
///   Dla współrzędnych rzędu 1000 float ma precyzję ~1e-4, więc tolerancja musi być wyraźnie powyżej.
/// </summary>
public static class GeometryTolerance
{
    /// <summary>Tolerancja odległości: duplikaty punktów, strona prostej, styczność itd.</summary>
    public const float Distance = 0.01f;

    /// <summary>Tolerancja na równoległość (bezwymiarowa, sinus kąta) i zerową długość wektora.</summary>
    public const float Parallel = 1e-6f;
}

public static class CellBoundaryBuilder
{
    private const float Tolerance = GeometryTolerance.Distance;

    /// <summary>
    /// Builds the visible boundary paths from the cell polygon, line midpoints,
    /// and accepted intersections.
    /// </summary>
    public static void BuildBoundaryPaths(CellData cell)
    {
        cell.BoundaryPaths.Clear();

        var polygon = new List<Vector2>(cell.VertexCount);

        for (int vertexIndex = 0; vertexIndex < cell.VertexCount; vertexIndex++)
        {
            polygon.Add(cell.Vertices2D[vertexIndex]);
        }

        if (polygon.Count < 3)
            throw new InvalidOperationException("Membrana musi mieć co najmniej 3 wierzchołki.");

        // Normalizujemy kolejność wierzchołków tak, aby wnętrze
        // wielokąta znajdowało się po prawej stronie jego krawędzi.
        if (SignedArea(polygon) > 0)
            polygon.Reverse();

        var points = new List<Vector2>(
            cell.IntersectionsData.Count + cell.LinesData.Count);

        foreach (IntersetionData intersection in cell.IntersectionsData.Values)
        {
            points.Add(intersection.Position);
        }

        foreach (LineData line in cell.LinesData.Values)
        {
            points.Add(line.MiddlePoint);
        }

        points = RemoveDuplicatePoints(points);

        if (points.Count == 0)
            return;

        Vector2 center = GetPolygonCenter(polygon);

        points.Sort((firstPoint, secondPoint) =>
        {
            float firstAngle = MathF.Atan2(
                firstPoint.Y - center.Y,
                firstPoint.X - center.X);

            float secondAngle = MathF.Atan2(
                secondPoint.Y - center.Y,
                secondPoint.X - center.X);

            return secondAngle.CompareTo(firstAngle);
        });

        // Jeden punkt: dwie styczne.
        if (points.Count == 1)
        {
            AddSinglePointPath(points[0], polygon, cell.BoundaryPaths);
            return;
        }

        // Od 2 punktów wzwyż używamy jednej ogólnej ścieżki.
        // Dla 2 punktów testuje ona OBA kierunki (0->1 oraz 1->0), więc wynik
        // nie zależy już od tego, po której stronie cięcia atan2 (±pi) leżą punkty.
        var validEdges = new bool[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            int next = (i + 1) % points.Count;

            validEdges[i] = CanConnect(points[i],
                points[next],
                polygon);
        }

        // Zdegenerowany przypadek: 2 punkty i oba kierunki poprawne
        // (wszystkie wierzchołki membrany współliniowe z odcinkiem).
        // Nie robimy z tego "zamkniętej" pętli p0 -> p1 -> p0.
        if (points.Count == 2 && validEdges[0] && validEdges[1])
        {
            AddOpenPath(points, polygon, cell.BoundaryPaths);
            return;
        }

        // Wszystkie punkty tworzą zamknięty obwód.
        bool allEdgesValid = true;

        for (int edgeIndex = 0; edgeIndex < validEdges.Length; edgeIndex++)
        {
            if (validEdges[edgeIndex])
                continue;

            allEdgesValid = false;
            break;
        }

        if (allEdgesValid)
        {
            BoundaryPathData closedPath = new();

            closedPath.Points.AddRange(points);
            closedPath.Points.Add(points[0]);

            cell.BoundaryPaths.Add(closedPath);
            return;
        }

        // Znajdujemy niepoprawną krawędź, od której zaczniemy
        // przechodzenie po punktach.
        int firstInvalidEdge = validEdges.FindIndex(x => !x);
        int startIndex = (firstInvalidEdge + 1) % points.Count;

        List<Vector2> currentPath = new();

        for (int i = 0; i < points.Count; i++)
        {
            int currentIndex = (startIndex + i) % points.Count;

            currentPath.Add(points[currentIndex]);

            // Ostatni punkt zamyka przejście przez całą listę.
            // Krawędź z niego do punktu startowego jest niepoprawna.
            if (i == points.Count - 1)
            {
                AddOpenPath(currentPath, polygon, cell.BoundaryPaths);
                break;
            }

            if (!validEdges[currentIndex])
            {
                AddOpenPath(currentPath, polygon, cell.BoundaryPaths);
                currentPath = new List<Vector2>();
            }
        }
    }

    /// <summary>
    /// Projects membrane vertices onto the nearest valid boundary-path intersection
    /// along the ray starting at the cell's average vertex.
    /// </summary>
    public static void CastMembranePoints(CellData cell)
    {
        for (int i = 0; i < cell.VertexCount; i++)
        {
            Vector2 membranePoint = cell.Vertices2D[i];

            if (!TryCastMembranePoint(membranePoint,
                    cell.AverageVertex,
                    cell.BoundaryPaths,
                    out Vector2 hitPoint))
            {
                continue;
            }

            cell.Vertices2D[i] = hitPoint;
        }
    }

    private static bool TryCastMembranePoint(Vector2 membranePoint,
        Vector2 averagePoint,
        List<BoundaryPathData> boundaryPaths,
        out Vector2 hitPoint)
    {
        hitPoint = Vector2.Zero;

        Vector2 toPoint = membranePoint - averagePoint;
        float toPointLength = toPoint.Length();

        if (toPointLength < Tolerance)
            return false;

        Vector2 direction = toPoint / toPointLength;

        float closestDistance = float.MaxValue;
        bool found = false;

        foreach (BoundaryPathData path in boundaryPaths)
        {
            for (int i = 0; i < path.Points.Count - 1; i++)
            {
                Vector2 a = path.Points[i];
                Vector2 b = path.Points[i + 1];

                if (!TryRaySegmentIntersection(membranePoint,
                        direction,
                        a,
                        b,
                        out Vector2 intersection))
                {
                    continue;
                }

                // direction jest jednostkowy, więc to jest odległość w jednostkach świata.
                float distance = (intersection - membranePoint).Dot(direction);

                if (distance < -Tolerance || distance >= closestDistance)
                    continue;

                closestDistance = distance;
                hitPoint = intersection;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    ///   rayDirection MUSI być jednostkowy.
    /// </summary>
    private static bool TryRaySegmentIntersection(Vector2 rayOrigin,
        Vector2 rayDirection,
        Vector2 segmentStart,
        Vector2 segmentEnd,
        out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        Vector2 segmentDirection = segmentEnd - segmentStart;
        float segmentLength = segmentDirection.Length();

        if (segmentLength < Tolerance)
            return false;

        Vector2 offset = segmentStart - rayOrigin;

        // |denominator| = |segmentDirection| * sin(kąta), więc porównujemy
        // z długością odcinka -> test równoległości nie zależy od skali.
        float denominator = rayDirection.Cross(segmentDirection);

        if (MathF.Abs(denominator) < GeometryTolerance.Parallel * segmentLength)
            return false;

        // t jest w jednostkach świata (rayDirection jednostkowy),
        // u jest bezwymiarowe (0..1 wzdłuż odcinka).
        float t = offset.Cross(segmentDirection) / denominator;
        float u = offset.Cross(rayDirection) / denominator;

        float uTolerance = Tolerance / segmentLength;

        if (t < -Tolerance || u < -uTolerance || u > 1f + uTolerance)
            return false;

        intersection = rayOrigin + rayDirection * t;
        return true;
    }

    private static void AddSinglePointPath(Vector2 point,
        List<Vector2> polygon,
        List<BoundaryPathData> boundaryPaths)
    {
        Vector2 incomingTangent = GetIncomingTangent(point, polygon);
        Vector2 outgoingTangent = GetOutgoingTangent(point, polygon);

        boundaryPaths.Add(new BoundaryPathData
        {
            Points = { incomingTangent, point, outgoingTangent },
        });
    }

    private static void AddOpenPath(List<Vector2> points,
        List<Vector2> polygon,
        List<BoundaryPathData> result)
    {
        if (points.Count == 0)
            return;

        Vector2 incomingTangent = GetIncomingTangent(points[0], polygon);
        Vector2 outgoingTangent = GetOutgoingTangent(points[^1], polygon);

        BoundaryPathData path = new();

        path.Points.Add(incomingTangent);
        path.Points.AddRange(points);
        path.Points.Add(outgoingTangent);

        result.Add(path);
    }

    // Sprawdza, czy odcinek jest poprawny:
    // 1. Nie przecina wnętrza membrany.
    // 2. Cała membrana znajduje się po prawej stronie odcinka.
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
            // Odległość ze znakiem (dodatnia = po lewej), w jednostkach świata.
            if (SideOf(a, b, vertex) > Tolerance)
                return false;
        }

        return true;
    }

    // Sprawdza przecięcie odcinka z wnętrzem wypukłego wielokąta
    // przez przycinanie parametryczne odcinka (Cyrus-Beck).
    // Wszystkie warunki są liczone w jednostkach odległości (znormalizowane długością krawędzi).
    /// <summary>
    /// Determines whether a segment passes through the interior of a convex polygon
    /// using parametric clipping.
    /// </summary>
    private static bool SegmentEntersPolygonInterior(Vector2 a,
        Vector2 b,
        List<Vector2> polygon)
    {
        Vector2 direction = b - a;
        float segmentLength = direction.Length();

        if (segmentLength < Tolerance)
            return false;

        float tMin = 0f;
        float tMax = 1f;

        // Wnętrze musi leżeć ściśle (głębiej niż Tolerance) po prawej stronie każdej krawędzi.
        const float limit = -Tolerance;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 edgeStart = polygon[i];
            Vector2 edgeEnd = polygon[(i + 1) % polygon.Count];
            Vector2 edge = edgeEnd - edgeStart;

            float edgeLength = edge.Length();

            // Zdublowany wierzchołek - krawędź nie niesie informacji.
            if (edgeLength < GeometryTolerance.Parallel)
                continue;

            // f0: odległość początku odcinka od prostej krawędzi (dodatnia = po lewej).
            // fd: zmiana tej odległości na całej długości odcinka (t: 0 -> 1).
            float f0 = Cross(edge, a - edgeStart) / edgeLength;
            float fd = Cross(edge, direction) / edgeLength;

            if (MathF.Abs(fd) < GeometryTolerance.Parallel)
            {
                if (f0 > limit)
                    return false;

                continue;
            }

            float t = (limit - f0) / fd;

            if (fd > 0)
                tMax = MathF.Min(tMax, t);
            else
                tMin = MathF.Max(tMin, t);

            if (tMin >= tMax)
                return false;
        }

        // Długość części odcinka wewnątrz wielokąta, w jednostkach świata (nie w jednostkach t).
        return (tMax - tMin) * segmentLength > Tolerance;
    }

    // Styczna wejściowa:
    // kierunek stycznej to T -> P.
    // Membrana musi być po prawej stronie tego kierunku.
    private static Vector2 GetIncomingTangent(Vector2 point,
        List<Vector2> polygon)
    {
        return FindTangent(point, polygon, incoming: true);
    }

    // Styczna wyjściowa:
    // kierunek stycznej to P -> T.
    // Membrana musi być po prawej stronie tego kierunku.
    private static Vector2 GetOutgoingTangent(Vector2 point,
        List<Vector2> polygon)
    {
        return FindTangent(point, polygon, incoming: false);
    }

    /// <summary>
    /// Finds the nearest polygon vertex that forms a valid incoming or outgoing
    /// tangent from a point outside the polygon.
    /// </summary>
    private static Vector2 FindTangent(Vector2 point,
        List<Vector2> polygon,
        bool incoming)
    {
        if (IsPointInsideOrOnPolygon(point, polygon))
        {
            throw new InvalidOperationException($"Punkt {point} leży wewnątrz membrany lub na jej obrysie. " +
                "Nie można wyznaczyć zewnętrznej stycznej.");
        }

        Vector2? bestCandidate = null;
        float bestDistance = float.MaxValue;

        foreach (Vector2 tangentPoint in polygon)
        {
            bool isValid = true;

            foreach (Vector2 vertex in polygon)
            {
                // incoming: prosta T -> P, outgoing: prosta P -> T.
                float side = incoming ?
                    SideOf(tangentPoint, point, vertex) :
                    SideOf(point, tangentPoint, vertex);

                if (side > Tolerance)
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
            throw new InvalidOperationException($"Nie udało się znaleźć stycznej dla punktu {point}.");
        }

        return bestCandidate.Value;
    }

    private static bool IsPointInsideOrOnPolygon(Vector2 point,
        List<Vector2> polygon)
    {
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];

            if (SideOf(a, b, point) > Tolerance)
                return false;
        }

        return true;
    }
    /// <summary>
    /// Removes points that are closer to an already accepted point
    /// than the configured geometric tolerance.
    /// </summary>
    /// <summary>
    /// Removes points that are closer to an already accepted point than the
    /// configured tolerance. Compacts the input list in place to avoid allocating
    /// a second list.
    /// </summary>
    private static List<Vector2> RemoveDuplicatePoints(List<Vector2> points)
    {
        const float toleranceSquared = Tolerance * Tolerance;

        int uniquePointCount = 0;

        for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
        {
            Vector2 candidatePoint = points[pointIndex];
            bool isDuplicate = false;

            for (int existingPointIndex = 0;
                 existingPointIndex < uniquePointCount;
                 existingPointIndex++)
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
            uniquePointCount++;
        }

        if (uniquePointCount < points.Count)
            points.RemoveRange(uniquePointCount, points.Count - uniquePointCount);

        return points;
    }

    private static Vector2 GetPolygonCenter(List<Vector2> polygon)
    {
        Vector2 center = Vector2.Zero;

        foreach (Vector2 vertex in polygon)
            center += vertex;

        return center / polygon.Count;
    }

    private static float SignedArea(List<Vector2> polygon)
    {
        float area = 0f;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];

            area += Cross(a, b);
        }

        return area * 0.5f;
    }

    /// <summary>
    ///   Odległość ze znakiem punktu p od prostej a -> b (dodatnia = po lewej stronie kierunku a -> b).
    ///   W przeciwieństwie do surowego Cross nie zależy od długości odcinka a-b.
    /// </summary>
    private static float SideOf(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 direction = b - a;
        float length = direction.Length();

        if (length < GeometryTolerance.Parallel)
            return 0f;

        return Cross(direction, p - a) / length;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }
}
