using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    ///   Buffer for starting points when generating membrane data
    /// </summary>
    private readonly List<Vector2> startingBuffer = new();

    /// <summary>
    ///   Gets a generator for the current thread. This is required to be used as the generators are not thread safe.
    /// </summary>
    /// <returns>A generator that can be used by the calling thread</returns>
    public static MembraneShapeGenerator GetThreadSpecificGenerator()
    {
        return ThreadLocalGenerator.Value!;
    }

    /// <summary>
    ///   Generates the 2D points for a membrane given the parameters that affect the shape
    /// </summary>
    /// <returns>
    ///   Computed data in a cache entry format (to be used with <see cref="ProceduralDataCache"/>, which should be
    ///   checked for existing data before computing new data)
    /// </returns>
    public MembranePointData GenerateMicrobeShape(Vector2[] hexPositions, int hexCount, MembraneType membraneType,
        bool isMulticellular)
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

    public MembranePointData GenerateMulticellularMembrane(string thisCellKey,
        ConcurrentDictionary<string, NeighbourData> neighboursData,
        Vector2[] multicellularPositions, int[]? multicellularOrientations)
    {
        var currentCellData = neighboursData[thisCellKey];
        var originalPointData = currentCellData.OriginalPointData;
        var thisCellPosition = currentCellData.CellPosition;
        var thisCellOrientation = currentCellData.Orientation;

        vertices2D.Clear();

        for (int i = 0; i < originalPointData.VertexCount; ++i)
        {
            vertices2D.Add(originalPointData.Vertices2D[i]);
        }

        GenerateMulticellularMembrane(thisCellKey, neighboursData);

        // Apply the same waviness pass used for single-cell membranes
        float circumference = 0.0f;
        for (int i = 0, end = vertices2D.Count; i < end; ++i)
            circumference += (vertices2D[(i + 1) % end] - vertices2D[i]).Length();

        MakeMembraneWavier(originalPointData.Type, circumference);

        int hexCount = originalPointData.HexPositionCount;
        var hexCopy = ArrayPool<Vector2>.Shared.Rent(hexCount);
        originalPointData.HexPositions.AsSpan(0, hexCount).CopyTo(hexCopy);

        // TODO: move to generatemembrane
        return new MembranePointData(hexCopy, hexCount,
            originalPointData.Type,
            vertices2D, multicellularPositions, thisCellPosition, multicellularOrientations, thisCellOrientation);
    }

    public MembranePointData GenerateMicrobeShape(ref MembraneGenerationParameters parameters,
        bool isMulticellular = false)
    {
        return GenerateMicrobeShape(parameters.HexPositions, parameters.HexPositionCount,
            parameters.Type, isMulticellular);
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

    // rotate helper: rotate a vector around origin
    private static Vector2 Rotate(Vector2 v, float angle)
    {
        (float s, float c) = MathF.SinCos(angle);
        return new Vector2(c * v.X - s * v.Y, s * v.X + c * v.Y);
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

    private void GenerateMulticellularMembrane(string thisCellKey,
        ConcurrentDictionary<string, NeighbourData> neighboursData)
    {
        var currentCellData = neighboursData[thisCellKey];
        var thisCellPosition = currentCellData.CellPosition;
        var thisCellOrientation = currentCellData.Orientation;
        float thisAngle = thisCellOrientation * (MathF.PI / 3f);

        var averageVertex = GetAverageVertex(vertices2D);

        List<string> closeNeighbours = [];

        foreach (var (neighbourKey, neighbourData) in neighboursData)
        {
            var otherCellPosition = neighbourData.CellPosition;

            if (neighbourKey != thisCellKey &&
                otherCellPosition.DistanceTo(thisCellPosition) <= 20)
            {
                closeNeighbours.Add(neighbourKey);
            }
        }

        // Pre-process all close neighbours: rotate and offset their vertices
        var allNeighbourVertices = new Dictionary<string, List<Vector2>>();
        foreach (var neighbourKey in closeNeighbours)
        {
            var neighbourData = neighboursData[neighbourKey];
            var neighbourVertices = RotateNeighbourVertices(neighbourData, thisAngle);

            var worldOffset = neighbourData.CellPosition - thisCellPosition;
            var localOffset = Rotate(worldOffset, -thisAngle);

            for (int i = 0; i < neighbourVertices.Count; ++i)
            {
                neighbourVertices[i] += localOffset;
            }

            allNeighbourVertices[neighbourKey] = neighbourVertices;
        }

        // Process neighbours using rotated coordinates
        foreach (var neighbourKey in closeNeighbours)
        {
            var neighbourData = neighboursData[neighbourKey];
            var otherCellPosition = neighbourData.CellPosition;

            if (neighbourKey == thisCellKey ||
                otherCellPosition.DistanceTo(thisCellPosition) > 20)
                continue;

            var neighbourVertices = allNeighbourVertices[neighbourKey];
            var localNeighbourAverageVertex = GetAverageVertex(neighbourVertices);

            // Find how far each cell's membrane reaches toward the other
            float thisReach = GetMembraneReachToward(vertices2D, averageVertex, localNeighbourAverageVertex);
            float neighbourReach =
                GetMembraneReachToward(neighbourVertices, localNeighbourAverageVertex, averageVertex);

            // Weight the point: cells with more reach "pull" the point toward themselves
            // i.e. a bigger membrane contribution means the junction sits closer to you
            float totalReach = thisReach + neighbourReach;
            float t = (totalReach > 0) ? thisReach / totalReach : 0.5f;

            var middlePointBetweenAvarageVertices = averageVertex + (localNeighbourAverageVertex - averageVertex) * t;

            var skipGeneration = IsInsideConvexPolygon(vertices2D, middlePointBetweenAvarageVertices) ||
                IsInsideConvexPolygon(neighbourVertices, middlePointBetweenAvarageVertices);

            if (skipGeneration)
                continue;

            middlePointBetweenAvarageVertices +=
                (middlePointBetweenAvarageVertices - averageVertex).Normalized() * 0.5f;

            GetTangentPoints(averageVertex, middlePointBetweenAvarageVertices,
                out var tangentPointIndex1, out var tangentPointIndex2);

            // Collect all other close neighbors to check for collisions
            var otherNeighbours = new List<List<Vector2>>();
            foreach (var otherNeighbourKey in closeNeighbours)
            {
                if (otherNeighbourKey == neighbourKey)
                    continue;

                otherNeighbours.Add(allNeighbourVertices[otherNeighbourKey]);
            }

            CastVerticesOntoTheCone(middlePointBetweenAvarageVertices, averageVertex, tangentPointIndex1,
                tangentPointIndex2, neighbourVertices, otherNeighbours);
        }

        // GD.Print("After:");
        // GD.Print(string.Join(",",
        //     vertices2D.Select(v => $"({v.X},{v.Y})")));
    }

    private void CastVerticesOntoTheCone(Vector2 middlePointBetweenAvarageVertices, Vector2 averageVertex,
        int tangentPoint1, int tangentPoint2, List<Vector2> neighbourVertices, List<List<Vector2>> otherNeighbours)
    {
        var castDirection = middlePointBetweenAvarageVertices - averageVertex;
        if (castDirection.LengthSquared() < 1e-6f)
            return;

        castDirection = castDirection.Normalized();

        int n = vertices2D.Count;

        var coneEdgeA = vertices2D[tangentPoint1] - middlePointBetweenAvarageVertices;
        var coneEdgeB = vertices2D[tangentPoint2] - middlePointBetweenAvarageVertices;

        if (coneEdgeA.LengthSquared() < 1e-6f || coneEdgeB.LengthSquared() < 1e-6f)
            return;

        // define from which index of verices2D to which index the points should be moved
        int forwardLength = (tangentPoint2 - tangentPoint1 + n) % n;
        int backwardLength = (tangentPoint1 - tangentPoint2 + n) % n;

        GetIterationIndices(tangentPoint1, tangentPoint2, forwardLength, backwardLength, out var indexStart,
            out var indexEnd);

        // Store casts in a temporary dictionary before applying them
        var validCasts = new Dictionary<int, Vector2>();

        // Check every other vertex to reduce calculations
        for (var i = indexStart; i < indexEnd; i++)
        {
            int idx = i % n;
            var point = vertices2D[idx];

            var hitLeftConeBoundary = FindRayLineIntersection(point, castDirection, middlePointBetweenAvarageVertices,
                coneEdgeA, out float distanceToLeftBoundary);

            var hitRightConeBoundary = FindRayLineIntersection(point, castDirection, middlePointBetweenAvarageVertices,
                coneEdgeB, out float distanceToRightBoundary);

            var projectedOntoLeftBoundary = hitLeftConeBoundary ?
                point + castDirection * distanceToLeftBoundary :
                vertices2D[tangentPoint1];

            var projectedOntoRightBoundary = hitRightConeBoundary ?
                point + castDirection * distanceToRightBoundary :
                vertices2D[tangentPoint2];

            var leftBoundaryIsCloser =
                hitLeftConeBoundary &&
                (!hitRightConeBoundary || MathF.Abs(distanceToLeftBoundary) < MathF.Abs(distanceToRightBoundary));

            Vector2 newPosition;
            if (leftBoundaryIsCloser)
            {
                newPosition = projectedOntoLeftBoundary;
            }
            else if (hitRightConeBoundary)
            {
                newPosition = projectedOntoRightBoundary;
            }
            else
            {
                return;
            }

            validCasts[idx] = newPosition;

            foreach (var otherNeighbour in otherNeighbours)
            {
                if (IsInsideConvexPolygon(otherNeighbour, newPosition))
                {
                    return;
                }
            }
        }

        foreach (var kvp in validCasts)
        {
            vertices2D[kvp.Key] = kvp.Value;
        }

        SmoothVertexRegion(indexStart, indexEnd, n, 1);
    }

    private void SmoothVertexRegion(int indexStart, int indexEnd, int n, int iterations = 2)
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = indexStart + 1; i < indexEnd - 1; i++)
            {
                int idx = i % n;
                int prev = (i - 1) % n;
                int next = (i + 1) % n;

                // Simple average of neighbors — pulls sharp corners toward center
                vertices2D[idx] = (vertices2D[prev] + 1 * vertices2D[idx] + vertices2D[next]) * 0.3333f;
            }
        }
    }

    /// <summary>
    /// Finds the intersection
    /// Returns false when the ray and line are parallel.
    /// rayDistance may be negative, meaning the intersection
    /// lies behind rayOrigin relative to rayDirection.
    /// </summary>
    private bool FindRayLineIntersection(Vector2 rayOrigin,
        Vector2 rayDirection,
        Vector2 linePoint,
        Vector2 lineDirection,
        out float rayDistance)
    {
        float determinant =
            rayDirection.X * lineDirection.Y -
            rayDirection.Y * lineDirection.X;

        rayDistance = 0;

        // Parallel lines
        if (MathF.Abs(determinant) < 1e-6f)
            return false;

        Vector2 offset = linePoint - rayOrigin;

        rayDistance =
            (offset.X * lineDirection.Y -
                offset.Y * lineDirection.X) / determinant;

        return true;
    }

    private static void GetIterationIndices(int tangentPoint1, int tangentPoint2, int forwardLength, int backwardLength,
        out int indexStart, out int indexEnd)
    {
        if (forwardLength <= backwardLength)
        {
            indexStart = tangentPoint1;
            indexEnd = tangentPoint1 + forwardLength;
        }
        else
        {
            indexStart = tangentPoint2;
            indexEnd = tangentPoint2 + backwardLength;
        }
    }

    private void GetTangentPoints(Vector2 averageVertex, Vector2 middlePointBetweenAvarageVertices,
        out int tangentPointIndex1, out int tangentPointIndex2)
    {
        tangentPointIndex1 = 0;
        tangentPointIndex2 = 0;
        var maxPositiveAngle = 0.0f;
        var maxNegativeAngle = 0.0f;

        for (var i = 0; i < vertices2D.Count; i++)
        {
            var point = vertices2D[i];
            var angle = Angle(averageVertex, middlePointBetweenAvarageVertices, point);

            if (angle > maxPositiveAngle)
            {
                maxPositiveAngle = angle;
                tangentPointIndex1 = i;
            }
            else if (angle < maxNegativeAngle)
            {
                maxNegativeAngle = angle;
                tangentPointIndex2 = i;
            }
        }
    }

    private static bool IsInsideConvexPolygon(List<Vector2> vertices, Vector2 point)
    {
        int n = vertices.Count;
        if (n < 3)
            return false;

        // Determine winding from the first edge so we know which sign = "inside"
        float firstCross = Cross(vertices[0], vertices[1], point);

        for (int i = 1; i < n; i++)
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

    // 2D cross product of vectors (b-a) and (p-a).
    // Positive = p is left of edge a→b, negative = right, zero = collinear.
    private static float Cross(Vector2 a, Vector2 b, Vector2 p)
    {
        return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
    }

    private static List<Vector2>
        RotateNeighbourVertices(NeighbourData neighbourData, float thisAngle)
    {
        // Neighbour vertices are in neighbour's local space.
        // To bring them into THIS cell's local space:
        //   1. rotate by neighbourOrientation (orient them in world space)
        //   2. counter-rotate by thisAngle (bring into this cell's local space)
        float neighbourAngle = neighbourData.Orientation * (MathF.PI / 3f);
        float relativeAngle = neighbourAngle - thisAngle;

        var neighbourVertices = new List<Vector2>(neighbourData.OriginalPointData.VertexCount);
        for (int i = 0; i < neighbourData.OriginalPointData.VertexCount; ++i)
            neighbourVertices.Add(neighbourData.OriginalPointData.Vertices2D[i]);
        if (relativeAngle != 0)
        {
            for (int i = 0; i < neighbourVertices.Count; ++i)
                neighbourVertices[i] = Rotate(neighbourVertices[i], relativeAngle);
        }

        return neighbourVertices;
    }

    private Vector2 GetAverageVertex(List<Vector2> vertices)
    {
        var averageVertex = Vector2.Zero;
        foreach (var vertex in vertices)
            averageVertex += vertex;
        averageVertex /= vertices.Count;

        return averageVertex;
    }

    private float Angle(Vector2 a, Vector2 b, Vector2 c)
    {
        var ba = a - b;
        var bc = c - b;

        var angle = ba.AngleTo(bc) * 180f / MathF.PI;
        if (angle > 180)
        {
            angle -= 360;
        }

        return angle;
    }

    /// <summary>
    /// Finds the vertex closest to the half-line from <paramref name="origin"/> toward <paramref name="target"/>,
    /// and returns its projection distance along that direction.
    /// </summary>
    private float GetMembraneReachToward(List<Vector2> vertices, Vector2 origin, Vector2 target)
    {
        var direction = (target - origin).Normalized();
        float minDistToRay = float.MaxValue;
        float bestProjection = 0f;

        foreach (var vertex in vertices)
        {
            var toVertex = vertex - origin;
            float projection = toVertex.Dot(direction);

            // Only consider vertices on the forward half
            if (projection <= 0f)
                continue;

            // Perpendicular distance from vertex to the ray
            float distToRay = (toVertex - direction * projection).Length();

            if (distToRay < minDistToRay)
            {
                minDistToRay = distToRay;
                bestProjection = projection;
            }
        }

        return bestProjection;
    }
}
