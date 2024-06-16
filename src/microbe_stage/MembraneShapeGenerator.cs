using System;
using System.Collections.Generic;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   This implements the membrane algorithm from going from 2D hex locations to a membrane mesh that can be used for
///   rendering or calculations that use the membrane point
/// </summary>
public class MembraneShapeGenerator
{
    // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
    // private static readonly ThreadLocal<MembraneShapeGenerator> ThreadLocalGenerator =
    //    new(() => new MembraneShapeGenerator());

    private static readonly Lazy<MembraneShapeGenerator> ThreadLocalGenerator =
        new(() => new MembraneShapeGenerator());

    /// <summary>
    ///   Amount of segments on one side of the above described square. The amount of points on the side of
    ///   the membrane.
    /// </summary>
    private static readonly int MembraneResolution = Constants.MEMBRANE_RESOLUTION;

    /// <summary>
    ///   Stores the generated 2-Dimensional membrane. Needed as easily resizable target work area. Data is copied
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
        return ThreadLocalGenerator.Value;
    }

    /// <summary>
    ///   Generates the 2D points for a membrane given the parameters that affect the shape
    /// </summary>
    /// <returns>
    ///   Computed data in a cache entry format (to be used with <see cref="ProceduralDataCache"/>, which should be
    ///   checked for existing data before computing new data)
    /// </returns>
    public MembranePointData GenerateShape(Vector2[] hexPositions, int hexCount, MembraneType membraneType)
    {
        // The length in pixels (probably not accurate?) of a side of the square that bounds the membrane.
        // Half the side length of the original square that is compressed to make the membrane.
        int cellDimensions = 10;

        for (int i = 0; i < hexCount; ++i)
        {
            var pos = hexPositions[i];
            if (Mathf.Abs(pos.X) + 1 > cellDimensions)
            {
                cellDimensions = (int)Mathf.Abs(pos.X) + 1;
            }

            if (Mathf.Abs(pos.Y) + 1 > cellDimensions)
            {
                cellDimensions = (int)Mathf.Abs(pos.Y) + 1;
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

        // Get new membrane points for vertices2D
        GenerateMembranePoints(hexPositions, hexCount, membraneType);

        // This makes a copy of the vertices so the data is safe to modify in further calls to this method
        return new MembranePointData(hexPositions, hexCount, membraneType, vertices2D);
    }

    public MembranePointData GenerateShape(ref MembraneGenerationParameters parameters)
    {
        return GenerateShape(parameters.HexPositions, parameters.HexPositionCount, parameters.Type);
    }

    public MembranePointData GenerateShape(IMembraneDataSource parameters)
    {
        return GenerateShape(parameters.HexPositions, parameters.HexPositionCount, parameters.Type);
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
        var mesh = BuildMesh(shapeData.Vertices2D, shapeData.VertexCount, shapeData.Type, out var surfaceIndex);

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

    private static int InitializeCorrectMembrane(Vector2[] vertices2D, int vertexCount, int writeIndex,
        Vector3[] vertices, Vector2[] uvs, MembraneType membraneType)
    {
        // common variables
        float height = 0.1f;
        float multiplier = 2.0f * Mathf.Pi;
        var center = new Vector2(0.5f, 0.5f);

        // cell walls need obvious inner/outer membranes (we can worry
        // about chitin later)
        if (membraneType.CellWall)
        {
            height = 0.05f;
        }

        vertices[writeIndex] = new Vector3(0, height / 2, 0);
        uvs[writeIndex] = center;
        ++writeIndex;

        for (int i = 0, end = vertexCount; i < end + 1; i++)
        {
            // Finds the UV coordinates be projecting onto a plane and
            // stretching to fit a circle.

            float currentRadians = multiplier * i / end;

            var sourceVertex = vertices2D[i % end];
            vertices[writeIndex] = new Vector3(sourceVertex.X, height / 2, sourceVertex.Y);

            uvs[writeIndex] = center +
                new Vector2(Mathf.Cos(currentRadians), Mathf.Sin(currentRadians)) / 2;

            ++writeIndex;
        }

        return writeIndex;
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

    /// <summary>
    ///   Creates the actual mesh object.
    /// </summary>
    private static ArrayMesh BuildMesh(Vector2[] vertices2D, int vertexCount, MembraneType membraneType,
        out int surfaceIndex)
    {
        // This is actually a triangle list, but the index buffer is used to build
        // the indices (to emulate a triangle fan)
        var bufferSize = vertexCount + 2;
        var indexSize = vertexCount * 3;

        var arrays = new Array();
        arrays.Resize((int)Mesh.ArrayType.Max);

        // Build vertex, index, and uv lists

        // Index mapping to build all triangles
        var indices = new int[indexSize];
        int currentVertexIndex = 1;

        for (int i = 0; i < indexSize; i += 3)
        {
            indices[i] = 0;
            indices[i + 1] = currentVertexIndex + 1;
            indices[i + 2] = currentVertexIndex;

            ++currentVertexIndex;
        }

        // Write mesh data //
        var vertices = new Vector3[bufferSize];
        var uvs = new Vector2[bufferSize];

        int writeIndex = 0;
        writeIndex = InitializeCorrectMembrane(vertices2D, vertexCount, writeIndex, vertices, uvs, membraneType);

        if (writeIndex != bufferSize)
            throw new Exception("Membrane buffer write ended up at wrong index");

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

            vertices[index] = new Vector3(0.5f, height / 2, 0.5f);
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

    private void GenerateMembranePoints(Vector2[] hexPositions, int hexCount, MembraneType membraneType)
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

        float waveFrequency = 2.0f * Mathf.Pi * Constants.MEMBRANE_NUMBER_OF_WAVES / vertices2D.Count;

        float heightMultiplier = membraneType.CellWall ?
            Constants.MEMBRANE_WAVE_HEIGHT_MULTIPLIER_CELL_WALL :
            Constants.MEMBRANE_WAVE_HEIGHT_MULTIPLIER;

        float waveHeight = Mathf.Pow(circumference, Constants.MEMBRANE_WAVE_HEIGHT_DEPENDENCE_ON_SIZE)
            * heightMultiplier;

        // Make the membrane wavier
        for (int i = 0, end = vertices2D.Count; i < end; ++i)
        {
            var point = vertices2D[i];
            var nextPoint = vertices2D[(i + 1) % end];
            var direction = (nextPoint - point).Normalized();

            // Turn 90 degrees
            direction = new Vector2(-direction.Y, direction.X);

            var movement = direction * Mathf.Sin(waveFrequency * i) * waveHeight;

            vertices2D[i] = point + movement;
        }
    }
}
