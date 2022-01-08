using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Script for the floating chunks (cell parts, rocks, hazards)
/// </summary>
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/microbe_stage/FloatingChunk.tscn", UsesEarlyResolve = false)]
public class FloatingChunk : RigidBody, ISpawned, ISaveLoadedTracked
{
    [Export]
    [JsonProperty]
    public PackedScene GraphicsScene;

    /// <summary>
    ///   If this is null, a sphere shape is used as a default for collision detections.
    /// </summary>
    [Export]
    [JsonProperty]
    public ConvexPolygonShape ConvexPhysicsMesh;

    /// <summary>
    ///   The node path to the mesh of this chunk
    /// </summary>
    public string ModelNodePath;

    [JsonProperty]
    private CompoundCloudSystem compoundClouds;

    /// <summary>
    ///   Used to check if a microbe wants to engulf this
    /// </summary>
    private HashSet<Microbe> touchingMicrobes = new HashSet<Microbe>();

    private MeshInstance chunkMesh;

    [JsonProperty]
    private bool isDissolving;

    [JsonProperty]
    private bool isFadingParticles;

    [JsonProperty]
    private float particleFadeTimer;

    [JsonProperty]
    private float dissolveEffectValue;

    [JsonProperty]
    private bool isParticles;

    public int DespawnRadiusSquared { get; set; }

    [JsonIgnore]
    public Node EntityNode => this;

    /// <summary>
    ///   Determines how big this chunk is for engulfing calculations. Set to &lt;= 0 to disable
    /// </summary>
    public float Size { get; set; } = -1.0f;

    /// <summary>
    ///   Compounds this chunk contains, and vents
    /// </summary>
    public CompoundBag ContainedCompounds { get; set; }

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

    public bool IsLoadedFromSave { get; set; }

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new AliveMarker();

    /// <summary>
    ///   Grabs data from the type to initialize this
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Doesn't initialize the graphics scene which needs to be set separately
    ///   </para>
    /// </remarks>
    public void Init(ChunkConfiguration chunkType, CompoundCloudSystem compoundClouds,
        string modelPath)
    {
        this.compoundClouds = compoundClouds;

        // Grab data
        VentPerSecond = chunkType.VentAmount;
        Dissolves = chunkType.Dissolves;
        Size = chunkType.Size;
        Damages = chunkType.Damages;
        DeleteOnTouch = chunkType.DeleteOnTouch;
        DamageType = string.IsNullOrEmpty(chunkType.DamageType) ? "chunk" : chunkType.DamageType;

        Mass = chunkType.Mass;

        // These are stored for saves to work
        Radius = chunkType.Radius;
        ChunkScale = chunkType.ChunkScale;

        ModelNodePath = modelPath;

        // Copy compounds to vent
        if (chunkType.Compounds?.Count > 0)
        {
            // Capacity is set to 0 so that no compounds can be added
            // the normal way to the chunk
            ContainedCompounds = new CompoundBag(0);

            foreach (var entry in chunkType.Compounds)
            {
                ContainedCompounds.Compounds.Add(entry.Key, entry.Value.Amount);
            }
        }
    }

    /// <summary>
    ///   Reverses the action of Init back to a ChunkConfiguration
    /// </summary>
    /// <returns>The reversed chunk configuration</returns>
    public ChunkConfiguration CreateChunkConfigurationFromThis()
    {
        var config = default(ChunkConfiguration);

        config.VentAmount = VentPerSecond;
        config.Dissolves = Dissolves;
        config.Size = Size;
        config.Damages = Damages;
        config.DeleteOnTouch = DeleteOnTouch;
        config.Mass = Mass;

        config.Radius = Radius;
        config.ChunkScale = ChunkScale;

        // Read graphics data set by the spawn function
        config.Meshes = new List<ChunkConfiguration.ChunkScene>();

        var item = new ChunkConfiguration.ChunkScene
        {
            LoadedScene = GraphicsScene, ScenePath = GraphicsScene.ResourcePath, SceneModelPath = ModelNodePath,
            LoadedConvexShape = ConvexPhysicsMesh, ConvexShapePath = ConvexPhysicsMesh?.ResourcePath,
        };

        config.Meshes.Add(item);

        if (ContainedCompounds?.Compounds.Count > 0)
        {
            config.Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>();

            foreach (var entry in ContainedCompounds)
            {
                config.Compounds.Add(entry.Key, new ChunkConfiguration.ChunkCompound { Amount = entry.Value });
            }
        }

        return config;
    }

    public override void _Ready()
    {
        if (compoundClouds == null)
            throw new InvalidOperationException("init hasn't been called on a FloatingChunk");

        var graphicsNode = GraphicsScene.Instance();
        GetNode("NodeToScale").AddChild(graphicsNode);

        if (string.IsNullOrEmpty(ModelNodePath))
        {
            if (graphicsNode.IsClass("MeshInstance"))
            {
                chunkMesh = (MeshInstance)graphicsNode;
            }
            else if (graphicsNode.IsClass("Particles"))
            {
                isParticles = true;
            }
            else
            {
                throw new Exception("Invalid class");
            }
        }
        else
        {
            chunkMesh = graphicsNode.GetNode<MeshInstance>(ModelNodePath);
        }

        if (chunkMesh == null && !isParticles)
            throw new InvalidOperationException("Can't make a chunk without graphics scene");

        InitPhysics();
    }

    public override void _Process(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        if (ContainedCompounds != null)
            VentCompounds(delta);

        if (isDissolving)
            HandleDissolving(delta);

        if (UsesDespawnTimer)
            DespawnTimer += delta;

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
                    microbe.Damage(Damages * delta, DamageType);
                }
            }

            bool disappear = false;

            // Engulfing
            if (Size > 0 && microbe.State == Microbe.MicrobeState.Engulf)
            {
                // Check can engulf based on the size of the chunk compared to the cell size
                if (microbe.EngulfSize >= Size * Constants.ENGULF_SIZE_RATIO_REQ)
                {
                    // Can engulf
                    if (ContainedCompounds != null)
                    {
                        foreach (var entry in ContainedCompounds)
                        {
                            var added = microbe.Compounds.AddCompound(entry.Key, entry.Value /
                                Constants.CHUNK_ENGULF_COMPOUND_DIVISOR) * Constants.CHUNK_ENGULF_COMPOUND_DIVISOR;

                            VentCompound(Translation, entry.Key, entry.Value - added);
                        }
                    }

                    disappear = true;
                }
            }

            if (DeleteOnTouch || disappear)
            {
                DissolveOrRemove();
                break;
            }
        }

        if (DespawnTimer > Constants.DESPAWNING_CHUNK_LIFETIME)
            DissolveOrRemove();

        if (isFadingParticles)
        {
            particleFadeTimer -= delta;

            if (particleFadeTimer <= 0)
            {
                OnDestroyed();
                this.DetachAndFree();
            }
        }
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    /// <summary>
    ///   Vents compounds if this is a chunk that contains compounds
    /// </summary>
    private void VentCompounds(float delta)
    {
        var pos = Translation;

        var keys = new List<Compound>(ContainedCompounds.Compounds.Keys);

        // Loop through all the compounds in the storage bag and eject them
        bool vented = false;
        foreach (var compound in keys)
        {
            var amount = ContainedCompounds.GetCompoundAmount(compound);

            if (amount <= 0)
                continue;

            var got = ContainedCompounds.TakeCompound(compound, VentPerSecond * delta);

            if (got > MathUtils.EPSILON)
            {
                VentCompound(pos, compound, got);
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

    private void VentCompound(Vector3 pos, Compound compound, float amount)
    {
        compoundClouds.AddCloud(
            compound, amount * Constants.CHUNK_VENT_COMPOUND_MULTIPLIER, pos);
    }

    /// <summary>
    ///   Handles the dissolving effect for the chunks when they run out of compounds.
    /// </summary>
    private void HandleDissolving(float delta)
    {
        // Disable collisions
        CollisionLayer = 0;
        CollisionMask = 0;

        var material = (ShaderMaterial)chunkMesh.MaterialOverride;

        dissolveEffectValue += delta * Constants.FLOATING_CHUNKS_DISSOLVE_SPEED;

        material.SetShaderParam("dissolveValue", dissolveEffectValue);

        if (dissolveEffectValue >= 1)
        {
            this.DestroyDetachAndQueueFree();
        }
    }

    private void InitPhysics()
    {
        // Apply physics shape
        var shape = GetNode<CollisionShape>("CollisionShape");

        if (ConvexPhysicsMesh == null)
        {
            shape.Shape = new SphereShape { Radius = Radius };
        }
        else
        {
            shape.Shape = ConvexPhysicsMesh;
            shape.Transform = chunkMesh.Transform;
        }

        // Needs physics callback when this is engulfable or damaging
        if (Damages > 0 || DeleteOnTouch || Size > 0)
        {
            ContactsReported = Constants.DEFAULT_STORE_CONTACTS_COUNT;
            Connect("body_shape_entered", this, nameof(OnContactBegin));
            Connect("body_shape_exited", this, nameof(OnContactEnd));
        }
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

            microbe = microbe.GetMicrobeFromShape(bodyShape);
            if (microbe != null)
                touchingMicrobes.Add(microbe);
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

            touchingMicrobes.Remove(microbe.GetMicrobeFromShape(bodyShape));
        }
    }

    private void DissolveOrRemove()
    {
        if (Dissolves)
        {
            isDissolving = true;
        }
        else if (isParticles && !isFadingParticles)
        {
            isFadingParticles = true;

            var particles = GetNode("NodeToScale").GetChild<Particles>(0);

            // Disable collisions
            CollisionLayer = 0;
            CollisionMask = 0;

            particles.Emitting = false;
            particleFadeTimer = particles.Lifetime;
        }
        else if (!isParticles)
        {
            this.DestroyDetachAndQueueFree();
        }
    }
}
