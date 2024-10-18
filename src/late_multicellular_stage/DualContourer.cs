using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Godot.NativeInterop;
using Array = Godot.Collections.Array;

/// <summary>
///   Generates meshes from mathematical functions defined in 3D space. Uses Dual contouring for this.
/// </summary>
public class DualContourer
{
    public int PointsPerUnit = 4;

    /// <summary>
    ///   Specifies start of generated box. Inclusive.
    /// </summary>
    public Vector3 UnitsFrom = new(-4.0f, -4.0f, -4.0f);

    /// <summary>
    ///   Specifies end of generated box. Inclusive.
    /// </summary>
    public Vector3 UnitsTo = new(4.0f, 4.0f, 4.0f);

    public bool Smoothen = true;

    public IMeshGeneratingFunction MathFunction;

    private static readonly Dictionary<int, Vector3I[]> LookupTable = new();

    public DualContourer(IMeshGeneratingFunction mathFunction)
    {
        CalculateLookupTableIfNeeded();
        MathFunction = mathFunction;
    }

    public Mesh DualContour()
    {
        var sw = new Stopwatch();
        sw.Start();

        // TODO: when this feature is polished up the amount of allocations done in this method should be attempted
        // to be reduced whenever possible (probably with persistent data structures that are cleared before each use)

        // int is point's index in points[]
        var placedPoints = new Dictionary<Vector3I, int>();

        Vector3I gridFrom = new Vector3I((int)(UnitsFrom.X * PointsPerUnit), (int)(UnitsFrom.Y * PointsPerUnit),
            (int)(UnitsFrom.Z * PointsPerUnit));
        Vector3I gridTo = new Vector3I((int)(UnitsTo.X * PointsPerUnit), (int)(UnitsTo.Y * PointsPerUnit),
            (int)(UnitsTo.Z * PointsPerUnit));

        // Safety checks not to blow up PCs
        gridFrom.Clamp(new Vector3I(-100, -100, -100), new Vector3I(100, 100, 100));
        gridTo.Clamp(new Vector3I(-100, -100, -100), new Vector3I(100, 100, 100));

        var capacity = PointsPerUnit * (gridTo.X - gridFrom.X) * (gridTo.Y - gridFrom.Y) *
            (gridTo.Z - gridFrom.Z) / 10;

        var points = new List<Vector3>(capacity);
        var triIndices = new List<int>(capacity);

        // Shape point is located at intersection of three lines of the grid
        // if a point's value is true, the point is inside shape, otherwise outside
        var shapePoints = new bool[gridTo.X - gridFrom.X + 2,
            gridTo.Y - gridFrom.Y + 2,
            gridTo.Z - gridFrom.Z + 2];

        // Points at grid cell centers at coordinates of gridpoint are related to shape points with coordinates of:
        // [gridpoint.X; gridpoint.X + 1] for x, [gridpoint.Y; gridpoint.Y + 1] for y,
        // [gridpoint.Z; gridpoint.Z + 1] for z

        CalculatePoints(shapePoints, gridFrom, gridTo);

        Vector3I gridOffset = -gridFrom;

        for (int x = gridFrom.X; x <= gridTo.X; ++x)
        {
            for (int y = gridFrom.Y; y <= gridTo.Y; ++y)
            {
                for (int z = gridFrom.Z; z <= gridTo.Z; ++z)
                {
                    // var realPos = new Vector3(x, y, z) / PointsPerUnit;
                    var gridPos = new Vector3I(x, y, z);

                    var tris = GetBestTriangles(x + gridOffset.X, y + gridOffset.Y, z + gridOffset.Z, shapePoints);

                    if (tris == null)
                        continue;

                    PlaceTriangles(gridPos, tris, points, triIndices, placedPoints);
                }
            }
        }

        var normals = new Vector3[points.Count];

        if (Smoothen)
        {
            AdjustVertices(points);
            AdjustVertices(points, 0.25f, normals);
        }

        // Subdivision can be used multiple times, but triangle count increases exponentially
        CatmullClarkSubdivision(points, triIndices, ref normals);

        var colors = new Color[points.Count];

        SetColours(points, colors);

        var arrays = new Array();
        arrays.Resize((int)Mesh.ArrayType.Max);

        arrays[(int)Mesh.ArrayType.Vertex] = points.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = triIndices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Color] = colors;

        ArrayMesh mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        sw.Stop();
        GD.Print($"Generated a mesh in {sw.Elapsed}");

        return mesh;
    }

    private static void CalculateLookupTableIfNeeded()
    {
        if (LookupTable.Count > 0)
            return;

        // Doesn't add triangles that go to negative x, y, or z to prevent triangles overlapping

        bool reverseTriangleFaces = false;

        for (int i = 1; i <= 254; ++i)
        {
            Cube cube = new Cube(i); // Make cube from index

            // [0;0;0] point is the origin (center of the cube)
            // All other points point to neighbouring cubes.
            // All the coordinates are one of the following: -1, 0, or 1
            List<Vector3I> tris = new List<Vector3I>(6);

            // Use XOR to ensure that only one of the two points is in the shape
            // If both or none are in the shape, then there are no triangles going between them

            // Parallel to x face, plane going through z = 1 and y = 1
            if (cube.Points[0, 1, 1] ^ cube.Points[1, 1, 1])
            {
                tris.Add(new Vector3I(0, 0, 0));
                tris.Add(new Vector3I(0, 1, 1));
                tris.Add(new Vector3I(0, 0, 1));

                tris.Add(new Vector3I(0, 0, 0));
                tris.Add(new Vector3I(0, 1, 0));
                tris.Add(new Vector3I(0, 1, 1));

                if (cube.Points[0, 1, 1])
                {
                    int lastID = tris.Count - 1;

                    (tris[lastID], tris[lastID - 1]) = (tris[lastID - 1], tris[lastID]);
                    (tris[lastID - 3], tris[lastID - 4]) = (tris[lastID - 4], tris[lastID - 3]);
                }
            }

            // Parallel to z face, plane going through x = 1 and y = 1
            if (cube.Points[1, 1, 0] ^ cube.Points[1, 1, 1])
            {
                tris.Add(new Vector3I(0, 0, 0));
                tris.Add(new Vector3I(1, 0, 0));
                tris.Add(new Vector3I(1, 1, 0));

                tris.Add(new Vector3I(0, 0, 0));
                tris.Add(new Vector3I(1, 1, 0));
                tris.Add(new Vector3I(0, 1, 0));

                if (cube.Points[1, 1, 0])
                {
                    int lastID = tris.Count - 1;

                    (tris[lastID], tris[lastID - 1]) = (tris[lastID - 1], tris[lastID]);
                    (tris[lastID - 3], tris[lastID - 4]) = (tris[lastID - 4], tris[lastID - 3]);
                }
            }

            // Parallel to y face, plane going through z = 1 and x = 1
            if (cube.Points[1, 0, 1] ^ cube.Points[1, 1, 1])
            {
                tris.Add(new Vector3I(0, 0, 0));
                tris.Add(new Vector3I(1, 0, 1));
                tris.Add(new Vector3I(1, 0, 0));

                tris.Add(new Vector3I(0, 0, 0));
                tris.Add(new Vector3I(0, 0, 1));
                tris.Add(new Vector3I(1, 0, 1));

                if (cube.Points[1, 0, 1])
                {
                    int lastID = tris.Count - 1;

                    (tris[lastID], tris[lastID - 1]) = (tris[lastID - 1], tris[lastID]);
                    (tris[lastID - 3], tris[lastID - 4]) = (tris[lastID - 4], tris[lastID - 3]);
                }
            }

            if (reverseTriangleFaces)
            {
                for (int j = 1; j < tris.Count; j += 3)
                {
                    (tris[j], tris[j + 1]) = (tris[j + 1], tris[j]);
                }
            }

            LookupTable.Add(i, tris.ToArray());
        }
    }

    private bool IsInShape(Vector3 pos)
    {
        if (MathFunction.GetValue(pos) > MathFunction.SurfaceValue)
            return true;

        return false;
    }

    private void CalculatePoints(bool[,,] shapePoints, Vector3I gridFrom, Vector3I gridTo)
    {
        var tasks = new List<Task>();

        Vector3I gridOffset = -gridFrom;

        int availableThreads = TaskExecutor.Instance.ParallelTasks;

        int threadsPerEdge = Math.Clamp((int)MathF.Ceiling(2.0f * availableThreads / 8.0f), 2, 3);

        if (availableThreads > 27)
            threadsPerEdge = 4;

        int stepX = (gridTo.X - gridFrom.X) / threadsPerEdge;
        int stepY = (gridTo.Y - gridFrom.Y) / threadsPerEdge;
        int stepZ = (gridTo.Z - gridFrom.Z) / threadsPerEdge;

        for (int x = gridFrom.X; x <= gridTo.X; x += stepX + 1)
        {
            for (int y = gridFrom.Y; y <= gridTo.Y; y += stepY + 1)
            {
                for (int z = gridFrom.Z; z <= gridTo.Z; z += stepZ + 1)
                {
                    // TODO: avoid lambda captures of variables here (at least from and this is captured)
                    // that cause memory allocations
                    Vector3I from = new Vector3I(x, y, z);
                    Vector3I to = from + new Vector3I(stepX, stepY, stepZ);
                    to = to.Clamp(from, gridTo);
                    var task = new Task(() => CalculatePointsInRange(shapePoints, from, to, gridOffset));
                    tasks.Add(task);
                }
            }
        }

        TaskExecutor.Instance.RunTasks(tasks);
    }

    private void CalculatePointsInRange(bool[,,] shapePoints, Vector3I gridFrom, Vector3I gridTo, Vector3I gridOffset)
    {
        float realFactor = 1.0f / PointsPerUnit;

        for (int x = gridFrom.X; x <= gridTo.X; ++x)
        {
            for (int y = gridFrom.Y; y <= gridTo.Y; ++y)
            {
                for (int z = gridFrom.Z; z <= gridTo.Z; ++z)
                {
                    // var gridPos = new Vector3I(x, y, z);
                    var realPos = new Vector3(x - 0.5f, y - 0.5f, z - 0.5f) * realFactor;

                    shapePoints[x + gridOffset.X, y + gridOffset.Y, z + gridOffset.Z] = IsInShape(realPos);
                }
            }
        }
    }

    private void SetColours(List<Vector3> points, Color[] colours)
    {
        for (int i = 0; i < colours.Length; ++i)
        {
            colours[i] = MathFunction.GetColour(points[i]);
        }
    }

    private int IdOf(bool x0Y0Z0, bool x0Y0Z1, bool x0Y1Z0, bool x0Y1Z1, bool x1Y0Z0, bool x1Y0Z1, bool x1Y1Z0,
        bool x1Y1Z1)
    {
        int id = 0;

        if (x0Y0Z0)
            id += 1;
        if (x0Y0Z1)
            id += 2;
        if (x0Y1Z0)
            id += 4;
        if (x0Y1Z1)
            id += 8;
        if (x1Y0Z0)
            id += 16;
        if (x1Y0Z1)
            id += 32;
        if (x1Y1Z0)
            id += 64;
        if (x1Y1Z1)
            id += 128;

        return id;
    }

    private Vector3I[]? GetBestTriangles(int x, int y, int z, bool[,,] shapePoints)
    {
        int id = IdOf(shapePoints[x, y, z],
            shapePoints[x, y, z + 1],
            shapePoints[x, y + 1, z],
            shapePoints[x, y + 1, z + 1],
            shapePoints[x + 1, y, z],
            shapePoints[x + 1, y, z + 1],
            shapePoints[x + 1, y + 1, z],
            shapePoints[x + 1, y + 1, z + 1]);

        LookupTable.TryGetValue(id, out var result);

        return result;
    }

    private Vector3 GetFunctionMomentarySpeed(Vector3 realPos, float funcAtPoint, float d = 0.01f)
    {
        // TODO: there's quite many divisions in a few methods here that are still called in loops that execute very
        // many times. It's perhaps faster to convert these to use multiplications as division is the most expensive
        // basic math operation for a CPU to do.
        Vector3 direction = new Vector3(
            (MathFunction.GetValue(new Vector3(realPos.X + d, realPos.Y, realPos.Z)) - funcAtPoint) / d,
            (MathFunction.GetValue(new Vector3(realPos.X, realPos.Y + d, realPos.Z)) - funcAtPoint) / d,
            (MathFunction.GetValue(new Vector3(realPos.X, realPos.Y, realPos.Z + d)) - funcAtPoint) / d);

        return direction;
    }

    /// <summary>
    ///   Adjusts mesh points to be at mesh's surface. Optionally also calculates mesh normals.
    /// </summary>
    private void AdjustVertices(List<Vector3> points, float changeClamp = 0.5f, Vector3[]? meshNormals = null)
    {
        // Vertex adjustment by normals:
        // 1. Find functions instantaneous speed at vertex
        // 2. Find required distance change for vertex to be at the mesh's surface
        // 3. Clamp the change for it not to be too far from the initial position.
        // In other words, no vertex should end up in a place where another vertex theoretically may be.
        // 4. Apply change, using instantaneous speed's vector as direction
        // 5. Make a second pass of 1-4. This algorithm is only an approximation, so sometimes one-pass approach
        // results in spiky meshes. Optionally clamp the value more than on the first pass.

        var tasks = new List<Task>();

        int availableThreads = TaskExecutor.Instance.ParallelTasks;

        int step = points.Count / availableThreads;

        if (step < 1)
            step = 1;

        int count = points.Count;

        for (int i = 0; i < count; i += step + 1)
        {
            int from = i;
            int to = Math.Clamp(i + step, i, count - 1);

            // TODO: try to avoid lambda allocations that capture many variables
            var task = new Task(() => AdjustVerticesInRange(from, to, points, changeClamp, meshNormals));
            tasks.Add(task);
        }

        TaskExecutor.Instance.RunTasks(tasks);
    }

    private void AdjustVerticesInRange(int from, int to, List<Vector3> points, float changeClamp = 0.5f,
        Vector3[]? meshNormals = null)
    {
        float maxToleratedChange = changeClamp / PointsPerUnit;
        float d = 0.25f / PointsPerUnit;

        for (int i = from; i <= to; ++i)
        {
            var point = points[i];

            float functionAtPoint = MathFunction.GetValue(point);

            Vector3 normal = GetFunctionMomentarySpeed(point, functionAtPoint, d);

            // If we move one unit in the direction of the normal, function value should be this much more.
            // (If we assume that the function is completely )
            float instantaneousSpeed = normal.Length();

            Vector3 change = (normal / instantaneousSpeed) * ((MathFunction.SurfaceValue - functionAtPoint)
                / instantaneousSpeed);

            change.X = Math.Clamp(change.X, -maxToleratedChange, maxToleratedChange);
            change.Y = Math.Clamp(change.Y, -maxToleratedChange, maxToleratedChange);
            change.Z = Math.Clamp(change.Z, -maxToleratedChange, maxToleratedChange);

            points[i] = point + change;

            if (meshNormals != null)
            {
                meshNormals[i] = -normal / instantaneousSpeed;
            }
        }
    }

    private void PlaceTriangles(Vector3I gridPos, Vector3I[] trisToPlace, List<Vector3> points, List<int> tris,
        Dictionary<Vector3I, int> placedPoints)
    {
        if (!placedPoints.TryGetValue(gridPos, out var originIndex))
        {
            points.Add(new Vector3(gridPos.X, gridPos.Y, gridPos.Z) / PointsPerUnit);
            originIndex = points.Count - 1;
            placedPoints.Add(gridPos, originIndex);
        }

        foreach (var pointRelativePos in trisToPlace)
        {
            if (pointRelativePos == Vector3I.Zero)
            {
                tris.Add(originIndex);
                continue;
            }

            var point = pointRelativePos + gridPos;

            if (placedPoints.TryGetValue(point, out var index))
            {
                tris.Add(index);
            }
            else
            {
                points.Add(new Vector3(point.X, point.Y, point.Z) / PointsPerUnit);
                index = points.Count - 1;
                placedPoints.Add(point, index);
                tris.Add(index);
            }
        }
    }

    /// <summary>
    ///   Subdivides given data using Catmull-Clark algorithm. Mutates given Lists, overwrites normals.
    /// </summary>
    private void CatmullClarkSubdivision(List<Vector3> points, List<int> triIndices, ref Vector3[] normals)
    {
        var sw = new Stopwatch();
        sw.Start();

        int originalTriIndexCount = triIndices.Count;
        int originalPointCount = points.Count;

        // Key is edge points' indices in points list.
        // Value is triangle index in triIndices list, where the triangle is adjacent to the edge.
        // Ints in tuple should be arranged in increasing order
        var edgeTriangles = new Dictionary<(int StartID, int EndID), EdgeData>();

        // New points are added to the points List multiple times in this algoritm.
        // Ultimately, its point order is as follows:
        // 1. Original points in an unmodified order. Count = originalPointCount
        // 2. Face points in the order of the original triIndices. Count = originalTriIndexCount / 3
        // 3. Edge points in no particular order. Count = edgeTriangles.Count (after mesh edges being added to it).

        int edgesWithFourFaces = 0;

        // Step one: add face centers, calculate faces adjacent to each edge
        for (int i = 0; i < originalTriIndexCount; i += 3)
        {
            int newID = points.Count;
            points.Add((points[triIndices[i]] + points[triIndices[i + 1]] + points[triIndices[i + 2]])
                / 3.0f);

            for (int j = i; j < i + 3; ++j)
            {
                int startID = triIndices[j];
                int endID;

                if (j == i + 2)
                {
                    endID = triIndices[j - 2];
                }
                else
                {
                    endID = triIndices[j + 1];
                }

                // Triangles' indices need to go in a clockwise order to render right face out.
                // Due to this, there needs to be an "objective" direction of each edge, based on which triangles
                // will determine if they are 'left-handed' or 'right-handed', which will then determine vertex order
                // for final triangle placement.
                // In this case, the "objective" direction is determined by vertices' IDs going in an increasing order.
                // If the current triangle's vertices on this edge go in the "objective" direction,
                // it is right-handed, otherwise it's left-handed
                bool isLeftHanded = false;

                // Vertices' IDs should go in an increasing order
                if (startID > endID)
                {
                    (startID, endID) = (endID, startID);
                    isLeftHanded = true;
                }

                if (!edgeTriangles.ContainsKey((startID, endID)))
                {
                    edgeTriangles.Add((startID, endID), new EdgeData(newID, isLeftHanded));
                }
                else
                {
                    edgeTriangles[(startID, endID)] = edgeTriangles[(startID, endID)].AddFace(newID, isLeftHanded,
                        out bool increasedFaceCount);

                    if (increasedFaceCount)
                    {
                        ++edgesWithFourFaces;
                    }
                }
            }
        }

        // Step two: calculate normals for each point
        var newNormals = new Vector3[originalPointCount + originalTriIndexCount / 3 + edgeTriangles.Count
            + edgesWithFourFaces];

        // Copy original points' normals
        for (int i = 0; i < originalPointCount; ++i)
        {
            newNormals[i] = normals[i];
        }

        // Calculate faces' normals
        for (int i = 0; i < originalTriIndexCount; i += 3)
        {
            int faceID = i / 3 + originalPointCount;
            newNormals[faceID] = (normals[triIndices[i]] + normals[triIndices[i + 1]] + normals[triIndices[i + 2]])
                .Normalized();
        }

        // Calculate edges' normals
        int edgeID = originalPointCount + originalTriIndexCount / 3;
        foreach (var edge in edgeTriangles)
        {
            newNormals[edgeID] = (normals[edge.Key.StartID] + normals[edge.Key.EndID]).Normalized();
            ++edgeID;

            // Edges with four faces create two points when subdivided.
            if (edge.Value.HasFourFaces)
            {
                newNormals[edgeID] = (normals[edge.Key.StartID] + normals[edge.Key.EndID]).Normalized();
                ++edgeID;
            }
        }

        normals = newNormals;

        // Step three: calculate edge centers's positions, place triangles
        (float Divisor, Vector3 PointSum)[] originalPointsAdjacencies = new (float, Vector3)[originalPointCount];

        triIndices.Clear();
        foreach (var edge in edgeTriangles)
        {
            var startID = edge.Key.StartID;
            var endID = edge.Key.EndID;

            if (!edgeTriangles.TryGetValue((startID, endID), out var faces))
            {
                GD.PrintErr("Error when subdividing: edge-adjacent faces not found");
                continue;
            }

            if (faces.HasFourFaces)
            {
                SubdivideEdge(startID, endID, faces.Face0ID, faces.Face1ID, points, triIndices,
                    originalPointsAdjacencies);

                SubdivideEdge(startID, endID, faces.Face2ID, faces.Face3ID, points, triIndices,
                    originalPointsAdjacencies);
            }
            else
            {
                SubdivideEdge(startID, endID, faces.Face0ID, faces.Face1ID, points, triIndices,
                    originalPointsAdjacencies);
            }
        }

        // Find original points' barycentric coordinates.
        // This isn't a canonical function of Catmull-Clark subdivision, but it's faster
        for (int i = 0; i < originalPointCount; ++i)
        {
            points[i] = (originalPointsAdjacencies[i].PointSum + points[i] * 6.0f)
                / (originalPointsAdjacencies[i].Divisor + 6.0f);
        }

        sw.Stop();
        GD.Print($"Subdivided a mesh in {sw.Elapsed}");
    }

    private void SubdivideEdge(int startID, int endID, int face1ID, int face2ID, List<Vector3> newPoints,
        List<int> newTriIndices, (float, Vector3)[] originalPointsAdjacencies)
    {
        if (face1ID == -1 || face2ID == -1)
        {
            GD.PrintErr("Error when subdividing: a face has wrong id");
            return;
        }

        Vector3 firstFaceCenter = newPoints[face1ID];
        Vector3 secondFaceCenter = newPoints[face2ID];

        Vector3 edgeCenter = (newPoints[startID] + newPoints[endID] + firstFaceCenter + secondFaceCenter) / 4.0f;

        originalPointsAdjacencies[startID].Item1 += 1.0f;
        originalPointsAdjacencies[startID].Item2 += edgeCenter;

        originalPointsAdjacencies[endID].Item1 += 1.0f;
        originalPointsAdjacencies[endID].Item2 += edgeCenter;

        int newPointID = newPoints.Count;
        newPoints.Add(edgeCenter);

        // Left-handed triangles
        newTriIndices.Add(newPointID);
        newTriIndices.Add(face1ID);
        newTriIndices.Add(endID);

        newTriIndices.Add(newPointID);
        newTriIndices.Add(startID);
        newTriIndices.Add(face1ID);

        // Right-handed triangles
        newTriIndices.Add(newPointID);
        newTriIndices.Add(face2ID);
        newTriIndices.Add(startID);

        newTriIndices.Add(newPointID);
        newTriIndices.Add(endID);
        newTriIndices.Add(face2ID);
    }

    private struct EdgeData
    {
        public int Face0ID;
        public int Face1ID;

        public int Face2ID;
        public int Face3ID;
        public bool HasFourFaces;

        public EdgeData(int faceID, bool isFaceLeftHanded)
        {
            if (isFaceLeftHanded)
            {
                Face0ID = faceID;
                Face1ID = -1;
            }
            else
            {
                Face0ID = -1;
                Face1ID = faceID;
            }

            Face2ID = -1;
            Face3ID = -1;

            HasFourFaces = false;
        }

        public EdgeData AddFace(int faceID, bool isFaceLeftHanded, out bool increasedFaceCount)
        {
            EdgeData edge = this;

            increasedFaceCount = false;

            // Dual Contouring has cases in which one edge might be shared between not 2, but 4 faces.
            if (edge.HasFourFaces)
            {
                if (isFaceLeftHanded)
                {
                    edge.Face2ID = faceID;
                }
                else
                {
                    edge.Face3ID = faceID;
                }
            }
            else
            {
                if (isFaceLeftHanded)
                {
                    if (Face0ID != -1)
                    {
                        edge.HasFourFaces = true;
                        edge.Face2ID = faceID;
                        increasedFaceCount = true;
                    }
                    else
                    {
                        edge.Face0ID = faceID;
                    }
                }
                else
                {
                    if (Face1ID != -1)
                    {
                        edge.HasFourFaces = true;
                        edge.Face3ID = faceID;
                        increasedFaceCount = true;
                    }
                    else
                    {
                        edge.Face1ID = faceID;
                    }
                }
            }

            return edge;
        }
    }

    private struct Cube
    {
        public readonly bool[,,] Points;

        public Cube(int id)
        {
            Points = new bool[2, 2, 2];

            Points[0, 0, 0] = id % 2 == 1;
            Points[0, 0, 1] = ((id % 4) / 2) == 1;
            Points[0, 1, 0] = ((id % 8) / 4) == 1;
            Points[0, 1, 1] = ((id % 16) / 8) == 1;
            Points[1, 0, 0] = ((id % 32) / 16) == 1;
            Points[1, 0, 1] = ((id % 64) / 32) == 1;
            Points[1, 1, 0] = ((id % 128) / 64) == 1;
            Points[1, 1, 1] = ((id % 256) / 128) == 1;
        }
    }
}
