using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   This implements the membrane algorithm from going from 2D hex locations to a membrane mesh that can be used for
///   rendering or calculations that use the membrane point
/// </summary>
public class MembraneShapeGenerator
{
    private static readonly ThreadLocal<MembraneShapeGenerator> ThreadLocalGenerator =
        new(() => new MembraneShapeGenerator());

    /// <summary>
    ///   Number of segments on one side of the above described square. The number of points on the side of
    ///   the membrane.
    /// </summary>
    private static readonly int MembraneResolution = Constants.MEMBRANE_RESOLUTION;

    /// <summary>
    ///   Half the number of points on the membrane prism's side.
    ///   The total amount is equal to verticalResolution * 2 + 1.
    /// </summary>
    private static readonly int MembraneVerticalResolution = Constants.MEMBRANE_VERTICAL_RESOLUTION;

    /// <summary>
    ///   Stores the generated 2-Dimensional membrane. Needed as an easily resizable target work area. Data is copied
    ///   from here to <see cref="MembranePointData"/> for actual usage (and checks like containing points)
    /// </summary>
    private readonly List<Vector2> vertices2D = new();

    /// <summary>
    ///   Stores the original unmodified vertices during multicellular processing
    /// </summary>
    private readonly List<Vector2> originalVertices2D = new();

    /// <summary>
    ///   Stores the neighbours' original vertices that have been rotated and shifted to current cells reference point
    ///   during multicellular processing
    /// </summary>
    private readonly Dictionary<long, List<Vector2>> originalNeighboursVertices = new();

    /// <summary>
    ///   Stores the neighbours' stretched vertices that have been rotated and shifted to current cells reference point
    ///   during multicellular processing
    /// </summary>
    private readonly Dictionary<long, List<Vector2>> editableNeighboursVertices = new();

    /// <summary>
    ///   Stores the neighbours' vertices shifts during multicellular processing for 'unshifting' purposes
    /// </summary>
    private readonly Dictionary<long, Vector2> neighbourShifts = new();

    /// <summary>
    ///   Stores the neighbours' vertices rotation angles during multicellular processing for 'unrotating' purposes
    /// </summary>
    private readonly Dictionary<long, float> neighbourRotations = new();

    /// <summary>
    ///   Stores key of the neighbours that are in the vicinity of the current cell
    /// </summary>
    private readonly List<long> closeNeighboursKeys = new();

    /// <summary>
    ///   Stores positions of current cells vertices that have been cast onto the tangent lines during
    ///   multicellular processing in case there are overlaps with other neighbours and the transformations cannot
    ///   be applied
    /// </summary>
    private readonly Dictionary<int, Vector2> validPointsCasts = new();

    /// <summary>
    ///   Stores positions of neighbour cells vertices that have been cast onto the tangent lines during
    ///   multicellular processing in case there are overlaps with other neighbours and the transformations cannot
    ///   be applied
    /// </summary>
    private readonly Dictionary<int, Vector2> validNeighbourPointsCasts = new();

    /// <summary>
    ///   Buffer for starting points when generating membrane data
    /// </summary>
    private readonly List<Vector2> startingBuffer = new();

    /// <summary>
    ///   Key of the current cell that is being processed during multicellular membrane stretching
    /// </summary>
    private long currentCellKey;

    /// <summary>
    ///   Key of the neighbour cell that is being processed during multicellular membrane stretching
    /// </summary>
    private long neighbourCellKey;

    /// <summary>
    ///   Gets a generator for the current thread. This is required to be used as the generators are not thread safe.
    /// </summary>
    /// <returns>A generator that can be used by the calling thread</returns>
    public static MembraneShapeGenerator GetThreadSpecificGenerator()
    {
        return ThreadLocalGenerator.Value!;
    }

    public MembranePointData GenerateMicrobeShape(ref MembraneGenerationParameters parameters,
        bool isMulticellular = false)
    {
        return GenerateMicrobeShape(parameters.HexPositions, parameters.HexPositionCount,
            parameters.Type, isMulticellular);
    }

    /// <summary>
    ///   Generates the 2D points for a membrane given the parameters that affect the shape
    /// </summary>
    /// <returns>
    ///   Computed data in a cache entry format (to be used with <see cref="ProceduralDataCache"/>, which should be
    ///   checked for existing data before computing new data)
    /// </returns>
    public MembranePointData GenerateMicrobeShape(Vector2[] hexPositions, int hexCount, MembraneType membraneType,
        bool isMulticellular = false)
    {
        // The length in pixels (probably not accurate?) of a side of the square that bounds the membrane.
        // Half the side length of the original square that is compressed to make the membrane.
        int cellDimensions = 10;

        for (int i = 0; i < hexCount; ++i)
        {
            var pos = hexPositions[i];
            if (MathF.Abs(pos.X) + 1 > cellDimensions)
            {
                cellDimensions = (int)MathF.Abs(pos.X) + 1;
            }

            if (MathF.Abs(pos.Y) + 1 > cellDimensions)
            {
                cellDimensions = (int)MathF.Abs(pos.Y) + 1;
            }
        }

        // Make the length longer to guarantee that everything fits easily inside the square
        cellDimensions *= 100;

        startingBuffer.Clear();

        // Integer divides are intentional here
        // ReSharper disable PossibleLossOfFraction

        for (int i = MembraneResolution; i > 0; i--)
        {
            startingBuffer.Add(new Vector2(-cellDimensions,
                cellDimensions - 2 * cellDimensions / MembraneResolution * i));
        }

        for (int i = MembraneResolution; i > 0; i--)
        {
            startingBuffer.Add(new Vector2(cellDimensions - 2 * cellDimensions / MembraneResolution * i,
                cellDimensions));
        }

        for (int i = MembraneResolution; i > 0; i--)
        {
            startingBuffer.Add(new Vector2(cellDimensions,
                -cellDimensions + 2 * cellDimensions / MembraneResolution * i));
        }

        for (int i = MembraneResolution; i > 0; i--)
        {
            startingBuffer.Add(new Vector2(-cellDimensions + 2 * cellDimensions / MembraneResolution * i,
                -cellDimensions));
        }

        // ReSharper restore PossibleLossOfFraction
        GenerateMembranePoints(hexPositions, hexCount, membraneType, isMulticellular);

        // This makes a copy of the vertices so the data is safe to modify in further calls to this method
        return new MembranePointData(hexPositions, hexCount, membraneType, vertices2D);
    }

    /// <summary>
    ///   Modifies already generated membrane to make it stretch towards other cells. Also applies membrane
    ///   waviness as it is normally done in single cellular membrane
    /// </summary>
    public MembranePointData GenerateMulticellularMembrane(long thisCellKey,
        ConcurrentDictionary<long, NeighbourData> neighboursData,
        Vector2[] multicellularPositions, int[]? multicellularOrientations)
    {
        currentCellKey = thisCellKey;
        var currentCellData = neighboursData[currentCellKey];
        var originalPointData = currentCellData.OriginalPointData;
        var thisCellPosition = currentCellData.CellPosition;
        var thisCellOrientation = currentCellData.Orientation;

        SetCellVertices(currentCellData);
        GenerateMulticellularMembrane(neighboursData);

        // Apply the same waviness pass used for single-cell membranes
        float circumference = 0.0f;
        for (int i = 0, end = vertices2D.Count; i < end; ++i)
            circumference += (vertices2D[(i + 1) % end] - vertices2D[i]).Length();

        MakeMembraneWavier(originalPointData.Type, circumference);

        int hexCount = originalPointData.HexPositionCount;
        var hexCopy = ArrayPool<Vector2>.Shared.Rent(hexCount);
        originalPointData.HexPositions.AsSpan(0, hexCount).CopyTo(hexCopy);

        return new MembranePointData(hexCopy, hexCount, originalPointData.Type, vertices2D, multicellularPositions,
            thisCellPosition, multicellularOrientations, thisCellOrientation);
    }

    /// <summary>
    ///   Creates the visual mesh from the overall shape (see GenerateShape method above) that is already created
    /// </summary>
    public (ArrayMesh Mesh, int SurfaceIndex) GenerateMesh(MembranePointData shapeData)
    {
        // TODO: should the 3D membrane generation already happen when GenerateMembranePoints is called?
        // That would reduce the load on the main thread when generating the final visual mesh, though the membrane
        // properties are also used in non-graphical context (species speed) so that'd result in quite a bit of
        // unnecessary computations
        var mesh = BuildMesh(shapeData.Vertices2D, shapeData.VertexCount, shapeData.Height, out var surfaceIndex);

        return (mesh, surfaceIndex);
    }

    /// <summary>
    ///   Creates the visual engulf mesh from the overall shape (see GenerateShape method above) that is already created
    /// </summary>
    public (ArrayMesh Mesh, int SurfaceIndex) GenerateEngulfMesh(MembranePointData shapeData)
    {
        var mesh = BuildEngulfMesh(shapeData.Vertices2D, shapeData.VertexCount, out var surfaceIndex);

        return (mesh, surfaceIndex);
    }

    /// <summary>
    ///   Takes a mesh with placed vertices and places UVs and normals on it
    /// </summary>
    private static void FinishMesh(int vertexCount, int layerCount, Vector3[] vertices, Vector2[] uvs,
        Vector3[] normals)
    {
        float uvAngleModifier = 2.0f * MathF.PI / vertexCount;

        for (int layer = 0; layer < layerCount; ++layer)
        {
            for (int i = 0; i < vertexCount; ++i)
            {
                int id = i + layer * vertexCount;

                float y = 0.8f * MathF.Abs(layer - MembraneVerticalResolution) / MembraneVerticalResolution;
                (float sin, float cos) = MathF.SinCos((uvAngleModifier * i) - 2.3f);

                Vector2 direction = (1.0f - y) * new Vector2(sin, cos) * 0.49f + new Vector2(0.5f, 0.5f);

                uvs[id] = direction;

                // Find normals
                Vector3 previous = i == 0 ? vertices[id + vertexCount - 1] : vertices[id - 1];
                Vector3 next = i == vertexCount - 1 ? vertices[id - i] : vertices[id + 1];

                Vector3 down = layer == 0 ? vertices[vertices.Length - 2] : vertices[id - vertexCount];
                Vector3 up = layer == layerCount - 1 ? vertices[vertices.Length - 1] : vertices[id + vertexCount];

                normals[id] = (next - previous).Cross(up - down).Normalized();
            }
        }

        uvs[uvs.Length - 2] = new Vector2(0.5f, 0.5f);
        uvs[uvs.Length - 1] = new Vector2(0.5f, 0.5f);

        normals[normals.Length - 2] = new Vector3(0.0f, -1.0f, 0.0f);
        normals[normals.Length - 1] = new Vector3(0.0f, 1.0f, 0.0f);
    }

    /// <summary>
    ///   Return the position of the closest organelle hex to the target point.
    /// </summary>
    private static Vector2 FindClosestOrganelleHex(Vector2[] hexPositions, int hexCount, Vector2 target)
    {
        float closestDistanceSoFar = float.MaxValue;
        Vector2 closest = new Vector2(0.0f, 0.0f);

        for (int i = 0; i < hexCount; ++i)
        {
            var pos = hexPositions[i];
            float lenToObject = (target - pos).LengthSquared();

            if (lenToObject < closestDistanceSoFar)
            {
                closestDistanceSoFar = lenToObject;
                closest = pos;
            }
        }

        return closest;
    }

    private static void PlaceTriangles(int vertexCount, int layerCount, int[] indices)
    {
        int writeIndex = 0;

        // To form side faces,
        // each point in the points[,] list forms a face with:
        // - the next point on the same layer
        // - same point on the layer higher
        // - next point on the layer higher
        // If the point is on the highest layer (aka the top layer), it doesn't connect with anything
        for (int layer = 0; layer < layerCount - 1; ++layer)
        {
            for (int i = 0; i < vertexCount - 1; ++i)
            {
                indices[writeIndex] = i + layer * vertexCount;
                indices[writeIndex + 1] = i + 1 + (layer + 1) * vertexCount;
                indices[writeIndex + 2] = i + 1 + layer * vertexCount;

                indices[writeIndex + 3] = i + layer * vertexCount;
                indices[writeIndex + 4] = i + (layer + 1) * vertexCount;
                indices[writeIndex + 5] = i + 1 + (layer + 1) * vertexCount;

                writeIndex += 6;
            }
        }

        // Final side face
        for (int layer = 0; layer < layerCount - 1; ++layer)
        {
            indices[writeIndex] = (vertexCount - 1) + layer * vertexCount;
            indices[writeIndex + 1] = (layer + 1) * vertexCount;
            indices[writeIndex + 2] = layer * vertexCount;

            indices[writeIndex + 3] = (vertexCount - 1) + layer * vertexCount;
            indices[writeIndex + 4] = (vertexCount - 1) + (layer + 1) * vertexCount;
            indices[writeIndex + 5] = (layer + 1) * vertexCount;
            writeIndex += 6;
        }

        int bottomPeakID = layerCount * vertexCount;
        int topPeakID = layerCount * vertexCount + 1;
        int topLayerIdStart = (layerCount - 1) * vertexCount;

        // Top face triangles
        for (int i = 0; i < vertexCount; ++i)
        {
            if (i == 0)
            {
                indices[writeIndex] = topPeakID;
                indices[writeIndex + 1] = topLayerIdStart;
                indices[writeIndex + 2] = topLayerIdStart + vertexCount - 1;
            }
            else
            {
                indices[writeIndex] = topPeakID;
                indices[writeIndex + 1] = topLayerIdStart + i;
                indices[writeIndex + 2] = topLayerIdStart + i - 1;
            }

            writeIndex += 3;
        }

        // Bottom face triangles. Same as top, but with reversed index order and different layer
        for (int i = 0; i < vertexCount; ++i)
        {
            if (i == 0)
            {
                indices[writeIndex] = bottomPeakID;
                indices[writeIndex + 1] = vertexCount - 1;
                indices[writeIndex + 2] = 0;
            }
            else
            {
                indices[writeIndex] = bottomPeakID;
                indices[writeIndex + 1] = i - 1;
                indices[writeIndex + 2] = i;
            }

            writeIndex += 3;
        }
    }

    private static Vector2 RotatePoint(Vector2 point, float angle)
    {
        (float sin, float cos) = MathF.SinCos(angle);
        return new Vector2(cos * point.X - sin * point.Y, sin * point.X + cos * point.Y);
    }

    /// <summary>
    ///   Creates the actual mesh object.
    /// </summary>
    private static ArrayMesh BuildMesh(Vector2[] vertices2D, int vertexCount, float height, out int surfaceIndex)
    {
        // Average of all outline points
        Vector3 center = Vector3.Zero;

        for (int i = 0; i < vertexCount; ++i)
        {
            center += new Vector3(vertices2D[i].X, 0.0f, vertices2D[i].Y);
        }

        center /= vertexCount;

        int layerCount = MembraneVerticalResolution * 2 + 1;

        // The index list is actually a triangle list (each three consequtive indexes building a triangle)
        // with the size of indexSize
        var bufferSize = layerCount * vertexCount + 2;
        var indexSize = (vertexCount * 2 + (layerCount - 1) * vertexCount * 2) * 3;

        var arrays = new Array();
        arrays.Resize((int)Mesh.ArrayType.Max);

        // Build vertex, index, and uv lists

        // Write mesh data //
        // for a point in points[,] array, on the layer of l and with original id of i,
        // id is equal to i + l * count, where count is the amount of points on one layer
        // (= amount of the outline points).
        // Last two vertices are reserved for the topmost and the bottommost vertices of the mesh, respectively
        var vertices = new Vector3[bufferSize];
        var uvs = new Vector2[bufferSize];
        var normals = new Vector3[bufferSize];

        const float sideRounding = Constants.MEMBRANE_SIDE_ROUNDING;
        const float smoothingPower = Constants.MEMBRANE_SMOOTHING_POWER;

        float roundingMaximum = MathF.Pow(1.0f + sideRounding * 1.05f, smoothingPower);
        float roundingMinimum = 1.0f;

        // Place prism points, already with squishification
        for (int layer = 0; layer < layerCount; ++layer)
        {
            float widthModifier = 1.0f - (MathF.Pow(sideRounding * MathF.Abs(layer - MembraneVerticalResolution)
                / MembraneVerticalResolution + 1.0f, smoothingPower) - roundingMinimum) / roundingMaximum;

            float vertical = height * (layer - MembraneVerticalResolution) / MembraneVerticalResolution;

            // Iterate through outline points
            for (int i = 0; i < vertexCount; ++i)
            {
                Vector3 point = new Vector3(vertices2D[i].X, 0.0f, vertices2D[i].Y);

                Vector3 offsetFromCenter = point - center;
                offsetFromCenter *= widthModifier;

                point = new Vector3(center.X + offsetFromCenter.X, vertical, center.Z + offsetFromCenter.Z);

                vertices[i + layer * vertexCount] = point;
            }
        }

        // Top vertex
        vertices[vertices.Length - 1] = new Vector3(center.X,
            vertices[(layerCount - 1) * vertexCount].Y, center.Z);

        // Bottom vertex
        vertices[vertices.Length - 2] = new Vector3(center.X, vertices[0].Y, center.Z);

        // Index mapping to build all triangles
        var indices = new int[indexSize];

        PlaceTriangles(vertexCount, layerCount, indices);

        FinishMesh(vertexCount, layerCount, vertices, uvs, normals);

        // Godot might do this automatically
        // // Set the bounds to get frustum culling and LOD to work correctly.
        // // TODO: make this more accurate by calculating the actual extents
        // m_mesh->_setBounds(Ogre::Aabb(Float3::ZERO, Float3::UNIT_SCALE * 50)
        //     /*, false*/);
        // m_mesh->_setBoundingSphereRadius(50);

        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;

        // Create the mesh
        var generatedMesh = new ArrayMesh();

        surfaceIndex = generatedMesh.GetSurfaceCount();
        generatedMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        return generatedMesh;
    }

    /// <summary>
    ///   Creates the engulf mesh object.
    /// </summary>
    private static ArrayMesh BuildEngulfMesh(Vector2[] vertices2D, int vertexCount, out int surfaceIndex)
    {
        // common variables
        const float height = 0.1f;

        var center = new Vector2(0.5f, 0.5f);

        // Engulf Mesh is a triangle strip extruded from the shape
        var trueVertexCount = vertexCount * 2;

        // Need two exta indices to connect back to the orginal triangle
        var indexSize = trueVertexCount + 2;

        var arrays = new Array();
        arrays.Resize((int)Mesh.ArrayType.Max);

        // Write mesh data
        var indices = new int[indexSize];
        var vertices = new Vector3[trueVertexCount];
        var uvs = new Vector2[trueVertexCount];

        for (int i = 0; i < vertexCount; ++i)
        {
            var index = i * 2;

            // This weird indexing is required to make the mesh respect winding order
            // Otherwise it will get culled
            var sourceVertex = vertices2D[vertexCount - i - 1];
            var extrudeDir = sourceVertex - center;

            Vector2 extrudedVertex = sourceVertex + extrudeDir *
                Constants.MEMBRANE_ENGULF_ANIMATION_DISTANCE;

            indices[index] = index;
            indices[index + 1] = index + 1;

            vertices[index] = new Vector3(sourceVertex.X, height / 2, sourceVertex.Y);
            vertices[index + 1] = new Vector3(extrudedVertex.X, height / 2, extrudedVertex.Y);

            // UVs are actually used like a distance calculation here instead of actual uvs
            uvs[index] = new Vector2(0, 0);
            uvs[index + 1] = new Vector2(0, 1);
        }

        // Connect back to the start

        indices[trueVertexCount] = 0;
        indices[trueVertexCount + 1] = 1;

        // Godot might do this automatically
        // // Set the bounds to get frustum culling and LOD to work correctly.
        // // TODO: make this more accurate by calculating the actual extents
        // m_mesh->_setBounds(Ogre::Aabb(Float3::ZERO, Float3::UNIT_SCALE * 50)
        //     /*, false*/);
        // m_mesh->_setBoundingSphereRadius(50);

        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;

        // Create the mesh
        var generatedMesh = new ArrayMesh();

        surfaceIndex = generatedMesh.GetSurfaceCount();
        generatedMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.TriangleStrip, arrays);

        return generatedMesh;
    }

    // 2D cross product of vectors (b-a) and (p-a).
    // Positive = p is left of edge a→b, negative = right, zero = collinear.
    private static float Cross(Vector2 a, Vector2 b, Vector2 p)
    {
        return (b - a).Cross(p - a);
    }

    private static bool IsInsideConvexPolygon(List<Vector2> vertices, Vector2 point)
    {
        int n = vertices.Count;
        if (n < 3)
            return false;

        // Determine winding from the first edge so we know which sign = "inside"
        float firstCross = Cross(vertices[0], vertices[1], point);

        for (int i = 1; i < n; ++i)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % n];
            float cross = Cross(a, b, point);

            // If the sign flips, the point is outside (or on) this edge
            if (firstCross * cross < 0)
                return false;
        }

        return true;
    }

    private static float GetOrientationAngle(int orientation)
    {
        return orientation * MathF.PI * 0.3333333f;
    }

    private static Vector2 GetAverageVertex(List<Vector2> vertices)
    {
        var averageVertex = Vector2.Zero;
        foreach (var vertex in vertices)
            averageVertex += vertex;
        averageVertex /= vertices.Count;

        return averageVertex;
    }

    private static float Angle(Vector2 a, Vector2 b, Vector2 c)
    {
        var ba = a - b;
        var bc = c - b;

        var angle = ba.AngleTo(bc) * MathUtils.DEGREES_TO_RADIANS;
        if (angle > 180)
        {
            angle -= 360;
        }

        return angle;
    }

    /// <summary>
    ///   Finds the vertex closest to the half-line from <paramref name="origin"/> toward <paramref name="target"/>,
    ///   and returns its projection distance along that direction.
    /// </summary>
    private static float GetMembraneReachToward(List<Vector2> vertices, Vector2 origin, Vector2 target,
        out int closestVertexIndex)
    {
        var direction = (target - origin).Normalized();
        float minDistanceToRay = float.MaxValue;
        float bestProjection = 0.0f;
        closestVertexIndex = 0;

        for (int i = 0; i < vertices.Count; ++i)
        {
            var vertex = vertices[i];
            var directionToVertex = vertex - origin;
            float projection = directionToVertex.Dot(direction);

            // Only consider vertices on the forward half
            if (projection <= 0.0f)
                continue;

            // Perpendicular distance from vertex to the ray
            float distanceToRay = (directionToVertex - direction * projection).Length();

            if (distanceToRay < minDistanceToRay)
            {
                minDistanceToRay = distanceToRay;
                bestProjection = projection;
                closestVertexIndex = i;
            }
        }

        return bestProjection;
    }

    /// <summary>
    ///   Finds the intersection ray (half-line) and line. Returns false when the lines are parallel or
    ///   the intersection lies behind rayOrigin relative to rayDirection.
    /// </summary>
    private static bool CalculateRayLineIntersection(Vector2 rayOrigin, Vector2 rayDirection, Vector2 linePoint,
        Vector2 lineDirection, out float rayDistance)
    {
        var determinant = rayDirection.X * lineDirection.Y - rayDirection.Y * lineDirection.X;
        rayDistance = 0;

        // Parallel lines
        if (MathF.Abs(determinant) < MathUtils.EPSILON)
        {
            return false;
        }

        var offset = linePoint - rayOrigin;
        rayDistance = (offset.X * lineDirection.Y - offset.Y * lineDirection.X) / determinant;

        // Interdection behind rayOrigin
        if (rayDistance < 0)
            return false;

        return true;
    }

    /// <summary>
    ///   Uses neigbours positions to smooth membrane points. Used to get rid of sharp corner in the membrane
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

    /// <summary>
    ///   Define from which index of verices2D to which index the points should be moved
    /// </summary>
    /// <param name="vertices"> The list of vertices </param>
    /// <param name="tangentPointIndexA"> First tangent line's point on the membrane </param>
    /// <param name="tangentPointIndexB"> Second tangent line's point on the membrane </param>
    /// <param name="reachVertexIndex"> A point on the membrane between the tangent points </param>
    /// <param name="indexStart"> The starting index for the iteration </param>
    /// <param name="indexEnd"> The ending index for the iteration </param>
    private static void CalculateIterationIndices(List<Vector2> vertices, int tangentPointIndexA,
        int tangentPointIndexB, int reachVertexIndex,
        out int indexStart, out int indexEnd)
    {
        var vertexCount = vertices.Count;

        int forwardLength = (tangentPointIndexB - tangentPointIndexA + vertexCount) % vertexCount;
        int backwardLength = (tangentPointIndexA - tangentPointIndexB + vertexCount) % vertexCount;

        int reachOffsetFromA = (reachVertexIndex - tangentPointIndexA + vertexCount) % vertexCount;

        if (reachOffsetFromA <= forwardLength)
        {
            indexStart = tangentPointIndexA;
            indexEnd = tangentPointIndexA + forwardLength;
            return;
        }

        indexStart = tangentPointIndexB;
        indexEnd = tangentPointIndexB + backwardLength;
    }

    private void GenerateMembranePoints(Vector2[] hexPositions, int hexCount, MembraneType membraneType,
        bool isMulticellular)
    {
        // Move all the points in the source buffer close to organelles
        // This operation used to be iterative but this is now a much faster version that moves things all the way in
        // a single step
        for (int i = 0, end = startingBuffer.Count; i < end; ++i)
        {
            var closestOrganelle = FindClosestOrganelleHex(hexPositions, hexCount, startingBuffer[i]);

            var direction = (startingBuffer[i] - closestOrganelle).Normalized();
            var movement = direction * Constants.MEMBRANE_ROOM_FOR_ORGANELLES;

            startingBuffer[i] = closestOrganelle + movement;
        }

        float circumference = 0.0f;

        for (int i = 0, end = startingBuffer.Count; i < end; ++i)
        {
            circumference += (startingBuffer[(i + 1) % end] - startingBuffer[i]).Length();
        }

        vertices2D.Clear();

        var lastAddedPoint = startingBuffer[0];

        vertices2D.Add(lastAddedPoint);

        float gap = circumference / startingBuffer.Count;
        float distanceToLastAddedPoint = 0.0f;
        float distanceToLastPassedPoint = 0.0f;

        // Go around the membrane and place points evenly in the target buffer.
        for (int i = 0, end = startingBuffer.Count; i < end; ++i)
        {
            var currentPoint = startingBuffer[i];
            var nextPoint = startingBuffer[(i + 1) % end];
            float distance = (nextPoint - currentPoint).Length();

            // Add a new point if the next point is too far
            if (distance + distanceToLastAddedPoint - distanceToLastPassedPoint > gap)
            {
                var direction = (nextPoint - currentPoint).Normalized();
                var movement = direction * (gap - distanceToLastAddedPoint + distanceToLastPassedPoint);

                lastAddedPoint = currentPoint + movement;

                vertices2D.Add(lastAddedPoint);

                if (vertices2D.Count >= end)
                    break;

                distanceToLastPassedPoint = (lastAddedPoint - currentPoint).Length();
                distanceToLastAddedPoint = 0.0f;
                --i;
            }
            else
            {
                distanceToLastAddedPoint += distance - distanceToLastPassedPoint;
                distanceToLastPassedPoint = 0.0f;
            }
        }

        if (isMulticellular)
            return;

        MakeMembraneWavier(membraneType, circumference);
    }

    private void MakeMembraneWavier(MembraneType membraneType, float circumference)
    {
        float waveFrequency = 2.0f * MathF.PI * Constants.MEMBRANE_NUMBER_OF_WAVES / vertices2D.Count;

        float heightMultiplier = membraneType.CellWall ?
            Constants.MEMBRANE_WAVE_HEIGHT_MULTIPLIER_CELL_WALL :
            Constants.MEMBRANE_WAVE_HEIGHT_MULTIPLIER;

        float waveHeight = MathF.Pow(circumference, Constants.MEMBRANE_WAVE_HEIGHT_DEPENDENCE_ON_SIZE)
            * heightMultiplier;

        // Make the membrane wavier
        for (int i = 0, end = vertices2D.Count; i < end; ++i)
        {
            var point = vertices2D[i];
            var nextPoint = vertices2D[(i + 1) % end];
            var direction = (nextPoint - point).Normalized();

            // Turn 90 degrees
            direction = new Vector2(-direction.Y, direction.X);

            var movement = direction * MathF.Sin(waveFrequency * i) * waveHeight;

            vertices2D[i] = point + movement;
        }
    }

    /// <summary>
    ///   Modifies single cell membrane to make it stretch towards its neighbours. First, a point between two cells is
    ///   chosen, then 2 tangent lines are created for each cell. After that, vertices between those lines are cast
    ///   onto them and afterwards smoothed out. If there are any overlaps with neighbours, the operation is canceled.
    /// </summary>
    private void GenerateMulticellularMembrane(ConcurrentDictionary<long, NeighbourData> neighboursData)
    {
        var cellData = neighboursData[currentCellKey];
        var cellPosition = cellData.CellPosition;
        var cellOrientation = cellData.Orientation;
        var cellAngle = GetOrientationAngle(cellOrientation);
        var cellAverageVertex = GetAverageVertex(originalVertices2D);

        GetCloseNeighbours(neighboursData, cellPosition);
        RotateAndShiftCloseNeighbours(neighboursData, cellAngle, cellPosition);

        foreach (var neighbourKey in closeNeighboursKeys)
        {
            var neighbourData = neighboursData[neighbourKey];

            if (cellData.ProcessedNeighbours.Contains(neighbourKey))
            {
                continue;
            }

            neighbourData.ProcessedNeighbours.Add(currentCellKey);
            neighbourCellKey = neighbourKey;

            var editableNeighbourVertices = editableNeighboursVertices[neighbourKey];
            var originalNeighbourVertices = originalNeighboursVertices[neighbourKey];

            var neighbourAverageVertex = GetAverageVertex(originalNeighbourVertices);

            var middlePoint = GetMiddlePoint(editableNeighbourVertices, cellAverageVertex, neighbourAverageVertex,
                out var closestCellVertexIndex, out var closestNeighbourVertexIndex);

            if (vertices2D[closestCellVertexIndex]
                    .DistanceSquaredTo(editableNeighbourVertices[closestNeighbourVertexIndex]) >
                Constants.MEMBRANE_NEIGHBOUR_MAX_DISTANCE_BETWEEN_VERTICES)
            {
                continue;
            }

            // This might happen if cells are close to each other
            var isMiddlePointInsideMembrane = IsInsideConvexPolygon(vertices2D, middlePoint) ||
                IsInsideConvexPolygon(editableNeighbourVertices, middlePoint);

            if (isMiddlePointInsideMembrane)
                continue;

            var pointsWereCastOntoTangentLines = CastVerticesOntoTheTangentLines(editableNeighbourVertices, middlePoint,
                cellAverageVertex, neighbourAverageVertex, closestCellVertexIndex, closestNeighbourVertexIndex);

            if (!pointsWereCastOntoTangentLines)
            {
                continue;
            }

            neighbourData.ModifiedVertices ??= new List<Vector2>(editableNeighbourVertices.Count);
            neighbourData.ModifiedVertices.Clear();

            // Revert the preprocessing transform (subtract local offset, counter-rotate) and store.
            var storedLocalOffset = neighbourShifts[neighbourKey];
            var storedRelativeAngle = neighbourRotations[neighbourKey];

            for (int i = 0; i < editableNeighbourVertices.Count; ++i)
            {
                var vertex = editableNeighbourVertices[i];
                vertex -= storedLocalOffset;
                vertex = RotatePoint(vertex, -storedRelativeAngle);
                neighbourData.ModifiedVertices.Add(vertex);
            }
        }

        // Store also current cells modified vertices so that when other neighbours are processed they can
        // see those changes and avoid overlaps
        cellData.ModifiedVertices ??= new List<Vector2>(vertices2D.Count);
        cellData.ModifiedVertices.Clear();

        for (int i = 0; i < vertices2D.Count; ++i)
        {
            cellData.ModifiedVertices.Add(vertices2D[i]);
        }
    }

    /// <summary>
    ///   Preprocess all neighbours in closeNeighboursKeys by rotating and shifting their original vertices
    ///   into this cell's local space, storing results in dictionaries to avoid per-iteration allocations.
    /// </summary>
    private void RotateAndShiftCloseNeighbours(ConcurrentDictionary<long, NeighbourData> neighboursData,
        float thisAngle, Vector2 thisCellPosition)
    {
        originalNeighboursVertices.Clear();
        editableNeighboursVertices.Clear();
        neighbourShifts.Clear();
        neighbourRotations.Clear();

        foreach (var neighbourKey in closeNeighboursKeys)
        {
            var neighbourData = neighboursData[neighbourKey];

            var neighbourAngle = GetOrientationAngle(neighbourData.Orientation);
            var relativeAngle = neighbourAngle - thisAngle;

            var worldOffset = neighbourData.CellPosition - thisCellPosition;
            var localOffset = RotatePoint(worldOffset, -thisAngle);

            var vertexCount = neighbourData.OriginalPointData.VertexCount;

            var originalList = new List<Vector2>(vertexCount);
            var shiftedList = new List<Vector2>(vertexCount);
            originalNeighboursVertices.Add(neighbourKey, originalList);
            editableNeighboursVertices.Add(neighbourKey, shiftedList);

            if (neighbourData.ModifiedVertices != null)
            {
                for (int i = 0; i < vertexCount; ++i)
                {
                    var original = neighbourData.OriginalPointData.Vertices2D[i];
                    var modified = neighbourData.ModifiedVertices[i];
                    var transformedOriginal = RotatePoint(original, relativeAngle) + localOffset;
                    var transformedModified = RotatePoint(modified, relativeAngle) + localOffset;
                    originalList.Add(transformedOriginal);
                    shiftedList.Add(transformedModified);
                }
            }
            else
            {
                for (int i = 0; i < vertexCount; ++i)
                {
                    var original = neighbourData.OriginalPointData.Vertices2D[i];
                    var transformedOriginal = RotatePoint(original, relativeAngle) + localOffset;
                    originalList.Add(transformedOriginal);
                    shiftedList.Add(transformedOriginal);
                }
            }

            neighbourShifts[neighbourKey] = localOffset;
            neighbourRotations[neighbourKey] = relativeAngle;
        }
    }

    private void SetCellVertices(NeighbourData currentCellData)
    {
        vertices2D.Clear();
        originalVertices2D.Clear();

        if (currentCellData.ModifiedVertices != null)
        {
            for (int i = 0; i < currentCellData.OriginalPointData.VertexCount; ++i)
            {
                originalVertices2D.Add(currentCellData.OriginalPointData.Vertices2D[i]);
                vertices2D.Add(currentCellData.ModifiedVertices[i]);
            }
        }
        else
        {
            for (int i = 0; i < currentCellData.OriginalPointData.VertexCount; ++i)
            {
                originalVertices2D.Add(currentCellData.OriginalPointData.Vertices2D[i]);
                vertices2D.Add(currentCellData.OriginalPointData.Vertices2D[i]);
            }
        }
    }

    /// <summary>
    ///   Calculates weighted middle point between cells' average vertex, based on how much the membrane reaches out
    ///   from that average point to the other average point
    /// </summary>
    private Vector2 GetMiddlePoint(List<Vector2> neighbourVertices2D, Vector2 averageVertex,
        Vector2 neighbourAverageVertex, out int closestVertexIndex,
        out int closestNeighbourVertexIndex)
    {
        // Find how far each cell's membrane reaches toward the other
        float thisReach = GetMembraneReachToward(vertices2D, averageVertex, neighbourAverageVertex,
            out closestVertexIndex);
        float neighbourReach =
            GetMembraneReachToward(neighbourVertices2D, neighbourAverageVertex, averageVertex,
                out closestNeighbourVertexIndex);

        float totalReach = thisReach + neighbourReach;

        // Fallback to just the middle just in case
        float calculatedReach = totalReach > 0 ? thisReach / totalReach : 0.5f;

        // Weighted middle point
        var middlePoint = averageVertex + (neighbourAverageVertex - averageVertex) * calculatedReach;
        return middlePoint;
    }

    private void GetCloseNeighbours(ConcurrentDictionary<long, NeighbourData> neighboursData,
        Vector2 thisCellPosition)
    {
        closeNeighboursKeys.Clear();

        foreach (var (neighbourKey, neighbourData) in neighboursData)
        {
            var otherCellPosition = neighbourData.CellPosition;

            if (neighbourKey != currentCellKey &&
                otherCellPosition.DistanceSquaredTo(thisCellPosition) <=
                Constants.MEMBRANE_NEIGHBOUR_MAX_DISTANCE_BETWEEN_CENTERS)
            {
                closeNeighboursKeys.Add(neighbourKey);
            }
        }
    }

    /// <summary>
    ///   Cast the vertices between tangent point on to the lines created by tangent points and middle point
    ///   int the direction of averageVertext->middlePoint
    /// </summary>
    private bool CastVerticesOntoTheTangentLines(List<Vector2> neighbourCellVertices, Vector2 middlePoint,
        Vector2 averageVertex, Vector2 neighbourAverageVertex, int closestVertexIndex, int neighbourClosestVertexIndex)
    {
        var castDirection = middlePoint - averageVertex;
        castDirection = castDirection.Normalized();

        // extend the middle point a little bit away from middle point,
        // so that vertices smoothing doesn't create a big gap
        var extendedMiddlePointForCurrentCell =
            middlePoint + castDirection * Constants.MEMBRANE_MIDDLE_POINT_OVERREACH;
        var extendedMiddlePointForNeighbourCell =
            middlePoint - castDirection * Constants.MEMBRANE_MIDDLE_POINT_OVERREACH;

        CalculateTangentPoints(vertices2D, averageVertex, extendedMiddlePointForCurrentCell,
            out var tangentPointIndexA, out var tangentPointIndexB);
        CalculateTangentPoints(neighbourCellVertices, neighbourAverageVertex, extendedMiddlePointForNeighbourCell,
            out var tangentPointIndexC, out var tangentPointIndexD);

        var tangentLineA = vertices2D[tangentPointIndexA] - extendedMiddlePointForCurrentCell;
        var tangentLineB = vertices2D[tangentPointIndexB] - extendedMiddlePointForCurrentCell;
        var tangentLineC = neighbourCellVertices[tangentPointIndexC] - extendedMiddlePointForNeighbourCell;
        var tangentLineD = neighbourCellVertices[tangentPointIndexD] - extendedMiddlePointForNeighbourCell;

        CalculateIterationIndices(vertices2D, tangentPointIndexA, tangentPointIndexB, closestVertexIndex,
            out var indexStart, out var indexEnd);
        CalculateIterationIndices(neighbourCellVertices, tangentPointIndexC, tangentPointIndexD,
            neighbourClosestVertexIndex, out var neighbourIndexStart, out var neighbourIndexEnd);

        // Store casts in a temporary dictionary before applying them
        validPointsCasts.Clear();
        validNeighbourPointsCasts.Clear();

        var canCastVertices = CanCastPoints(validPointsCasts, vertices2D, extendedMiddlePointForCurrentCell, indexStart,
            indexEnd, castDirection, tangentLineA, tangentLineB);
        canCastVertices &= CanCastPoints(validNeighbourPointsCasts, neighbourCellVertices,
            extendedMiddlePointForNeighbourCell, neighbourIndexStart, neighbourIndexEnd, -castDirection, tangentLineC,
            tangentLineD);

        if (!canCastVertices)
        {
            return false;
        }

        foreach (var castPoints in validPointsCasts)
        {
            vertices2D[castPoints.Key] = castPoints.Value;
        }

        foreach (var castPoints in validNeighbourPointsCasts)
        {
            neighbourCellVertices[castPoints.Key] = castPoints.Value;
        }

        SmoothVertexRegion(vertices2D, indexStart, indexEnd);
        SmoothVertexRegion(neighbourCellVertices, neighbourIndexStart, neighbourIndexEnd);

        return true;
    }

    private bool CanCastPoints(Dictionary<int, Vector2> validCasts, List<Vector2> vertices, Vector2 middlePoint,
        int indexStart, int indexEnd, Vector2 castDirection, Vector2 tangentLineA, Vector2 tangentLineB)
    {
        for (var i = indexStart; i < indexEnd; ++i)
        {
            int index = i % vertices.Count;
            var point = vertices[index];

            var hitLineA = CalculateRayLineIntersection(point, castDirection, middlePoint,
                tangentLineA, out float distanceToLeftBoundary);

            var hitLineB = CalculateRayLineIntersection(point, castDirection, middlePoint,
                tangentLineB, out float distanceToRightBoundary);

            if (!hitLineA || !hitLineB)
            {
                GD.PrintErr("Failed to find intersection for membrane point with tangent lines");
                return false;
            }

            // TODO: throw if negative
            var distance = Math.Min(distanceToLeftBoundary, distanceToRightBoundary);
            var newPosition = point + castDirection * distance;

            validCasts.Add(index, newPosition);

            // Check that the new position does not lie inside any other neighbour's (preprocessed) polygon.
            foreach (var otherNeighbourKey in closeNeighboursKeys)
            {
                if (otherNeighbourKey == neighbourCellKey || otherNeighbourKey == currentCellKey)
                    continue;

                if (editableNeighboursVertices.TryGetValue(otherNeighbourKey, out var otherNeighbourVerts))
                {
                    if (IsInsideConvexPolygon(otherNeighbourVerts, newPosition))
                    {
                        // Reject this cast if it would put a vertex inside another neighbour.
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    ///   Gets two points on the membrane that create lines going through middlePoint that are tangent to the membrane
    /// </summary>
    private void CalculateTangentPoints(List<Vector2> vertices, Vector2 averageVertex, Vector2 middlePoint,
        out int tangentPointIndexA,
        out int tangentPointIndexB)
    {
        tangentPointIndexA = 0;
        tangentPointIndexB = 0;
        var maxPositiveAngle = 0.0f;
        var maxNegativeAngle = 0.0f;

        for (var i = 0; i < vertices.Count; ++i)
        {
            var point = vertices[i];
            var angle = Angle(averageVertex, middlePoint, point);

            if (angle > maxPositiveAngle)
            {
                maxPositiveAngle = angle;
                tangentPointIndexA = i;
            }
            else if (angle < maxNegativeAngle)
            {
                maxNegativeAngle = angle;
                tangentPointIndexB = i;
            }
        }
    }
}
