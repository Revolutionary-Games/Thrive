using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;

/// <summary>
///   Coordinator that implements two-pass membrane generation.
///   - First pass: generate base membranes per cell
///   - Second pass: generate stretched multicellular membranes if needed. They are not persistently cached. Instead,
///   a finished stretched result is handed off exactly once to the main-thread request that is waiting for it
/// </summary>
public static class MembraneGenerationCoordinator
{
    private static readonly ConcurrentDictionary<long, ColonyTracker> ColonyTrackers = new();

    /// <summary>
    ///   Stores finished stretched multicellular membranes. Unlike a cache, an entry here is
    ///   removed the moment it is consumed.
    /// </summary>
    private static readonly ConcurrentDictionary<long, MembranePointData> FinishedMulticellularMembranes = new();

    /// <summary>
    ///   Handles membrane generation requests. For single-cell requests the list contains one hash.
    /// </summary>
    public static List<long> HandleGenerationRequest(ref MembraneGenerationParameters generationParameters)
    {
        var generatedMembranes = new List<long>();
        var generator = MembraneShapeGenerator.GetThreadSpecificGenerator();
        var isSingleCell = generationParameters.GrownCellsData.Length == 0 ||
            !generationParameters.IsMulticellularMembraneDataValid;

        if (isSingleCell)
        {
            var membranePointData = generator.GenerateMicrobeShape(ref generationParameters);
            var hash = ProceduralDataCache.Instance.WriteMembraneData(ref membranePointData);
            generatedMembranes.Add(hash);
            return generatedMembranes;
        }

        generationParameters.IsPreMulticellularStretch = true;

        var stretchedHash = generationParameters.ComputeMembraneDataHash();
        var singleCellMembranePointData = ProceduralDataCache.Instance.ReadMembraneData(stretchedHash);
        if (singleCellMembranePointData == null)
        {
            singleCellMembranePointData = generator.GenerateMicrobeShape(ref generationParameters, true);
            ProceduralDataCache.Instance.WriteMembraneData(ref singleCellMembranePointData);
        }
        else
        {
            // Cache hit — hexes are no longer needed, return to pool
            ArrayPool<Vector2>.Shared.Return(generationParameters.HexPositions);
        }

        var grownCellsData = generationParameters.GrownCellsData;
        var cellPosition = generationParameters.CurrentCellMulticellularMembraneGenerationCellData.Position;
        var cellOrientation = generationParameters.CurrentCellMulticellularMembraneGenerationCellData.Orientation;

        var tracker = ColonyTrackers.GetOrAdd(generationParameters.LeaderCellKey,
            _ => new ColonyTracker { ExpectedCount = grownCellsData.Length });

        var multicellularMembraneData = new MulticellularMembraneGenerationCellData(cellPosition, cellOrientation);

        var singleCellData = new NeighbourData(generationParameters.CellKey, multicellularMembraneData,
            singleCellMembranePointData);

        var angle = MembraneShapeGenerator.GetOrientationAngle(cellOrientation);
        var modifiedVetices2D = singleCellMembranePointData.Vertices2D
            .Select(v => MembraneShapeGenerator.RotatePoint(v, angle) + cellPosition).ToArray();

        tracker.NeighboursData[generationParameters.CellKey] = singleCellData;
        tracker.CellsData[generationParameters.CellKey] = new CellData(generationParameters.CellKey,
            modifiedVetices2D.ToList().GetRange(0, singleCellMembranePointData.VertexCount), multicellularMembraneData);

        // TODO: Maybe implement a system that clears trackers every now and then that are inactive
        // for too long to avoid potential memory leaks

        // Colony not yet complete — return empty
        if (tracker.NeighboursData.Count < tracker.ExpectedCount)
            return generatedMembranes;

        // Pass 2: all base membranes are ready. Use a flag to ensure exactly one thread executes the second pass.
        if (!tracker.TryBeginSecondPass())
            return generatedMembranes;

        GetPointsInfo(tracker);

        foreach (var (key, cellData) in tracker.CellsData)
        {
            var pos = cellData.MulticellularMembraneGenerationCellData.Position;
            var ang = MembraneShapeGenerator.GetOrientationAngle(cellData.MulticellularMembraneGenerationCellData
                .Orientation);
            cellData.Vertices2D = cellData.Vertices2D.Select(v => MembraneShapeGenerator.RotatePoint(v - pos, -ang))
                .ToList();

            // GD.Print(string.Join(", ", cellData.Vertices2D.Select(v => $"({v.X:F3}, {v.Y:F3})")));
        }

        // Return ALL resolved hashes so every cell's pending entry gets cleared
        foreach (var (key, data) in tracker.CellsData)
        {
            var cellData = tracker.CellsData[key];
            var multicellularMembrane =
                generator.GenerateMulticellularMembrane(key, tracker.NeighboursData, cellData,
                    generationParameters.LeaderCellKey);

            AddMulticellularMembrane(data.CellKey, multicellularMembrane);
            generatedMembranes.Add(data.CellKey);
        }

        ColonyTrackers.TryRemove(generationParameters.LeaderCellKey, out _);

        return generatedMembranes;
    }

    private static float ClosestPointsOnSegments(Vector2 a0,
        Vector2 a1,
        Vector2 b0,
        Vector2 b1,
        out Vector2 closestA,
        out Vector2 closestB)
    {
        var d1 = a1 - a0;
        var d2 = b1 - b0;
        var r = a0 - b0;

        var a = d1.Dot(d1);
        var e = d2.Dot(d2);
        var f = d2.Dot(r);

        float s;
        float t;

        const float epsilon = 1e-8f;

        if (a <= epsilon && e <= epsilon)
        {
            closestA = a0;
            closestB = b0;
            return closestA.DistanceSquaredTo(closestB);
        }

        if (a <= epsilon)
        {
            s = 0f;
            t = Math.Clamp(f / e, 0f, 1f);
        }
        else
        {
            var c = d1.Dot(r);

            if (e <= epsilon)
            {
                t = 0f;
                s = Math.Clamp(-c / a, 0f, 1f);
            }
            else
            {
                var b = d1.Dot(d2);
                var denominator = a * e - b * b;

                s = denominator > epsilon ? Math.Clamp((b * f - c * e) / denominator, 0f, 1f) : 0f;

                t = (b * s + f) / e;

                if (t < 0f)
                {
                    t = 0f;
                    s = Math.Clamp(-c / a, 0f, 1f);
                }
                else if (t > 1f)
                {
                    t = 1f;
                    s = Math.Clamp((b - c) / a, 0f, 1f);
                }
            }
        }

        closestA = a0 + d1 * s;
        closestB = b0 + d2 * t;

        return closestA.DistanceSquaredTo(closestB);
    }

    private static void GetPointsInfo(ColonyTracker tracker)
    {
        var maxDistance = 1500.0f;
        var info = "";

        foreach (var (key, data) in tracker.CellsData)
        {
            info += string.Join(", ",
                data.Vertices2D.Select(v => $"({v.X:F3}, {v.Y:F3})"));
            info += "\n";
        }

        foreach (var (cellKey, cellData) in tracker.CellsData)
        {
            var candidates = new List<LineData>();

            var cellPosition =
                cellData.MulticellularMembraneGenerationCellData.Position;

            var averageVertex = cellData.AverageVertex;

            var verticesA = cellData.Vertices2D
                .Where(p => p.X != 0 || p.Y != 0)
                .ToArray();

            if (verticesA.Length < 2)
                continue;

            foreach (var neighbourData in tracker.CellsData)
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

                var verticesB = neighbourCell.Vertices2D
                    .Where(p => p.X != 0 || p.Y != 0)
                    .ToArray();

                if (verticesB.Length < 2)
                    continue;

                var minDistance = float.MaxValue;

                var closestA = Vector2.Zero;
                var closestB = Vector2.Zero;

                // Porównywanie wszystkich odcinków obu membran.
                for (var i = 0; i < verticesA.Length; i++)
                {
                    var a0 = verticesA[i];
                    var a1 = verticesA[(i + 1) % verticesA.Length];

                    for (var j = 0; j < verticesB.Length; j++)
                    {
                        var b0 = verticesB[j];
                        var b1 = verticesB[(j + 1) % verticesB.Length];

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

            info = GetOnlyCloseLines(tracker,
                cellData,
                candidates,
                averageVertex,
                info);
        }
        
        // RemoveAsymmetricLines(tracker);
        CopyLinesToNeighbours(tracker);

        foreach (var (cellKey, cellData) in tracker.CellsData)
        {
            var averageVertex = cellData.AverageVertex;
            info = GetIntersections(cellData, averageVertex, info);
        }

        // Filter incomplete triples
        FilterInconsistentIntersections(tracker);

        // Print only intersections that survived filtering
        foreach (var (cellKey, cellData) in tracker.CellsData)
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

        foreach (var (key, data) in tracker.CellsData)
        {
            info += string.Join(", ",
                data.Vertices2D.Select(v => $"({v.X:F3}, {v.Y:F3})"));
            info += "\n";
        }

        if (tracker.NeighboursData.Count >= 3)
        {
            GD.Print();
            GD.Print($"{info}");
        }
    }

    private static void SmoothVertexRegion(CellData cellData)
    {
        var vertexCount = cellData.Vertices2D.Count;

        for (int i = 0; i <= vertexCount; ++i)
        {
            int index = i % vertexCount;
            int previousPointIndex = (i + vertexCount - 1) % vertexCount;
            int nextPointIndex = (i + 1) % vertexCount;

            cellData.Vertices2D[index] = (cellData.Vertices2D[previousPointIndex] + cellData.Vertices2D[index] * 3 +
                cellData.Vertices2D[nextPointIndex]) * 0.2f;
        }
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

    private static string GetOnlyCloseLines(ColonyTracker tracker, CellData cell, List<LineData> candidates,
        Vector2 averageVertex,
        string info)
    {
        const float tolerance = GeometryTolerance.Distance;

        foreach (var candidate in candidates)
        {
            var middlePoint = candidate.MiddlePoint;

            var line = candidate.Line;

            if (Math.Abs(line.X) < 0.00001f)
            {
                info += $"x = {middlePoint.X:F3}\n";
            }
            else
            {
                var a = line.Y / line.X;
                var b = middlePoint.Y - a * middlePoint.X;

                info += $"y = {a:F3}x + {b:F3}\n";
            }

            info += $"({middlePoint.X:F3}, {middlePoint.Y:F3})\n";
        }

        tracker.CellsData[cell.CellKey].LinesData = candidates
            .Where((candidate, index) =>
            {
                for (var i = 0; i < candidates.Count; i++)
                {
                    if (i == index)
                        continue;

                    var otherLine = candidates[i];

                    var sideAverage = SignedDistanceToLine(otherLine, averageVertex);
                    var sidePoint = SignedDistanceToLine(otherLine, candidate.MiddlePoint);

                    var separated =
                        (sideAverage > tolerance && sidePoint < -tolerance) ||
                        (sideAverage < -tolerance && sidePoint > tolerance);

                    if (separated)
                        return false;
                }

                return true;
            })
            .ToDictionary(candidate => candidate.LineKey, candidate => candidate);

        // foreach (var (key, candidate) in tracker.CellsData[cell.CellKey].LinesData)
        // {
        //     var middlePoint = candidate.MiddlePoint;
        //
        //     var line = candidate.Line;
        //
        //     if (Math.Abs(line.X) < 0.00001f)
        //     {
        //         info += $"x = {middlePoint.X:F3}\n";
        //     }
        //     else
        //     {
        //         var a = line.Y / line.X;
        //         var b = middlePoint.Y - a * middlePoint.X;
        //
        //         info += $"y = {a:F3}x + {b:F3}\n";
        //     }
        //
        //     info += $"({middlePoint.X:F3}, {middlePoint.Y:F3})\n";
        // }

        return info;
    }

    private static string GetIntersections(CellData cell,
        Vector2 averageVertex, string info)
    {
        var lines = cell.LinesData.Values.ToList();
        var intersections = new Dictionary<long, IntersetionData>();

        // Jedna tolerancja do widoczności i do duplikatów.
        const float tolerance = GeometryTolerance.Distance;

        for (var i = 0; i < lines.Count; i++)
        {
            for (var j = i + 1; j < lines.Count; j++)
            {
                var line1 = lines[i];
                var line2 = lines[j];

                if (!TryGetIntersection(line1, line2, out var intersection))
                    continue;

                var isVisible = true;

                // Check intersection against every line
                foreach (var line in lines)
                {
                    var sideAverage = SignedDistanceToLine(line, averageVertex);
                    var sideIntersection = SignedDistanceToLine(line, intersection);

                    var separated =
                        (sideAverage > tolerance && sideIntersection < -tolerance) ||
                        (sideAverage < -tolerance && sideIntersection > tolerance);

                    if (separated)
                    {
                        isVisible = false;
                        break;
                    }
                }

                if (!isVisible)
                    continue;

                // Avoid duplicate intersection points.
                // Przy punkcie wspólnym 3+ linii zostaje tu dowolna (pierwsza) para -
                // to jest OK, bo FilterInconsistentIntersections nie opiera się już na tej parze,
                // tylko na zbiorze wszystkich linii przechodzących przez punkt.
                var alreadyExists = intersections.Any(existing =>
                    existing.Value.Position.DistanceSquaredTo(intersection) <
                    tolerance * tolerance);

                if (alreadyExists)
                    continue;

                var intersectionData = new IntersetionData(intersection,
                    cell.CellKey,
                    line1.OtherCellKey,
                    line1.LineKey,
                    line2.LineKey);

                intersections.Add(intersectionData.IntersectionId, intersectionData);
            }
        }

        cell.IntersectionsData = intersections;
        return info;
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

    private static void FilterInconsistentIntersections(ColonyTracker tracker)
    {
        // (komórka, przecięcie) -> sygnatura
        var signatures = new Dictionary<(long Cell, long Intersection), long[]>();

        // sygnatura -> komórki, które mają przecięcie o dokładnie tej sygnaturze
        var owners = new Dictionary<string, HashSet<long>>();

        // First pass: policz sygnatury wszystkich przecięć
        foreach (var (cellKey, cell) in tracker.CellsData)
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
        foreach (var (cellKey, cell) in tracker.CellsData)
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
    ///   Linia komórki A -> B jest zachowana tylko wtedy, gdy komórka B zachowała linię B -> A.
    ///   Filtrowanie w GetOnlyCloseLines jest liczone osobno dla każdej komórki (inny averageVertex),
    ///   więc wynik może być asymetryczny - tutaj go symetryzujemy.
    /// </summary>
    // private static void RemoveAsymmetricLines(ColonyTracker tracker)
    // {
    //     // Snapshot: które pary (komórka, sąsiad) przetrwały filtrowanie.
    //     var surviving = new HashSet<(long Cell, long Other)>();
    //
    //     foreach (var (_, cell) in tracker.CellsData)
    //     {
    //         foreach (var line in cell.LinesData.Values)
    //             surviving.Add((cell.CellKey, line.OtherCellKey));
    //     }
    //
    //     foreach (var (_, cell) in tracker.CellsData)
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
    private static void CopyLinesToNeighbours(ColonyTracker tracker)
    {
        // Najpierw zbieramy, potem dodajemy - nie modyfikujemy słowników w trakcie iteracji
        // i każda komórka jest oceniana względem stanu sprzed kopiowania.
        var toAdd = new List<(CellData Target, LineData Line)>();

        foreach (var (_, cell) in tracker.CellsData)
        {
            foreach (var line in cell.LinesData.Values)
            {
                if (!tracker.CellsData.TryGetValue(line.OtherCellKey, out var other))
                    continue;

                var otherAlreadyHasIt = other.LinesData.Values
                    .Any(l => l.OtherCellKey == cell.CellKey);

                if (otherAlreadyHasIt)
                    continue;

                toAdd.Add((other,
                    new LineData(line.MiddlePoint,
                        -line.Normal,
                        other.CellKey,
                        cell.CellKey)));
            }
        }

        foreach (var (target, line) in toAdd)
            target.LinesData[line.LineKey] = line;
    }

    /// <summary>
    ///   Attempts to retrieve and remove a finished stretched multicellular membrane.
    /// </summary>
    public static MembranePointData? TryTakeFinishedMulticellularMembrane(long key)
    {
        FinishedMulticellularMembranes.TryRemove(key, out var data);
        return data;
    }

    /// <summary>
    ///   Disposes and removes any finished multicellular membranes that were computed but never collected,
    ///   and clears all colony trackers.
    /// </summary>
    public static void ClearCoordinator()
    {
#if DEBUG
        if (!FinishedMulticellularMembranes.IsEmpty)
        {
            GD.PrintErr("FinishedMulticellularMembranes is not empty");
        }

        if (!ColonyTrackers.IsEmpty)
        {
            GD.PrintErr("ColonyTrackers is not empty");
        }
#endif

        foreach (var key in FinishedMulticellularMembranes.Keys)
        {
            if (FinishedMulticellularMembranes.TryRemove(key, out var orphaned))
                orphaned.Dispose();
        }

        ColonyTrackers.Clear();
    }

    private static void AddMulticellularMembrane(long cellKey, MembranePointData data)
    {
        if (FinishedMulticellularMembranes.TryRemove(cellKey, out var previous))
        {
            GD.PrintErr("FinishedMulticellularMembranes was overwritten");
            previous.Dispose();
        }

        FinishedMulticellularMembranes[cellKey] = data;
    }

    private class ColonyTracker
    {
        public int ExpectedCount;
        public ConcurrentDictionary<long, NeighbourData> NeighboursData = new();
        public ConcurrentDictionary<long, CellData> CellsData = new();
        private int secondPassStarted;

        /// <summary>
        ///   Returns true if the current thread should run the second pass
        ///   and false if another thread already runs the second pass
        /// </summary>
        public bool TryBeginSecondPass()
        {
            return Interlocked.CompareExchange(ref secondPassStarted, 1, 0) == 0;
        }
    }
}
