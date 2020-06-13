using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Membrane for microbes
/// </summary>
public class Membrane : MeshInstance
{
    /// <summary>
    ///   This must be big enough that no organelle can be at this position
    /// </summary>
    public const float INVALID_FOUND_ORGANELLE = -999999.0f;

    [Export]
    public ShaderMaterial MaterialToEdit;

    public MembraneType Type;

    // private ArrayMesh generatedMesh;

    private float healthFraction = 1.0f;
    private float wigglyNess = 1.0f;
    private float sizeWigglyNessDampeningFactor = 0.22f;
    private float movementWigglyNess = 1.0f;
    private float sizeMovementWigglyNessDampeningFactor = 0.22f;
    private Color tint = new Color(1, 1, 1, 1);
    private float dissolveEffectValue = 0.0f;

    private Texture normalTexture;
    private Texture damagedTexture;
    private Texture noiseTexture;

    private string currentlyLoadedNormalTexture;

    private bool dirty = true;
    private bool radiusIsDirty = true;
    private float cachedRadius;

    /// <summary>
    ///   The length in pixels of a side of the square that bounds the
    ///   membrane. Half the side length of the original square that
    ///   is compressed to make the membrane.
    /// </summary>
    private int cellDimensions = 10;

    /// <summary>
    ///   Amount of segments on one side of the above described
    ///   square. The amount of points on the side of the membrane.
    /// </summary>
    private int membraneResolution = Constants.MEMBRANE_RESOLUTION;

    /// <summary>
    ///   Stores the generated 2-Dimensional membrane. Needed for contains calculations
    /// </summary>
    private List<Vector2> vertices2D;

    /// <summary>
    ///   When true the mesh needs to be regenerated and material properties applied
    /// </summary>
    public bool Dirty
    {
        get
        {
            return dirty;
        }
        set
        {
            if (value)
                radiusIsDirty = true;
            dirty = value;
        }
    }

    /// <summary>
    ///   Organelle positions of the microbe, needs to be set for the membrane to appear
    /// </summary>
    public List<Vector2> OrganellePositions { get; set; }

    /// <summary>
    ///   How healthy the cell is, mixes in a damaged texture. Range 0.0f - 1.0f
    /// </summary>
    public float HealthFraction
    {
        get
        {
            return healthFraction;
        }
        set
        {
            value = value.Clamp(0.0f, 1.0f);
            if (value == HealthFraction)
                return;

            healthFraction = value;
            if (MaterialToEdit != null)
                ApplyHealth();
        }
    }

    /// <summary>
    ///   How much the membrane wiggles. Used values are 0 and 1
    /// </summary>
    public float WigglyNess
    {
        get
        {
            return wigglyNess;
        }
        set
        {
            wigglyNess = value;
            if (MaterialToEdit != null)
                ApplyWiggly();
        }
    }

    public float MovementWigglyNess
    {
        get
        {
            return movementWigglyNess;
        }
        set
        {
            movementWigglyNess = value;
            if (MaterialToEdit != null)
                ApplyMovementWiggly();
        }
    }

    public Color Tint
    {
        get
        {
            return tint;
        }
        set
        {
            // Desaturate it here so it looks nicer (could implement as method that
            // could be called i suppose)

            // According to stack overflow HSV and HSB are the same thing
            value.ToHsv(out var hue, out var saturation, out var brightness);

            value = Color.FromHsv(hue, saturation * 0.75f, brightness);

            if (tint == value)
                return;

            tint = value;

            // If we already have created a material we need to re-apply it
            if (MaterialToEdit != null)
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
        get
        {
            return dissolveEffectValue;
        }
        set
        {
            dissolveEffectValue = value;
            if (MaterialToEdit != null)
                ApplyDissolveEffect();
        }
    }

    public override void _Ready()
    {
        if (Type == null)
            Type = SimulationParameters.Instance.GetMembrane("single");

        if (MaterialToEdit == null)
            GD.PrintErr("MaterialToEdit on Membrane is not set");

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
    ///   Finds the position of external organelles based on its "internal" location.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The returned Vector is in world coordinates (x, 0, z) and
    ///     not in internal membrane coordinates (x, y, 0). This is so
    ///     that gameplay code doesn't have to do the conversion
    ///     everywhere this is used.
    ///   </para>
    /// </remarks>
    public Vector3 GetExternalOrganelle(float x, float y)
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

    /// <summary>
    ///   Return the position of the closest organelle to the target
    ///   point if it is less then a certain threshold away.
    /// </summary>
    public Vector2 FindClosestOrganelles(Vector2 target)
    {
        // The distance we want the membrane to be from the organelles squared.
        float closestSoFar = 4;
        Vector2 closest = new Vector2(INVALID_FOUND_ORGANELLE, INVALID_FOUND_ORGANELLE);

        foreach (var pos in OrganellePositions)
        {
            float lenToObject = (target - pos).LengthSquared();

            if (lenToObject < 4 && lenToObject < closestSoFar)
            {
                closestSoFar = lenToObject;
                closest = pos;
            }
        }

        return closest;
    }

    /// <summary>
    ///   Decides where the point needs to move based on the position of the closest organelle.
    /// </summary>
    private static Vector2 GetMovement(Vector2 target, Vector2 closestOrganelle)
    {
        float power = Mathf.Pow(2.7f, -(target - closestOrganelle).Length() / 10) / 50;

        return (closestOrganelle - target) * power;
    }

    private static Vector2 GetMovementForCellWall(Vector2 target, Vector2 closestOrganelle)
    {
        float power = Mathf.Pow(10.0f, -(target - closestOrganelle).Length()) / 50;

        return (closestOrganelle - target) * power;
    }

    // Vector2 GetMovementForCellWall(Vector2 target, Vector2 closestOrganelle);

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

    /// <summary>
    ///   Updates things and marks as not dirty
    /// </summary>
    private void Update()
    {
        Dirty = false;
        InitializeMesh();
        ApplyAllMaterialParameters();
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
        float wigglyNessToApply =
            WigglyNess / (EncompassingCircleRadius * sizeWigglyNessDampeningFactor);

        MaterialToEdit.SetShaderParam("wigglyNess", Mathf.Min(WigglyNess, wigglyNessToApply));
    }

    private void ApplyMovementWiggly()
    {
        float wigglyNessToApply =
            MovementWigglyNess / (EncompassingCircleRadius * sizeMovementWigglyNessDampeningFactor);

        MaterialToEdit.SetShaderParam("movementWigglyNess", Mathf.Min(MovementWigglyNess, wigglyNessToApply));
    }

    private void ApplyHealth()
    {
        MaterialToEdit.SetShaderParam("healthFraction", HealthFraction);
    }

    private void ApplyTint()
    {
        MaterialToEdit.SetShaderParam("tint", Tint);
    }

    private void ApplyTextures()
    {
        // We must update the texture on already-existing membranes,
        // due to the membrane texture changing for the player microbe.
        if (normalTexture != null && currentlyLoadedNormalTexture == Type.NormalTexture)
            return;

        normalTexture = Type.LoadedNormalTexture;
        damagedTexture = Type.LoadedDamagedTexture;

        noiseTexture = GD.Load<Texture>("res://assets/textures/dissolve_noise.tres");

        MaterialToEdit.SetShaderParam("albedoTexture", normalTexture);
        MaterialToEdit.SetShaderParam("damagedTexture", damagedTexture);
        MaterialToEdit.SetShaderParam("dissolveTexture", noiseTexture);

        currentlyLoadedNormalTexture = Type.NormalTexture;
    }

    private void ApplyDissolveEffect()
    {
        MaterialToEdit.SetShaderParam("dissolveValue", DissolveEffectValue);
    }

    /// <summary>
    ///   First generates the 2D vertices and then builds the 3D mesh
    /// </summary>
    private void InitializeMesh()
    {
        // For preview scenes, add just one organelle
        if (OrganellePositions == null)
        {
            OrganellePositions = new List<Vector2>() { new Vector2(0, 0) };
        }

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

        vertices2D = new List<Vector2>();

        for (int i = membraneResolution; i > 0; i--)
        {
            vertices2D.Add(new Vector2(-cellDimensions,
                cellDimensions - 2 * cellDimensions / membraneResolution * i));
        }

        for (int i = membraneResolution; i > 0; i--)
        {
            vertices2D.Add(new Vector2(
                cellDimensions - 2 * cellDimensions / membraneResolution * i,
                cellDimensions));
        }

        for (int i = membraneResolution; i > 0; i--)
        {
            vertices2D.Add(new Vector2(cellDimensions,
                -cellDimensions + 2 * cellDimensions / membraneResolution * i));
        }

        for (int i = membraneResolution; i > 0; i--)
        {
            vertices2D.Add(new Vector2(
                -cellDimensions + 2 * cellDimensions / membraneResolution * i,
                -cellDimensions));
        }

        // This needs to actually run a bunch of times as the points
        // moving towards the organelles is iterative. Right now this
        // wastes a bunch of allocations by reallocating a second list
        // each function call.
        for (int i = 0; i < 40 * cellDimensions; i++)
        {
            DrawCorrectMembrane();
        }

        BuildMesh();
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

        var arrays = new Godot.Collections.Array();
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
    }

    private int InitializeCorrectMembrane(int writeIndex, Vector3[] vertices,
        Vector2[] uvs)
    {
        // common variables
        float height = 0.1f;
        float multiplier = 2.0f * Mathf.Pi;
        var center = new Vector2(0.5f, 0.5f);

        movementWigglyNess = Type.MovementWigglyness;

        // cell walls need obvious inner/outer membranes (we can worry
        // about chitin later)
        if (Type.CellWall)
        {
            height = 0.05f;
            multiplier = Mathf.Pi;
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

    private void DrawCorrectMembrane()
    {
        if (Type.CellWall)
        {
            DrawMembrane(GetMovementForCellWall);
        }
        else
        {
            DrawMembrane(GetMovement);
        }
    }

    private void DrawMembrane(Func<Vector2, Vector2, Vector2> movementFunc)
    {
        // Stores the temporary positions of the membrane.
        var newPositions = new List<Vector2>(vertices2D);

        // Loops through all the points in the membrane and relocates them as
        // necessary.
        for (int i = 0, end = newPositions.Count; i < end; i++)
        {
            var closestOrganelle = FindClosestOrganelles(vertices2D[i]);
            if (closestOrganelle ==
                new Vector2(INVALID_FOUND_ORGANELLE, INVALID_FOUND_ORGANELLE))
            {
                newPositions[i] =
                    (vertices2D[(end + i - 1) % end] + vertices2D[(i + 1) % end]) /
                    2;
            }
            else
            {
                var movementDirection = movementFunc(vertices2D[i], closestOrganelle);

                newPositions[i] = new Vector2(newPositions[i].x - movementDirection.x,
                    newPositions[i].y - movementDirection.y);
            }
        }

        // Allows for the addition and deletion of points in the membrane.
        for (int i = 0; i < newPositions.Count - 1; i++)
        {
            // Check to see if the gap between two points in the membrane is too
            // big.
            if ((newPositions[i] - newPositions[(i + 1) % newPositions.Count])
                .Length() > (float)cellDimensions / membraneResolution)
            {
                // Add an element after the ith term that is the average of the
                // i and i+1 term.
                var tempPoint =
                    (newPositions[(i + 1) % newPositions.Count] +
                        newPositions[i]) /
                    2;

                newPositions.Insert(i + 1, tempPoint);
                i++;
            }

            // Check to see if the gap between two points in the membrane is too
            // small.
            if ((newPositions[(i + 1) % newPositions.Count] -
                    newPositions[(i + newPositions.Count - 1) % newPositions.Count])
                .Length() < (float)cellDimensions / membraneResolution)
            {
                // Delete the ith term.
                newPositions.RemoveAt(i);
            }
        }

        // Replace the old vertex data with the new data. This may be
        // faster than copying the data back
        vertices2D = newPositions;
    }
}
