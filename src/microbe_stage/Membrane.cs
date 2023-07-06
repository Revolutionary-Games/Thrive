using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using Array = Godot.Collections.Array;

/// <summary>
///   Membrane for microbes
/// </summary>
public class Membrane : MeshInstance, IComputedMembraneData
{
    [Export]
    public ShaderMaterial? MaterialToEdit;

    private static readonly List<Vector2> PreviewMembraneOrganellePositions = new() { new Vector2(0, 0) };

    /// <summary>
    ///   Stores the generated 2-Dimensional membrane. Needed for contains calculations
    /// </summary>
    private readonly List<Vector2> vertices2D = new();

    /// <summary>
    ///   Buffer for starting points when generating membrane data
    /// </summary>
    private readonly List<Vector2> startingBuffer = new();

    private float healthFraction = 1.0f;
    private float wigglyNess = 1.0f;
    private float sizeWigglyNessDampeningFactor = 0.22f;
    private float movementWigglyNess = 1.0f;
    private float sizeMovementWigglyNessDampeningFactor = 0.32f;
    private Color tint = Colors.White;
    private float dissolveEffectValue;

    private MembraneType? type;

#pragma warning disable CA2213
    private Texture? albedoTexture;
    private Texture noiseTexture = null!;
#pragma warning restore CA2213

    private string? currentlyLoadedAlbedoTexture;

    private bool dirty = true;
    private bool radiusIsDirty = true;
    private bool convexShapeIsDirty = true;
    private float cachedRadius;
    private Vector3[] cachedConvexShape = null!;

    /// <summary>
    ///   Amount of segments on one side of the above described
    ///   square. The amount of points on the side of the membrane.
    /// </summary>
    private int membraneResolution = Constants.MEMBRANE_RESOLUTION;

    /// <summary>
    ///   When true the mesh needs to be regenerated and material properties applied
    /// </summary>
    public bool Dirty
    {
        get => dirty;
        set
        {
            if (value)
            {
                radiusIsDirty = true;
                convexShapeIsDirty = true;
            }

            dirty = value;
        }
    }

    /// <summary>
    ///   Organelle positions of the microbe, needs to be set for the membrane to appear. This includes all hex
    ///   positions of the organelles to work better with cells that have multihex organelles. As a result this
    ///   doesn't contain just the center positions of organelles but will contain multiple entries for multihex
    ///   organelles.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The contents in this list should not be modified, a new list should be assigned.
    ///   </para>
    /// </remarks>
    public IReadOnlyList<Vector2> OrganellePositions { get; set; } = PreviewMembraneOrganellePositions;

    /// <summary>
    ///   Returns a convex shaped 3-Dimensional array of vertices from the generated <see cref="vertices2D"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: This is not the same as the 3D vertices used for the visuals.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public Vector3[] ConvexShape
    {
        get
        {
            if (convexShapeIsDirty)
            {
                if (Dirty)
                    Update();

                float height = 0.1f;

                if (Type.CellWall)
                    height = 0.05f;

                cachedConvexShape = new Vector3[vertices2D.Count];
                for (var i = 0; i < vertices2D.Count; ++i)
                {
                    cachedConvexShape[i] = new Vector3(vertices2D[i].x, height / 2, vertices2D[i].y);
                }

                convexShapeIsDirty = false;
            }

            return cachedConvexShape;
        }
    }

    /// <summary>
    ///   The type of the membrane.
    /// </summary>
    /// <exception cref="InvalidOperationException">When trying to read before this is initialized</exception>
    /// <exception cref="ArgumentNullException">If value is attempted to be set to null</exception>
    public MembraneType Type
    {
        get => type ?? throw new InvalidOperationException("Membrane type has not been set yet");
        set
        {
            if (value == null)
                throw new ArgumentNullException();

            if (type == value)
                return;

            type = value;
            dirty = true;
        }
    }

    /// <summary>
    ///   How healthy the cell is, mixes in a damaged texture. Range 0.0f - 1.0f
    /// </summary>
    public float HealthFraction
    {
        get => healthFraction;
        set
        {
            value = value.Clamp(0.0f, 1.0f);
            if (value == HealthFraction)
                return;

            healthFraction = value;
            ApplyHealth();
        }
    }

    /// <summary>
    ///   How much the membrane wiggles. Used values are 0 and 1
    /// </summary>
    public float WigglyNess
    {
        get => wigglyNess;
        set
        {
            wigglyNess = Mathf.Clamp(value, 0.0f, 1.0f);
            ApplyWiggly();
        }
    }

    public float MovementWigglyNess
    {
        get => movementWigglyNess;
        set
        {
            movementWigglyNess = Mathf.Clamp(value, 0.0f, 1.0f);
            ApplyMovementWiggly();
        }
    }

    public Color Tint
    {
        get => tint;
        set
        {
            // Desaturate it here so it looks nicer (could implement as method that
            // could be called i suppose)

            // According to stack overflow HSV and HSB are the same thing
            value.ToHsv(out var hue, out var saturation, out var brightness);

            value = Color.FromHsv(hue, saturation * 0.75f, brightness,
                Mathf.Clamp(value.a, 0.4f - brightness * 0.3f, 1.0f));

            if (tint == value)
                return;

            tint = value;

            // If we already have created a material we need to re-apply it
            ApplyTint();
        }
    }

    /// <summary>
    ///   Quick radius value for the membrane size
    /// </summary>
    public float EncompassingCircleRadius
    {
        get
        {
            if (radiusIsDirty)
            {
                cachedRadius = CalculateEncompassingCircleRadius();
                radiusIsDirty = false;
            }

            return cachedRadius;
        }
    }

    public float DissolveEffectValue
    {
        get => dissolveEffectValue;
        set
        {
            dissolveEffectValue = value;
            ApplyDissolveEffect();
        }
    }

    public override void _Ready()
    {
        type ??= SimulationParameters.Instance.GetMembrane("single");

        if (MaterialToEdit == null)
            GD.PrintErr("MaterialToEdit on Membrane is not set");

        noiseTexture = GD.Load<Texture>("res://assets/textures/dissolve_noise.tres");

        Dirty = true;
    }

    public override void _Process(float delta)
    {
        if (!Dirty)
            return;

        Update();
    }

    /// <summary>
    ///   Sees if the given point is inside the membrane.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is quite an expensive method as this loops all the vertices
    ///   </para>
    /// </remarks>
    public bool Contains(float x, float y)
    {
        bool crosses = false;

        int n = vertices2D.Count;
        for (int i = 0; i < n - 1; i++)
        {
            if ((vertices2D[i].y <= y && y < vertices2D[i + 1].y) ||
                (vertices2D[i + 1].y <= y && y < vertices2D[i].y))
            {
                if (x < (vertices2D[i + 1].x - vertices2D[i].x) *
                    (y - vertices2D[i].y) /
                    (vertices2D[i + 1].y - vertices2D[i].y) +
                    vertices2D[i].x)
                {
                    crosses = !crosses;
                }
            }
        }

        return crosses;
    }

    /// <summary>
    ///   Finds the point on the membrane nearest to the given point.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Used for finding out where to put an external organelle.
    ///   </para>
    ///   <para>
    ///     The returned Vector is in world coordinates (x, 0, z) and
    ///     not in internal membrane coordinates (x, y, 0). This is so
    ///     that gameplay code doesn't have to do the conversion
    ///     everywhere this is used.
    ///   </para>
    /// </remarks>
    public Vector3 GetVectorTowardsNearestPointOfMembrane(float x, float y)
    {
        // Calculate now if dirty to make flagella positioning only have to be done once
        // NOTE: that flagella position should only be read once all organelles that are
        // going to be added / removed on this game update are done.
        if (Dirty)
            Update();

        float organelleAngle = Mathf.Atan2(y, x);

        Vector2 closestSoFar = new Vector2(0, 0);
        float angleToClosest = Mathf.Pi * 2;

        foreach (var vertex in vertices2D)
        {
            if (Mathf.Abs(Mathf.Atan2(vertex.y, vertex.x) - organelleAngle) <
                angleToClosest)
            {
                closestSoFar = new Vector2(vertex.x, vertex.y);
                angleToClosest =
                    Mathf.Abs(Mathf.Atan2(vertex.y, vertex.x) - organelleAngle);
            }
        }

        return new Vector3(closestSoFar.x, 0, closestSoFar.y);
    }

    public bool MatchesCacheParameters(ICacheableData cacheData)
    {
        if (cacheData is IComputedMembraneData data)
            return this.MembraneDataFieldsEqual(data);

        return false;
    }

    public long ComputeCacheHash()
    {
        return this.ComputeMembraneDataHash();
    }

    /// <summary>
    ///   First generates the 2D vertices and then builds the 3D mesh
    /// </summary>
    private void InitializeMesh()
    {
        // First try to get from cache as it's very expensive to generate the membrane
        var cached = this.FetchDataFromCache(ProceduralDataCache.Instance.ReadMembraneData);

        if (cached != null)
        {
            CopyMeshFromCache(cached);
            return;
        }

        // The length in pixels (probably not accurate?) of a side of the square that bounds the membrane.
        // Half the side length of the original square that is compressed to make the membrane.
        int cellDimensions = 10;

        foreach (var pos in OrganellePositions)
        {
            if (Mathf.Abs(pos.x) + 1 > cellDimensions)
            {
                cellDimensions = (int)Mathf.Abs(pos.x) + 1;
            }

            if (Mathf.Abs(pos.y) + 1 > cellDimensions)
            {
                cellDimensions = (int)Mathf.Abs(pos.y) + 1;
            }
        }

        // Make the length longer to guarantee that everything fits easily inside the square
        cellDimensions *= 100;

        startingBuffer.Clear();

        // Integer divides are intentional here
        // ReSharper disable PossibleLossOfFraction

        for (int i = membraneResolution; i > 0; i--)
        {
            startingBuffer.Add(new Vector2(-cellDimensions,
                cellDimensions - 2 * cellDimensions / membraneResolution * i));
        }

        for (int i = membraneResolution; i > 0; i--)
        {
            startingBuffer.Add(new Vector2(
                cellDimensions - 2 * cellDimensions / membraneResolution * i,
                cellDimensions));
        }

        for (int i = membraneResolution; i > 0; i--)
        {
            startingBuffer.Add(new Vector2(cellDimensions,
                -cellDimensions + 2 * cellDimensions / membraneResolution * i));
        }

        for (int i = membraneResolution; i > 0; i--)
        {
            startingBuffer.Add(new Vector2(
                -cellDimensions + 2 * cellDimensions / membraneResolution * i,
                -cellDimensions));
        }

        // ReSharper restore PossibleLossOfFraction

        // Get new membrane points for vertices2D
        GenerateMembranePoints(startingBuffer, vertices2D);

        BuildMesh();
    }

    private int InitializeCorrectMembrane(int writeIndex, Vector3[] vertices,
        Vector2[] uvs)
    {
        // common variables
        float height = 0.1f;
        float multiplier = 2.0f * Mathf.Pi;
        var center = new Vector2(0.5f, 0.5f);

        // cell walls need obvious inner/outer membranes (we can worry
        // about chitin later)
        if (Type.CellWall)
        {
            height = 0.05f;
        }

        vertices[writeIndex] = new Vector3(0, height / 2, 0);
        uvs[writeIndex] = center;
        ++writeIndex;

        for (int i = 0, end = vertices2D.Count; i < end + 1; i++)
        {
            // Finds the UV coordinates be projecting onto a plane and
            // stretching to fit a circle.

            float currentRadians = multiplier * i / end;

            vertices[writeIndex] = new Vector3(vertices2D[i % end].x, height / 2,
                vertices2D[i % end].y);

            uvs[writeIndex] = center +
                new Vector2(Mathf.Cos(currentRadians), Mathf.Sin(currentRadians)) / 2;

            ++writeIndex;
        }

        return writeIndex;
    }

    /// <summary>
    ///   Updates things and marks as not dirty
    /// </summary>
    private void Update()
    {
        Dirty = false;
        InitializeMesh();
        ApplyAllMaterialParameters();
    }

    /// <summary>
    ///   Return the position of the closest organelle hex to the target point.
    /// </summary>
    private Vector2 FindClosestOrganelleHex(Vector2 target)
    {
        float closestDistanceSoFar = float.MaxValue;
        Vector2 closest = new Vector2(0.0f, 0.0f);

        foreach (var pos in OrganellePositions)
        {
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
    ///   Cheaper version of contains for absorbing stuff.Calculates a
    ///   circle radius that contains all the points (when it is
    ///   placed at 0,0 local coordinate).
    /// </summary>
    private float CalculateEncompassingCircleRadius()
    {
        if (Dirty)
            Update();

        float distanceSquared = 0;

        foreach (var vertex in vertices2D)
        {
            var currentDistance = vertex.LengthSquared();
            if (currentDistance > distanceSquared)
                distanceSquared = currentDistance;
        }

        return Mathf.Sqrt(distanceSquared);
    }

    private void ApplyAllMaterialParameters()
    {
        ApplyWiggly();
        ApplyMovementWiggly();
        ApplyHealth();
        ApplyTint();
        ApplyTextures();
        ApplyDissolveEffect();
    }

    private void ApplyWiggly()
    {
        if (MaterialToEdit == null)
            return;

        // Don't apply wigglyness too early if this is dirty as getting the circle radius forces membrane position
        // calculation, which we don't want to do twice when initializing a microbe
        if (Dirty)
            return;

        float wigglyNessToApply =
            WigglyNess / (EncompassingCircleRadius * sizeWigglyNessDampeningFactor);

        MaterialToEdit.SetShaderParam("wigglyNess", Mathf.Min(WigglyNess, wigglyNessToApply));
    }

    private void ApplyMovementWiggly()
    {
        if (MaterialToEdit == null)
            return;

        // See comment in ApplyWiggly
        if (Dirty)
            return;

        float wigglyNessToApply =
            MovementWigglyNess / (EncompassingCircleRadius * sizeMovementWigglyNessDampeningFactor);

        MaterialToEdit.SetShaderParam("movementWigglyNess", Mathf.Min(MovementWigglyNess, wigglyNessToApply));
    }

    private void ApplyHealth()
    {
        MaterialToEdit?.SetShaderParam("healthFraction", HealthFraction);
    }

    private void ApplyTint()
    {
        MaterialToEdit?.SetShaderParam("tint", Tint);
    }

    private void ApplyTextures()
    {
        // We must update the texture on already-existing membranes, due to the membrane texture changing
        // for the player microbe.
        if (albedoTexture != null && currentlyLoadedAlbedoTexture == Type.AlbedoTexture)
            return;

        albedoTexture = Type.LoadedAlbedoTexture;

        MaterialToEdit!.SetShaderParam("albedoTexture", albedoTexture);
        MaterialToEdit.SetShaderParam("normalTexture", Type.LoadedNormalTexture);
        MaterialToEdit.SetShaderParam("damagedTexture", Type.LoadedDamagedTexture);
        MaterialToEdit.SetShaderParam("dissolveTexture", noiseTexture);

        currentlyLoadedAlbedoTexture = Type.AlbedoTexture;
    }

    private void ApplyDissolveEffect()
    {
        MaterialToEdit?.SetShaderParam("dissolveValue", DissolveEffectValue);
    }

    private void CopyMeshFromCache(ComputedMembraneData cached)
    {
        // TODO: check if it would be better for us to just keep readonly data in the membrane cache so we could
        // just copy a reference here
        vertices2D.Clear();
        vertices2D.AddRange(cached.Vertices2D);

        // Apply the mesh to us
        Mesh = cached.GeneratedMesh;
        SetSurfaceMaterial(cached.SurfaceIndex, MaterialToEdit);
    }

    /// <summary>
    ///   Creates the actual mesh object. Call InitializeMesh instead of this directly
    /// </summary>
    private void BuildMesh()
    {
        // This is actually a triangle list, but the index buffer is used to build
        // the indices (to emulate a triangle fan)
        var bufferSize = vertices2D.Count + 2;
        var indexSize = vertices2D.Count * 3;

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
        writeIndex = InitializeCorrectMembrane(writeIndex, vertices, uvs);

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
        arrays[(int)Mesh.ArrayType.TexUv] = uvs;

        // Create the mesh
        var generatedMesh = new ArrayMesh();

        var surfaceIndex = generatedMesh.GetSurfaceCount();
        generatedMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        // Apply the mesh to us
        Mesh = generatedMesh;
        SetSurfaceMaterial(surfaceIndex, MaterialToEdit);

        ProceduralDataCache.Instance.WriteMembraneData(CreateDataForCache(generatedMesh, surfaceIndex));
    }

    private void GenerateMembranePoints(List<Vector2> sourceBuffer, List<Vector2> targetBuffer)
    {
        // Move all the points in the source buffer close to organelles
        // This operation used to be iterative but this is now a much faster version that moves things all the way in
        // a single step
        for (int i = 0, end = sourceBuffer.Count; i < end; ++i)
        {
            var closestOrganelle = FindClosestOrganelleHex(sourceBuffer[i]);

            var direction = (sourceBuffer[i] - closestOrganelle).Normalized();
            var movement = direction * Constants.MEMBRANE_ROOM_FOR_ORGANELLES;

            sourceBuffer[i] = closestOrganelle + movement;
        }

        float circumference = 0.0f;

        for (int i = 0, end = sourceBuffer.Count; i < end; ++i)
        {
            circumference += (sourceBuffer[(i + 1) % end] - sourceBuffer[i]).Length();
        }

        targetBuffer.Clear();

        var lastAddedPoint = sourceBuffer[0];

        targetBuffer.Add(lastAddedPoint);

        float gap = circumference / sourceBuffer.Count;
        float distanceToLastAddedPoint = 0.0f;
        float distanceToLastPassedPoint = 0.0f;

        // Go around the membrane and place points evenly in the target buffer.
        for (int i = 0, end = sourceBuffer.Count; i < end; ++i)
        {
            var currentPoint = sourceBuffer[i];
            var nextPoint = sourceBuffer[(i + 1) % end];
            float distance = (nextPoint - currentPoint).Length();

            // Add a new point if the next point is too far
            if (distance + distanceToLastAddedPoint - distanceToLastPassedPoint > gap)
            {
                var direction = (nextPoint - currentPoint).Normalized();
                var movement = direction * (gap - distanceToLastAddedPoint + distanceToLastPassedPoint);

                lastAddedPoint = currentPoint + movement;

                targetBuffer.Add(lastAddedPoint);

                if (targetBuffer.Count >= end)
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

        float waveFrequency = 2.0f * Mathf.Pi * Constants.MEMBRANE_NUMBER_OF_WAVES / targetBuffer.Count;

        float heightMultiplier = Type.CellWall ?
            Constants.MEMBRANE_WAVE_HEIGHT_MULTIPLIER_CELL_WALL :
            Constants.MEMBRANE_WAVE_HEIGHT_MULTIPLIER;

        float waveHeight = Mathf.Pow(circumference, Constants.MEMBRANE_WAVE_HEIGHT_DEPENDENCE_ON_SIZE)
            * heightMultiplier;

        // Make the membrane wavier
        for (int i = 0, end = targetBuffer.Count; i < end; ++i)
        {
            var point = targetBuffer[i];
            var nextPoint = targetBuffer[(i + 1) % end];
            var direction = (nextPoint - point).Normalized();

            // Turn 90 degrees
            direction = new Vector2(-direction.y, direction.x);

            var movement = direction * Mathf.Sin(waveFrequency * i) * waveHeight;

            targetBuffer[i] = point + movement;
        }
    }

    private ComputedMembraneData CreateDataForCache(ArrayMesh mesh, int surfaceIndex)
    {
        // Need to copy our data here when caching it as if we get new organelles and change we would pollute the
        // cache entry
        return new ComputedMembraneData(OrganellePositions, Type, new List<Vector2>(vertices2D), mesh, surfaceIndex);
    }
}
