using System;
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
    public float Size { get; set; }

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

    public void Init(CompoundCloudSystem compoundClouds)
    {
        this.compoundClouds = compoundClouds;
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

        // Loop through all the compounds in the storage bag and eject them
        bool vented = false;
        foreach (var entry in ContainedCompounds.Compounds)
        {
            if (entry.Value <= 0)
                continue;

            var got = ContainedCompounds.TakeCompound(entry.Key, VentPerSecond * delta);

            if (got > MathUtils.EPSILON)
            {
                VentCompound(pos, entry.Key, got);
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
