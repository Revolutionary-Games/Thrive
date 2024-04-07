using System;
using Godot;

/// <summary>
///   Membrane for microbes
/// </summary>
public partial class Membrane : MeshInstance3D
{
    // It used to be the case that membrane could be previewed in the Godot editor, if anyone is still interested in
    // that feature, please reimplement it

#pragma warning disable CA2213
    [Export]
    public ShaderMaterial? MembraneShaderMaterial;

    [Export]
    public ShaderMaterial? EngulfShaderMaterial;

    private readonly StringName healthParameterName = new("healthFraction");
    private readonly StringName wigglynessParameterName = new("wigglyNess");
    private readonly StringName movementWigglynessParameterName = new("movementWigglyNess");
    private readonly StringName fadeParameterName = new("fade");

    [Export]
    private MeshInstance3D engulfAnimationMeshInstance = null!;
#pragma warning disable CA2213
    private Texture2D? albedoTexture;

    /// <summary>
    ///   Shared cache data about the calculated points for this membrane. This is not disposed as the cache manager
    ///   handles doing that.
    /// </summary>
    private MembranePointData membraneData = null!;
#pragma warning restore CA2213

    private string? currentlyLoadedAlbedoTexture;

    private float healthFraction = 1.0f;
    private float wigglyNess = 1.0f;
    private float sizeWigglyNessDampeningFactor = 0.22f;
    private float movementWigglyNess = 1.0f;
    private float sizeMovementWigglyNessDampeningFactor = 0.32f;
    private double engulfFade = 1.0f;

    /// <summary>
    ///   When true the material properties need to be reapplied
    /// </summary>
    public bool Dirty { get; set; } = true;

    /// <summary>
    ///   This is true when a new <see cref="MembraneData"/> instance is being generated but it is not ready yet.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is automatically reset when the next time <see cref="MembraneData"/> is set
    ///   </para>
    /// </remarks>
    public bool IsChangingShape { get; set; }

    /// <summary>
    ///   Generated membrane point data. Must be set when instances of this class is created before anything is allowed
    ///   to be done with this.
    /// </summary>
    public MembranePointData MembraneData
    {
        get => membraneData;
        set
        {
            // Cache returns the same instance, so skip doing anything here if we got applied the same data again as we
            // already had
            if (ReferenceEquals(membraneData, value))
                return;

            bool reapply = membraneData != null!;

            membraneData = value;
            IsChangingShape = false;

            // This needs to be marked dirty purely to support swapping membrane types, other shader parameters would
            // just happily keep working when the material is applied to the new mesh
            Dirty = true;

            if (reapply)
                SetMesh();
        }
    }

    public MembraneType Type => membraneData.Type;

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

            // Health is a special case as this is applied so often compared to the other properties that this has an
            // apply shortcut to reduce processing
            if (MembraneShaderMaterial == null)
            {
                Dirty = true;
            }
            else
            {
                ApplyHealth();
            }
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
            Dirty = true;
        }
    }

    public float MovementWigglyNess
    {
        get => movementWigglyNess;
        set
        {
            movementWigglyNess = Mathf.Clamp(value, 0.0f, 1.0f);
            Dirty = true;
        }
    }

    /// <summary>
    ///   Quick radius value for the membrane size
    /// </summary>
    public float EncompassingCircleRadius => membraneData.Radius;

    public static Color MembraneTintFromSpeciesColour(Color color)
    {
        // Desaturate it here so it looks nicer (could implement as method that
        // could be called i suppose)

        // According to stack overflow HSV and HSB are the same thing
        color.ToHsv(out var hue, out var saturation, out var brightness);

        return Color.FromHsv(hue, saturation * 0.75f, brightness,
            Mathf.Clamp(color.A, 0.4f - brightness * 0.3f, 1.0f));
    }

    public override void _Ready()
    {
        if (membraneData == null!)
            throw new InvalidOperationException("Membrane was not property initialized with membrane data");

        if (MembraneShaderMaterial == null)
            throw new Exception("MembraneShaderMaterial on Membrane is not set");

        if (EngulfShaderMaterial == null)
            throw new Exception("EngulfShaderMaterial on Membrane is not set");

        SetMesh();
    }

    public override void _Process(double delta)
    {
        if (!Dirty)
            return;

        Dirty = false;
        ApplyAllMaterialParameters();
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

        int n = membraneData.VertexCount;
        var vertices = membraneData.Vertices2D;

        for (int i = 0; i < n - 1; i++)
        {
            if ((vertices[i].Y <= y && y < vertices[i + 1].Y) ||
                (vertices[i + 1].Y <= y && y < vertices[i].Y))
            {
                if (x < (vertices[i + 1].X - vertices[i].X) *
                    (y - vertices[i].Y) /
                    (vertices[i + 1].Y - vertices[i].Y) +
                    vertices[i].X)
                {
                    crosses = !crosses;
                }
            }
        }

        return crosses;
    }

    public void EnableEngulfAnimation(bool enable, double delta)
    {
        if (enable && engulfFade < 1)
        {
            engulfFade += delta / 0.5;
            engulfFade = Math.Min(engulfFade, 1);
        }
        else if (!enable && engulfFade > 0)
        {
            engulfFade -= delta / 0.5;
            engulfFade = Math.Max(engulfFade, 0);
        }

        if (engulfFade != 0)
        {
            EngulfShaderMaterial?.SetShaderParameter(fadeParameterName, engulfFade);
            engulfAnimationMeshInstance.Visible = true;
        }
        else
        {
            // Turning of visability when fade is 0 stops the shader from
            // being run when it can't be seen anyway.
            engulfAnimationMeshInstance.Visible = false;
        }
    }

    /// <summary>
    ///   Finds the point on the membrane nearest to the given point.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Used for finding out where to put an external organelle.
    ///   </para>
    ///   <para>
    ///     The returned Vector is in world coordinates (x, 0, z) and not in internal membrane coordinates (x, y, 0).
    ///     This is so that gameplay code doesn't have to do the conversion everywhere this is used.
    ///   </para>
    /// </remarks>
    public Vector3 GetVectorTowardsNearestPointOfMembrane(float x, float y)
    {
        float organelleAngle = Mathf.Atan2(y, x);

        Vector2 closestSoFar = new Vector2(0, 0);
        float angleToClosest = Mathf.Pi * 2;

        int count = membraneData.VertexCount;
        var vertices = membraneData.Vertices2D;

        for (int i = 0; i < count; ++i)
        {
            var vertex = vertices[i];
            if (Mathf.Abs(Mathf.Atan2(vertex.Y, vertex.X) - organelleAngle) < angleToClosest)
            {
                closestSoFar = new Vector2(vertex.X, vertex.Y);
                angleToClosest = Mathf.Abs(Mathf.Atan2(vertex.Y, vertex.X) - organelleAngle);
            }
        }

        return new Vector3(closestSoFar.X, 0, closestSoFar.Y);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            healthParameterName.Dispose();
            wigglynessParameterName.Dispose();
            movementWigglynessParameterName.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Applies the mesh to us from the shared cache data (first membrane to apply the mesh causes the mesh to be
    ///   created)
    /// </summary>
    private void SetMesh()
    {
        Mesh = membraneData.GeneratedMesh;
        MaterialOverride = MembraneShaderMaterial;

        engulfAnimationMeshInstance.Mesh = membraneData.GeneratedEngulfMesh;
        engulfAnimationMeshInstance.MaterialOverride = EngulfShaderMaterial;
    }

    private void ApplyAllMaterialParameters()
    {
        ApplyWiggly();
        ApplyMovementWiggly();
        ApplyHealth();
        ApplyTextures();
    }

    private void ApplyWiggly()
    {
        if (MembraneShaderMaterial == null || EngulfShaderMaterial == null)
            return;

        float wigglyNessToApply =
            WigglyNess / (EncompassingCircleRadius * sizeWigglyNessDampeningFactor);

        MembraneShaderMaterial.SetShaderParameter(wigglynessParameterName,
            Mathf.Min(WigglyNess, wigglyNessToApply));

        EngulfShaderMaterial.SetShaderParameter(wigglynessParameterName,
            Mathf.Min(WigglyNess, wigglyNessToApply));
    }

    private void ApplyMovementWiggly()
    {
        if (MembraneShaderMaterial == null || EngulfShaderMaterial == null)
            return;

        float wigglyNessToApply =
            MovementWigglyNess / (EncompassingCircleRadius * sizeMovementWigglyNessDampeningFactor);

        MembraneShaderMaterial.SetShaderParameter(movementWigglynessParameterName,
            Mathf.Min(MovementWigglyNess, wigglyNessToApply));

        EngulfShaderMaterial.SetShaderParameter(movementWigglynessParameterName,
            Mathf.Min(MovementWigglyNess, wigglyNessToApply));
    }

    private void ApplyHealth()
    {
        MembraneShaderMaterial?.SetShaderParameter(healthParameterName, HealthFraction);
    }

    private void ApplyTextures()
    {
        // We must update the texture on already-existing membranes, due to the membrane texture changing
        // for the player microbe (thanks to edits made in the cell editor).
        if (albedoTexture != null && currentlyLoadedAlbedoTexture == Type.AlbedoTexture)
            return;

        albedoTexture = Type.LoadedAlbedoTexture;

        MembraneShaderMaterial!.SetShaderParameter("albedoTexture", albedoTexture);
        MembraneShaderMaterial.SetShaderParameter("normalTexture", Type.LoadedNormalTexture);
        MembraneShaderMaterial.SetShaderParameter("damagedTexture", Type.LoadedDamagedTexture);

        currentlyLoadedAlbedoTexture = Type.AlbedoTexture;
    }
}
