using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Script for the floating chunks (cell parts, rocks, hazards)
/// </summary>
public class FloatingChunk : RigidBody, ISpawned
{
    [Export]
    public PackedScene GraphicsScene;

    private CompoundCloudSystem compoundClouds;

    /// <summary>
    ///   Used to check if a microbe wants to engulf this
    /// </summary>
    private HashSet<Microbe> touchingMicrobes = new HashSet<Microbe>();

    public int DespawnRadiusSqr { get; set; }

    public Node SpawnedNode
    {
        get
        {
            return this;
        }
    }

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
    public bool Disolves { get; set; } = false;

    /// <summary>
    ///   If > 0 applies damage to a cell on touch
    /// </summary>
    public float Damages { get; set; } = 0.0f;

    /// <summary>
    ///   If true this gets deleted when a cell touches this
    /// </summary>
    public bool DeleteOnTouch { get; set; } = false;

    /// <summary>
    ///   Grabs data from the type to initialize this
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Doesn't initialize the graphics scene which needs to be set separately
    ///   </para>
    /// </remarks>
    public void Init(Biome.ChunkConfiguration chunkType, CompoundCloudSystem compoundClouds)
    {
        this.compoundClouds = compoundClouds;

        // Grab data
        VentPerSecond = chunkType.VentAmount;
        Disolves = chunkType.Dissolves;
        Size = chunkType.Size;
        Damages = chunkType.Damages;
        DeleteOnTouch = chunkType.DeleteOnTouch;

        Mass = chunkType.Mass;

        // Apply physics shape
        var shape = GetNode<CollisionShape>("CollisionShape");

        // This only works as long as the sphere shape type is not changed in the editor
        ((SphereShape)shape.Shape).Radius = chunkType.Radius;

        // Copy compounds to vent
        if (chunkType.Compounds != null && chunkType.Compounds.Count > 0)
        {
            // Capacity is set to 0 so that no compounds can be added
            // the normal way to the chunk
            ContainedCompounds = new CompoundBag(0);

            foreach (var entry in chunkType.Compounds)
            {
                ContainedCompounds.Compounds.Add(entry.Key, entry.Value.Amount);
            }
        }

        // Needs physics callback when this is engulfable or damaging
        if (Damages > 0 || DeleteOnTouch || Size > 0)
        {
            ContactsReported = Constants.DEFAULT_STORE_CONTACTS_COUNT;
            Connect("body_shape_entered", this, "OnContactBegin");
            Connect("body_shape_exited", this, "OnContactEnd");
        }
    }

    public override void _Ready()
    {
        if (GraphicsScene == null)
        {
            GD.PrintErr("FloatingChunk doesn't have GraphicsScene set");
            return;
        }

        if (compoundClouds == null)
        {
            GD.PrintErr("FloatingChunk hasn't have init called");
            return;
        }

        AddChild(GraphicsScene.Instance());
    }

    public override void _Process(float delta)
    {
        if (ContainedCompounds != null)
            VentCompounds(delta);

        // Check contacts
        foreach (var microbe in touchingMicrobes)
        {
            // TODO: is it possible that this throws the disposed exception?
            if (microbe.Dead)
                continue;

            // Damage
            if (Damages > 0)
            {
                float totalDamage = Damages * delta;

                // TODO: Not the cleanest way to play the damage sound
                if (DeleteOnTouch)
                {
                    microbe.Damage(totalDamage, "toxin");
                }
                else
                {
                    microbe.Damage(totalDamage, "chunk");
                }
            }

            bool disappear = false;

            // Engulfing
            if (Size > 0 && microbe.EngulfMode)
            {
                // Check can engulf based on the size of the chunk compared to the cell size
                if (microbe.EngulfSize >= Size * Constants.ENGULF_SIZE_RATIO_REQ)
                {
                    // Can engulf
                    if (ContainedCompounds != null)
                    {
                        // TODO: could spill the amount that was wasted here as a cloud

                        foreach (var entry in ContainedCompounds)
                        {
                            microbe.Compounds.AddCompound(entry.Key, entry.Value /
                                Constants.CHUNK_ENGULF_COMPOUND_DIVISOR);
                        }
                    }

                    disappear = true;
                }
            }

            if (DeleteOnTouch || disappear)
            {
                QueueFree();
                break;
            }
        }
    }

    /// <summary>
    ///   Vents compounds if this is a chunk that contains compounds
    /// </summary>
    private void VentCompounds(float delta)
    {
        var pos = Translation;

        var keys = new List<string>(ContainedCompounds.Compounds.Keys);

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
        if (!vented && Disolves)
        {
            QueueFree();
        }
    }

    private void VentCompound(Vector3 pos, string compound, float amount)
    {
        compoundClouds.AddCloud(
            compound, amount * Constants.CHUNK_VENT_COMPOUND_MULTIPLIER, pos);
    }

    private void OnContactBegin(int bodyID, Node body, int bodyShape, int localShape)
    {
        if (body is Microbe microbe)
        {
            // Can't engulf with a pilus
            if (microbe.IsPilus(microbe.ShapeFindOwner(bodyShape)))
                return;

            touchingMicrobes.Add(microbe);
        }
    }

    private void OnContactEnd(int bodyID, Node body, int bodyShape, int localShape)
    {
        if (body is Microbe microbe)
        {
            // This might help in a case where the cell is touching with both a pilus and non-pilus part
            if (microbe.IsPilus(microbe.ShapeFindOwner(bodyShape)))
                return;

            touchingMicrobes.Remove(microbe);
        }
    }
}
