using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
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

    private static readonly Dictionary<int, Vector3I[]> LookupTableInt = new();

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

        // Subdivision can be used multiple times, but the triangle count increases exponentially in this way
        CatmullClarkSubdivision(points, ref triIndices, ref normals);

        var colors = new Color[points.Count];

        SetColours(points, colors);

        var arrays = new Array();
        arrays.Resize((int)Mesh.ArrayType.Max);

        arrays[(int)Mesh.ArrayType.Vertex] = points.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = triIndices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Color] = colors;

        // arrays[(int)Mesh.ArrayType.TexUV] = newUV;
        // arrays[(int)Mesh.ArrayType.TexUV2] = newUV1;

        ArrayMesh mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        sw.Stop();
        GD.Print($"Generated a mesh in {sw.Elapsed}");

        return mesh;
    }

    private static void CalculateLookupTableIfNeeded()
    {
        if (LookupTableInt.Count > 0)
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

            LookupTableInt.Add(i, tris.ToArray());
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

        LookupTableInt.TryGetValue(id, out var result);

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
    ///   Subdivides given data using Catmull-Clark algorithm.
    /// </summary>
    private void CatmullClarkSubdivision(List<Vector3> newPoints, ref List<int> triIndices, ref Vector3[] normals)
    {
        var sw = new Stopwatch();
        sw.Start();

        int triIndexCount = triIndices.Count;
        int originalPointCount = newPoints.Count;

        // newPoints has the following structure:
        // Original points go first, count = points.Count
        // Face centers go next, count = triIndices.Count / 3. Their order is defined by tri order in triIndices
        // Edge centers go last. Order is defined by point order in triIndices.
        // So, if a triangle is defined as (A, B, C), then the edge order is as follows: AB, BC, CA.
        // TODO: once this works, expand points instead of creating a new List
        List<Vector3> newNormals = new List<Vector3>(normals);

        // Key is two edge points' indices in points list.
        // Value is triangle index in triIndices list.
        // Ints in tuple should be arranged in increasing order
        Dictionary<(int StartID, int EndID), int[]> edgeTriangles = new Dictionary<(int, int), int[]>();

        // Step one: add face centers, calculate faces adjacent to each edge
        for (int i = 0; i < triIndexCount; i += 3)
        {
            int newID = newPoints.Count;
            newPoints.Add((newPoints[triIndices[i]] + newPoints[triIndices[i + 1]] + newPoints[triIndices[i + 2]])
                / 3.0f);
            newNormals.Add((normals[triIndices[i]] + normals[triIndices[i + 1]] + normals[triIndices[i + 2]])
                .Normalized());

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

                AssignFaceEdges(startID, endID, isLeftHanded, newID, edgeTriangles);
            }
        }

        // [0;2]
        int triangleNumber = 0;

        List<int> newTriIndices = new List<int>();

        (float Divisor, Vector3 PointSum)[] originalPointsAdjacencies = new (float, Vector3)[originalPointCount];

        // Ints in tuple should be arranged in increasing order
        Dictionary<(int StartID, int EndID), bool> calculatedEdges = new Dictionary<(int, int), bool>();

        // Step two: calculate edge centers, place triangles
        for (int i = 0; i < triIndexCount; ++i)
        {
            int startID = triIndices[i];
            int endID;

            if (triangleNumber == 2)
            {
                endID = triIndices[i - 2];
                triangleNumber = 0;
            }
            else
            {
                endID = triIndices[i + 1];
                ++triangleNumber;
            }

            if (startID > endID)
            {
                (startID, endID) = (endID, startID);
            }

            if (calculatedEdges.ContainsKey((startID, endID)))
                continue;

            edgeTriangles.TryGetValue((startID, endID), out var faces);

            if (faces == null)
            {
                GD.PrintErr("Error when subdividing: edge-adjacent faces not found");
                continue;
            }

            if (faces.Length == 4)
            {
                SubdivideEdge(startID, endID, faces[0], faces[1], newPoints, newTriIndices, newNormals,
                    originalPointsAdjacencies);

                SubdivideEdge(startID, endID, faces[2], faces[3], newPoints, newTriIndices, newNormals,
                    originalPointsAdjacencies);
            }
            else
            {
                SubdivideEdge(startID, endID, faces[0], faces[1], newPoints, newTriIndices, newNormals,
                    originalPointsAdjacencies);
            }

            calculatedEdges.Add((startID, endID), true);
        }

        // Find original points' barycentric coordinates.
        // This isn't a canonical function of Catmull-Clark subdivision, but it's faster
        for (int i = 0; i < originalPointCount; ++i)
        {
            newPoints[i] = (originalPointsAdjacencies[i].PointSum + newPoints[i] * 6.0f)
                / (originalPointsAdjacencies[i].Divisor + 6.0f);
        }

        triIndices = newTriIndices;
        normals = newNormals.ToArray();

        sw.Stop();
        GD.Print($"Subdivided a mesh in {sw.Elapsed}");
    }

    /// <summary>
    ///   Assign a face to an adjacent edge (defined by startID, endID)
    /// </summary>
    private void AssignFaceEdges(int startID, int endID, bool isLeftHanded, int faceID,
        Dictionary<(int StartID, int EndID), int[]> edgeTriangles)
    {
        if (!edgeTriangles.ContainsKey((startID, endID)))
        {
            if (isLeftHanded)
            {
                edgeTriangles.Add((startID, endID), new int[] { faceID, -1 });
            }
            else
            {
                edgeTriangles.Add((startID, endID), new int[] { -1, faceID });
            }
        }
        else
        {
            var tris = edgeTriangles[(startID, endID)];

            // Dual Contouring has cases in which one edge might be shared between not 2, but 4 faces.
            if (tris.Length == 4)
            {
                if (isLeftHanded)
                {
                    tris[2] = faceID;
                }
                else
                {
                    tris[3] = faceID;
                }
            }
            else
            {
                if (isLeftHanded)
                {
                    if (tris[0] != -1)
                    {
                        tris = new int[] { tris[0], tris[1], faceID, -1 };
                        edgeTriangles[(startID, endID)] = tris;
                    }
                    else
                    {
                        tris[0] = faceID;
                    }
                }
                else
                {
                    if (tris[1] != -1)
                    {
                        tris = new int[] { tris[0], tris[1], -1, faceID };
                        edgeTriangles[(startID, endID)] = tris;
                    }
                    else
                    {
                        tris[1] = faceID;
                    }
                }
            }
        }
    }

    private void SubdivideEdge(int startID, int endID, int face1ID, int face2ID, List<Vector3> newPoints,
        List<int> newTriIndices, List<Vector3> newNormals, (float, Vector3)[] originalPointsAdjacencies)
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

        newNormals.Add((newNormals[startID] + newNormals[endID] + newNormals[face1ID] + newNormals[face2ID])
            .Normalized());

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
