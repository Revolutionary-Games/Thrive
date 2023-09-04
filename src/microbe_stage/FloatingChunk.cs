using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Script for the floating chunks (cell parts, rocks, hazards)
/// </summary>
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/microbe_stage/FloatingChunk.tscn", UsesEarlyResolve = false)]
public class FloatingChunk : RigidBody, ISpawned, IEngulfable, IInspectableEntity
{
#pragma warning disable CA2213 // a shared resource from the chunk definition
    [Export]
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

        /// <summary>
    ///   The organelles this unlocks when the player ingests it.
    /// </summary>
    [JsonProperty]
    private IEnumerable<OrganelleDefinition>? unlocksOrganelles;

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
    public float EntityWeight => 1.0f;

    [JsonIgnore]
    public Spatial EntityNode => this;

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

    public IEnumerable<OrganelleDefinition>? UnlocksOrganelles => unlocksOrganelles;

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
    ///   The name of kind of damage type this chunk inflicts. Default is "chunk".
    /// </summary>
    public string DamageType { get; set; } = "chunk";

    public string ChunkName { get; set; } = string.Empty;

    public bool EasterEgg { get; set; }

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

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

    public override void _Ready()
    {
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

        Mass = chunkType.Mass;

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

        unlocksOrganelles = chunkType.UnlocksOrganelles;

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
        config.Mass = Mass;
        config.DamageType = DamageType;

        config.Radius = Radius;
        config.ChunkScale = ChunkScale;

        // Read graphics data set by the spawn function
        config.Meshes = new List<ChunkConfiguration.ChunkScene>();

        var item = new ChunkConfiguration.ChunkScene
        {
            LoadedScene = GraphicsScene,
            ScenePath = GraphicsScene.ResourcePath,
            SceneModelPath = ModelNodePath,
            LoadedConvexShape = ConvexPhysicsMesh,
            ConvexShapePath = ConvexPhysicsMesh?.ResourcePath,
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

    public void ProcessChunk(float delta, CompoundCloudSystem compoundClouds)
    {
        if (PhagocytosisStep != PhagocytosisPhase.None)
            return;

        if (isDissolving)
            HandleDissolving(delta);

        if (isFadingParticles)
        {
            particleFadeTimer -= delta;

            if (particleFadeTimer <= 0)
            {
                this.DestroyDetachAndQueueFree();
            }
        }

        elapsedSinceProcess += delta;

        // Skip some of our more expensive operations if not enough time has passed
        // This doesn't actually seem to have that much effect with reasonable chunk counts... but doesn't seem
        // to hurt either, so for the future I think we should keep this -hhyyrylainen
        if (elapsedSinceProcess < Constants.FLOATING_CHUNK_PROCESS_INTERVAL)
            return;

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
                DissolveOrRemove();
                break;
            }
        }

        if (DespawnTimer > Constants.DESPAWNING_CHUNK_LIFETIME)
        {
            VentAllCompounds(compoundClouds);
            DissolveOrRemove();
        }

        elapsedSinceProcess = 0;
    }

    public void PopImmediately(CompoundCloudSystem compoundClouds)
    {
        VentAllCompounds(compoundClouds);
        this.DestroyDetachAndQueueFree();
    }

    public void VentAllCompounds(CompoundCloudSystem compoundClouds)
    {
        // Vent all remaining compounds immediately
        if (Compounds.Compounds.Count > 0)
        {
            var pos = Translation;

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

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
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
        GetNode("NodeToScale").AddChild(graphicsNode);

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
            throw new Exception("Invalid class");
        }
    }

    private void InitPhysics()
    {
        // Apply physics shape
        var shape = GetNode<CollisionShape>("CollisionShape");

        if (ConvexPhysicsMesh == null)
        {
            var sphereShape = new SphereShape { Radius = Radius };
            shape.Shape = sphereShape;
        }
        else
        {
            if (chunkMesh == null)
                throw new InvalidOperationException("Can't use convex physics shape without mesh for chunk");

            shape.Shape = ConvexPhysicsMesh;
            shape.Transform = chunkMesh.Transform;
        }

        // Needs physics callback when this is engulfable or damaging
        if (Damages > 0 || DeleteOnTouch || EngulfSize > 0)
        {
            ContactsReported = Constants.DEFAULT_STORE_CONTACTS_COUNT;
            Connect("body_shape_entered", this, nameof(OnContactBegin));
            Connect("body_shape_exited", this, nameof(OnContactEnd));
        }
    }

    /// <summary>
    ///   Vents compounds if this is a chunk that contains compounds
    /// </summary>
    private void VentCompounds(float delta, CompoundCloudSystem compoundClouds)
    {
        if (Compounds.Compounds.Count <= 0)
            return;

        var pos = Translation;

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
    private void HandleDissolving(float delta)
    {
        if (chunkMesh == null)
            throw new InvalidOperationException("Chunk without a mesh can't dissolve");

        if (PhagocytosisStep != PhagocytosisPhase.None)
            return;

        // Disable collisions
        CollisionLayer = 0;
        CollisionMask = 0;

        DigestedAmount += delta * Constants.FLOATING_CHUNKS_DISSOLVE_SPEED;

        if (DigestedAmount >= Constants.FULLY_DIGESTED_LIMIT)
        {
            this.DestroyDetachAndQueueFree();
        }
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

    private void OnContactBegin(int bodyID, Node body, int bodyShape, int localShape)
    {
        _ = bodyID;
        _ = localShape;

        if (body is Microbe microbe)
        {
            // Can't engulf with a pilus
            if (microbe.IsPilus(microbe.ShapeFindOwner(bodyShape)))
                return;

            var target = microbe.GetMicrobeFromShape(bodyShape);
            if (target != null)
                touchingMicrobes.Add(target);
        }
    }

    private void OnContactEnd(int bodyID, Node body, int bodyShape, int localShape)
    {
        _ = bodyID;
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
        }
    }

    private void DissolveOrRemove()
    {
        if (Dissolves)
        {
            isDissolving = true;
        }
        else if (particles != null && !isFadingParticles)
        {
            isFadingParticles = true;

            // Disable collisions
            CollisionLayer = 0;
            CollisionMask = 0;

            particles.Emitting = false;
            particleFadeTimer = particles.Lifetime;
        }
        else if (particles == null)
        {
            this.DestroyDetachAndQueueFree();
        }
    }
}
