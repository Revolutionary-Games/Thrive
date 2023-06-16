using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Script for the floating chunks (cell parts, rocks, hazards)
/// </summary>
[JSONAlwaysDynamicType]
public class FloatingChunk : SimulatedPhysicsEntity, ISimulatedEntityWithDirectVisuals, ISpawned,
    IEngulfable /*, IInspectableEntity*/
{
#pragma warning disable CA2213 // a shared resource from the chunk definition
    [JsonProperty]
    public PackedScene GraphicsScene = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   If this is null, a sphere shape is used as a default for collision detections.
    /// </summary>
    [Export]
    [JsonProperty]
    public ConvexPolygonShape? ConvexPhysicsMesh;

    /// <summary>
    ///   The node path to the mesh of this chunk
    /// </summary>
    public string? ModelNodePath;

    /// <summary>
    ///   The node path to the animation of this chunk
    /// </summary>
    public string? AnimationPath;

    /// <summary>
    ///   Used to check if a microbe wants to engulf this
    /// </summary>
    private HashSet<Microbe> touchingMicrobes = new();

#pragma warning disable CA2213
    private MeshInstance? chunkMesh;
    private Particles? particles;
#pragma warning restore CA2213

    [JsonProperty]
    private bool isDissolving;

    [JsonProperty]
    private bool isFadingParticles;

    [JsonProperty]
    private float particleFadeTimer;

    [JsonProperty]
    private float dissolveEffectValue;

    [JsonProperty]
    private float elapsedSinceProcess;

    [JsonProperty]
    private int renderPriority;

    [JsonProperty]
    private float engulfSize;

    public int DespawnRadiusSquared { get; set; }

    [JsonIgnore]
    public bool DisallowDespawning => false;

    [JsonIgnore]
    public float EntityWeight => 1000.0f;

    [JsonIgnore]
    public GeometryInstance EntityGraphics
    {
        get
        {
            if (chunkMesh != null)
                return chunkMesh;

            if (particles != null)
                return particles;

            throw new InstanceNotLoadedYetException();
        }
    }

    [JsonIgnore]
    public Spatial VisualNode { get; private set; } = new();

    [JsonIgnore]
    public int RenderPriority
    {
        get => renderPriority;
        set
        {
            renderPriority = value;
            ApplyRenderPriority();
        }
    }

    /// <summary>
    ///   Determines how big this chunk is for engulfing calculations. Set to &lt;= 0 to disable
    /// </summary>
    [JsonIgnore]
    public float EngulfSize
    {
        get => engulfSize * (1 - DigestedAmount);
        set => engulfSize = value;
    }

    /// <summary>
    ///   Compounds this chunk contains, and vents
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Capacity is set to 0 so that no compounds can be added the normal way to the chunk.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public CompoundBag Compounds { get; private set; } = new(0.0f);

    /// <summary>
    ///   How much of each compound is vented per second
    /// </summary>
    public float VentPerSecond { get; set; } = 5.0f;

    /// <summary>
    ///   If true this chunk is destroyed when all compounds are vented
    /// </summary>
    public bool Dissolves { get; set; }

    /// <summary>
    ///   If > 0 applies damage to a cell on touch
    /// </summary>
    public float Damages { get; set; }

    /// <summary>
    ///   When true, the chunk will despawn when the despawn timer finishes
    /// </summary>
    public bool UsesDespawnTimer { get; set; }

    /// <summary>
    ///   How much time has passed since a chunk that uses this timer has been spawned
    /// </summary>
    [JsonProperty]
    public float DespawnTimer { get; private set; }

    /// <summary>
    ///   If true this gets deleted when a cell touches this
    /// </summary>
    public bool DeleteOnTouch { get; set; }

    public float Radius { get; set; }

    public float ChunkScale { get; set; }

    /// <summary>
    ///   The name of the kind of damage type this chunk inflicts. Default is "chunk".
    /// </summary>
    public string DamageType { get; set; } = "chunk";

    public string ChunkName { get; set; } = string.Empty;

    public bool EasterEgg { get; set; }

    [JsonProperty]
    public PhagocytosisPhase PhagocytosisStep { get; set; }

    [JsonProperty]
    public EntityReference<Microbe> HostileEngulfer { get; private set; } = new();

    [JsonProperty]
    public Enzyme? RequisiteEnzymeToDigest { get; private set; }

    /// <summary>
    ///   This is both the digestion and dissolve effect progress value for now.
    /// </summary>
    [JsonIgnore]
    public float DigestedAmount
    {
        get => dissolveEffectValue;
        set
        {
            dissolveEffectValue = Mathf.Clamp(value, 0.0f, 1.0f);
            UpdateDissolveEffect();
        }
    }

    [JsonIgnore]
    public string ReadableName => TranslationServer.Translate(ChunkName);

    public override void OnAddedToSimulation(IWorldSimulation simulation)
    {
        base.OnAddedToSimulation(simulation);

        InitGraphics();

        if (chunkMesh == null && particles == null)
            throw new InvalidOperationException("Can't make a chunk without graphics scene");

        InitPhysics();
    }

    /// <summary>
    ///   Grabs data from the type to initialize this
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Doesn't initialize the graphics scene which needs to be set separately
    ///   </para>
    /// </remarks>
    public void Init(ChunkConfiguration chunkType, string? modelPath, string? animationPath)
    {
        // Grab data
        ChunkName = chunkType.Name;
        VentPerSecond = chunkType.VentAmount;
        Dissolves = chunkType.Dissolves;
        EngulfSize = chunkType.Size;
        Damages = chunkType.Damages;
        DeleteOnTouch = chunkType.DeleteOnTouch;
        DamageType = string.IsNullOrEmpty(chunkType.DamageType) ? "chunk" : chunkType.DamageType;
        EasterEgg = chunkType.EasterEgg;

        // TODO: proper density values from the JSON
        Density = chunkType.Mass * 1000;

        // These are stored for saves to work
        Radius = chunkType.Radius;
        ChunkScale = chunkType.ChunkScale;

        ModelNodePath = modelPath;
        AnimationPath = animationPath;

        // Copy compounds to vent
        if (chunkType.Compounds?.Count > 0)
        {
            foreach (var entry in chunkType.Compounds)
            {
                Compounds.Compounds.Add(entry.Key, entry.Value.Amount);
            }
        }

        if (!string.IsNullOrEmpty(chunkType.DissolverEnzyme))
            RequisiteEnzymeToDigest = SimulationParameters.Instance.GetEnzyme(chunkType.DissolverEnzyme);
    }

    /// <summary>
    ///   Reverses the action of Init back to a ChunkConfiguration
    /// </summary>
    /// <returns>The reversed chunk configuration</returns>
    public ChunkConfiguration CreateChunkConfigurationFromThis()
    {
        var config = default(ChunkConfiguration);

        config.Name = ChunkName;
        config.VentAmount = VentPerSecond;
        config.Dissolves = Dissolves;
        config.Size = EngulfSize;
        config.Damages = Damages;
        config.DeleteOnTouch = DeleteOnTouch;
        config.Mass = Density / 1000;
        config.DamageType = DamageType;

        config.Radius = Radius;
        config.ChunkScale = ChunkScale;

        // Read graphics data set by the spawn function
        config.Meshes = new List<ChunkConfiguration.ChunkScene>();

        var item = new ChunkConfiguration.ChunkScene
        {
            LoadedScene = GraphicsScene, ScenePath = GraphicsScene.ResourcePath, SceneModelPath = ModelNodePath,
            LoadedConvexShape = ConvexPhysicsMesh, ConvexShapePath = ConvexPhysicsMesh?.ResourcePath,
            SceneAnimationPath = AnimationPath,
        };

        config.Meshes.Add(item);

        if (Compounds.Compounds.Count > 0)
        {
            config.Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>();

            foreach (var entry in Compounds)
            {
                config.Compounds.Add(entry.Key, new ChunkConfiguration.ChunkCompound { Amount = entry.Value });
            }
        }

        if (RequisiteEnzymeToDigest != null)
            config.DissolverEnzyme = RequisiteEnzymeToDigest.InternalName;

        return config;
    }

    /// <summary>
    ///   Processes this chunk
    /// </summary>
    /// <returns>True if this wants to be destroyed</returns>
    public bool ProcessChunk(float delta, CompoundCloudSystem compoundClouds)
    {
        if (PhagocytosisStep != PhagocytosisPhase.None)
            return false;

        if (isDissolving)
        {
            if (HandleDissolving(delta))
            {
                return true;
            }
        }

        if (isFadingParticles)
        {
            particleFadeTimer -= delta;

            if (particleFadeTimer <= 0)
            {
                return true;
            }
        }

        elapsedSinceProcess += delta;

        // Skip some of our more expensive operations if not enough time has passed
        // This doesn't actually seem to have that much effect with reasonable chunk counts... but doesn't seem
        // to hurt either, so for the future I think we should keep this -hhyyrylainen
        if (elapsedSinceProcess < Constants.FLOATING_CHUNK_PROCESS_INTERVAL)
            return false;

        VentCompounds(elapsedSinceProcess, compoundClouds);

        if (UsesDespawnTimer)
            DespawnTimer += elapsedSinceProcess;

        // Check contacts
        foreach (var microbe in touchingMicrobes)
        {
            // TODO: is it possible that this throws the disposed exception?
            if (microbe.Dead)
                continue;

            // Damage
            if (Damages > 0)
            {
                if (DeleteOnTouch)
                {
                    microbe.Damage(Damages, DamageType);
                }
                else
                {
                    microbe.Damage(Damages * elapsedSinceProcess, DamageType);
                }
            }

            if (DeleteOnTouch)
            {
                if (DissolveOrRemove())
                {
                    return true;
                }

                break;
            }
        }

        if (DespawnTimer > Constants.DESPAWNING_CHUNK_LIFETIME)
        {
            VentAllCompounds(compoundClouds);
            if (DissolveOrRemove())
            {
                return true;
            }
        }

        elapsedSinceProcess = 0;
        return false;
    }

    public void PopImmediately(CompoundCloudSystem compoundClouds)
    {
        VentAllCompounds(compoundClouds);
    }

    public void VentAllCompounds(CompoundCloudSystem compoundClouds)
    {
        // Vent all remaining compounds immediately
        if (Compounds.Compounds.Count > 0)
        {
            var pos = Position;

            var keys = new List<Compound>(Compounds.Compounds.Keys);

            foreach (var compound in keys)
            {
                var amount = Compounds.GetCompoundAmount(compound);
                Compounds.TakeCompound(compound, amount);

                if (amount < MathUtils.EPSILON)
                    continue;

                VentCompound(pos, compound, amount, compoundClouds);
            }
        }
    }

    public Dictionary<Compound, float>? CalculateAdditionalDigestibleCompounds()
    {
        return null;
    }

    public void OnAttemptedToBeEngulfed()
    {
    }

    public void OnIngestedFromEngulfment()
    {
    }

    public void OnExpelledFromEngulfment()
    {
        if (DigestedAmount > 0)
        {
            // Just dissolve this chunk entirely (assume that it has become unstable from digestion)
            DespawnTimer = Constants.DESPAWNING_CHUNK_LIFETIME + 1;
        }
    }

    public void OnMouseEnter(RaycastResult result)
    {
    }

    public void OnMouseExit(RaycastResult result)
    {
    }

    private void InitGraphics()
    {
        var graphicsNode = GraphicsScene.Instance();

        VisualNode.AddChild(graphicsNode);

        // Scale is now applied here as this doesn't conflict with the random rotation set by the spawner
        VisualNode.Scale = new Vector3(ChunkScale, ChunkScale, ChunkScale);

        if (!string.IsNullOrEmpty(ModelNodePath))
        {
            chunkMesh = graphicsNode.GetNode<MeshInstance>(ModelNodePath);
            return;
        }

        if (graphicsNode.IsClass("MeshInstance"))
        {
            chunkMesh = (MeshInstance)graphicsNode;
        }
        else if (graphicsNode.IsClass("Particles"))
        {
            particles = (Particles)graphicsNode;
        }
        else
        {
            throw new Exception("Invalid class for chunk graphics scene node");
        }
    }

    private void InitPhysics()
    {
        // Apply physics shape
        throw new NotImplementedException();

        /*if (ConvexPhysicsMesh == null)
        {
            var sphereShape = new SphereShape { Radius = Radius };
            shape.Shape = sphereShape;
        }
        else
        {
            if (chunkMesh == null)
                throw new InvalidOperationException("Can't use convex physics shape without mesh for chunk");

            // TODO: scale?

            shape.Shape = ConvexPhysicsMesh;
            shape.Transform = chunkMesh.Transform;
        }*/

        // Needs physics callback when this is engulfable or damaging
        if (Damages > 0 || DeleteOnTouch || EngulfSize > 0)
        {
            RegisterCollisionCallback(OnContactBegin);

            // TODO: contact end callback / modify the begin callback to continuously trigger for active physics
            // Connect("body_shape_exited", this, nameof(OnContactEnd));
        }
    }

    /// <summary>
    ///   Vents compounds if this is a chunk that contains compounds
    /// </summary>
    private void VentCompounds(float delta, CompoundCloudSystem compoundClouds)
    {
        if (Compounds.Compounds.Count <= 0)
            return;

        var pos = Position;

        var keys = new List<Compound>(Compounds.Compounds.Keys);

        // Loop through all the compounds in the storage bag and eject them
        bool vented = false;
        foreach (var compound in keys)
        {
            var amount = Compounds.GetCompoundAmount(compound);

            if (amount <= 0)
                continue;

            var got = Compounds.TakeCompound(compound, VentPerSecond * delta);

            if (got > MathUtils.EPSILON)
            {
                VentCompound(pos, compound, got, compoundClouds);
                vented = true;
            }
        }

        // If you did not vent anything this step and the venter component
        // is flagged to dissolve you, dissolve you
        if (!vented && Dissolves)
        {
            isDissolving = true;
        }
    }

    private void VentCompound(Vector3 pos, Compound compound, float amount, CompoundCloudSystem compoundClouds)
    {
        compoundClouds.AddCloud(compound, amount * Constants.CHUNK_VENT_COMPOUND_MULTIPLIER, pos);
    }

    /// <summary>
    ///   Handles the dissolving effect for the chunks when they run out of compounds.
    /// </summary>
    private bool HandleDissolving(float delta)
    {
        if (chunkMesh == null)
            throw new InvalidOperationException("Chunk without a mesh can't dissolve");

        if (PhagocytosisStep != PhagocytosisPhase.None)
            return false;

        // Disable collisions
        DisableAllCollisions();

        DigestedAmount += delta * Constants.FLOATING_CHUNKS_DISSOLVE_SPEED;

        if (DigestedAmount >= Constants.FULLY_DIGESTED_LIMIT)
        {
            return true;
        }

        return false;
    }

    private void UpdateDissolveEffect()
    {
        if (chunkMesh == null)
            throw new InvalidOperationException("Chunk without a mesh can't dissolve");

        if (chunkMesh.MaterialOverride is ShaderMaterial material)
            material.SetShaderParam("dissolveValue", dissolveEffectValue);
    }

    private void ApplyRenderPriority()
    {
        if (chunkMesh == null)
            throw new InvalidOperationException("Chunk without a mesh can't be applied a render priority");

        chunkMesh.MaterialOverride.RenderPriority = RenderPriority;
    }

    private void OnContactBegin(PhysicsBody physicsBody, int collidedSubShapeDataOurs, int bodyShape)
    {
        throw new NotImplementedException();

        /*_ = bodyID;
        _ = localShape;

        if (body is Microbe microbe)
        {
            // Can't engulf with a pilus
            if (microbe.IsPilus(microbe.ShapeFindOwner(bodyShape)))
                return;

            var target = microbe.GetMicrobeFromShape(bodyShape);
            if (target != null)
                touchingMicrobes.Add(target);
        }*/
    }

    private void OnContactEnd(int bodyID, Node body, int bodyShape, int localShape)
    {
        throw new NotImplementedException();
        /*_ = bodyID;
        _ = localShape;

        if (body is Microbe microbe)
        {
            var shapeOwner = microbe.ShapeFindOwner(bodyShape);

            // This can happen when a microbe unbinds while also touching a floating chunk
            // TODO: Do something more elegant to stop the error messages in the log
            if (shapeOwner == 0)
            {
                touchingMicrobes.Remove(microbe);
                return;
            }

            // This might help in a case where the cell is touching with both a pilus and non-pilus part
            if (microbe.IsPilus(shapeOwner))
                return;

            var target = microbe.GetMicrobeFromShape(bodyShape);

            if (target != null)
                touchingMicrobes.Remove(target);
        }*/
    }

    private bool DissolveOrRemove()
    {
        if (Dissolves)
        {
            isDissolving = true;
        }
        else if (particles != null && !isFadingParticles)
        {
            isFadingParticles = true;

            DisableAllCollisions();

            particles.Emitting = false;
            particleFadeTimer = particles.Lifetime;
        }
        else if (particles == null)
        {
            return true;
        }

        return false;
    }

    // TODO: do something with these
    public Vector3 RelativePosition { get; set; }
    public Quat RelativeRotation { get; set; }
    public bool AttachedToAnEntity { get; set; }
}
