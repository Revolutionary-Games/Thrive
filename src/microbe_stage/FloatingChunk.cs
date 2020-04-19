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

    public int DespawnRadiusSqr { get; set; }

    public Node SpawnedNode
    {
        get
        {
            return this;
        }
    }

    /// <summary>
    ///   Determines how big this chunk is for engulfing calculations
    /// </summary>
    public float Size { get; set; } = 1000.0f;

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

        // TODO: setup physics callbacks
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
}
