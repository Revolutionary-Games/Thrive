using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

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

    private static System.Collections.Generic.Dictionary<int, Vector3I[]>? lookupTableInt;

    public DualContourer(IMeshGeneratingFunction mathFunction)
    {
        CalculateLookupTableIfNeeded();
        MathFunction = mathFunction;
    }

    public bool IsInShape(Vector3 pos)
    {
        if (MathFunction.GetValue(pos) > MathFunction.SurfaceValue)
            return true;

        return false;
    }

    public Mesh DualContour()
    {
        var sw = new Stopwatch();
        sw.Start();

        var placedPoints =
            new System.Collections.Generic.Dictionary<Vector3I, int>(); // int is point's index in points[]

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

                    PlaceTriangles(gridPos,
                        tris,
                        points, triIndices, placedPoints);
                }
            }
        }

        var normals = new Vector3[points.Count];

        if (Smoothen)
        {
            AdjustVertices(points);
            AdjustVertices(points, 0.25f, normals);
        }

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
        if (lookupTableInt != null)
            return;

        lookupTableInt = new System.Collections.Generic.Dictionary<int, Vector3I[]>();

        // Doesn't add triangles that go to negative x, y, or z to prevent triangles overlapping

        bool reverseTriangleFaces = false;

        for (int i = 1; i <= 254; ++i)
        {
            Cube cube = new Cube(i); // Make cube from index

            // [0;0;0] point is the origin (center of the cube)
            // All other points point to neighbooring cubes.
            // All of the coordinates are one of the following: -1, 0, or 1
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

            lookupTableInt.Add(i, tris.ToArray());
        }
    }

    private void CalculatePoints(bool[,,] shapePoints, Vector3I gridFrom, Vector3I gridTo)
    {
        var sw = new Stopwatch();
        sw.Start();

        Vector3I gridOffset = -gridFrom;

        Parallel.For(gridFrom.X, gridTo.X + 1, delegate(int x)
        {
            Parallel.For(gridFrom.Y, gridTo.Y + 1, delegate(int y)
            {
                Parallel.For(gridFrom.Z, gridTo.Z + 1, delegate(int z)
                {
                    // var gridPos = new Vector3I(x, y, z);
                    var realPos = new Vector3(x - 0.5f, y - 0.5f, z - 0.5f) / PointsPerUnit;

                    shapePoints[x + gridOffset.X, y + gridOffset.Y, z + gridOffset.Z] = IsInShape(realPos);
                });
            });
        });

        sw.Stop();
    }

    private void SetColours(List<Vector3> points, Color[] colours)
    {
        for (int i = 0; i < colours.Length; i++)
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

        lookupTableInt!.TryGetValue(id, out var result);

        return result;
    }

    private Vector3 GetFunctionMomentarySpeed(Vector3 realPos, float funcAtPoint, float d = 0.01f)
    {
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
        // 1. Find functions instanteous speed at vertex
        // 2. Find required distance change for vertex to be at the mesh's surface
        // 3. Clamp the change for it not to be too far from the initial position.
        // In other words, no vertex should end up in a place where another vertex theoretically may be.
        // 4. Apply change, using instanteous speed's vector as direction
        // 5. Make a second pass of 1-4. This algorithm is only an approximation, so sometimes one-pass approach
        // results in spiky meshes. Optionally clamp the value more than on the first pass.

        float maxToleratedChange = changeClamp / PointsPerUnit;

        float d = 0.25f / PointsPerUnit;

        int count = points.Count;
        Parallel.For(0, count, delegate(int i)
        {
            float functionAtPoint = MathFunction.GetValue(points[i]);

            Vector3 normal = GetFunctionMomentarySpeed(points[i], functionAtPoint, d);

            // If we move one unit in the direction of the normal, function value should be this much more.
            // (If we assume that the function is completely )
            float instanteousSpeed = normal.Length();

            Vector3 change = (normal / instanteousSpeed) * ((MathFunction.SurfaceValue - functionAtPoint)
                / instanteousSpeed);

            change.X = Mathf.Clamp(change.X, -maxToleratedChange, maxToleratedChange);
            change.Y = Mathf.Clamp(change.Y, -maxToleratedChange, maxToleratedChange);
            change.Z = Mathf.Clamp(change.Z, -maxToleratedChange, maxToleratedChange);

            points[i] += change;

            if (meshNormals != null)
            {
                meshNormals[i] = -normal / instanteousSpeed;
            }
        });
    }

    private void PlaceTriangles(Vector3I gridPos, Vector3I[] trisToPlace, List<Vector3> points, List<int> tris,
        System.Collections.Generic.Dictionary<Vector3I, int> placedPoints)
    {
        if (!placedPoints.TryGetValue(gridPos, out var originIndex))
        {
            points.Add(new Vector3(gridPos.X, gridPos.Y, gridPos.Z) / PointsPerUnit);
            originIndex = points.Count - 1;
            placedPoints.Add(gridPos, originIndex);
        }

        for (int i = 0; i < trisToPlace.Length; i++)
        {
            var pointRelativePos = trisToPlace[i];

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

    private struct Cube
    {
        public bool[,,] Points;

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
