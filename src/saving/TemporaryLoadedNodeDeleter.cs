using System;
using System.Collections.Generic;
using Godot;

public class TemporaryLoadedNodeDeleter : Node
{
    private static TemporaryLoadedNodeDeleter instance;

    private readonly List<Node> nodesToDelete = new List<Node>();
    private readonly HashSet<string> deletionHolds = new HashSet<string>();

    private TemporaryLoadedNodeDeleter()
    {
        instance = this;
    }

    public static TemporaryLoadedNodeDeleter Instance => instance;

    /// <summary>
    ///   If true skips deleting things, used to apply saves in steps
    /// </summary>
    public bool HoldDeletion => deletionHolds.Count > 0;

    public override void _Ready()
    {
        PauseMode = PauseModeEnum.Process;
    }

    /// <summary>
    ///   Adds a deletion hold. While at least one deletion hold hasn't been
    /// </summary>
    /// <param name="name">Key of the deletion hold, use the same key to remove it later</param>
    public void AddDeletionHold(string name)
    {
        if (!deletionHolds.Add(name))
            throw new ArgumentException("can't add same deletion hold twice", nameof(name));
    }

    public void RemoveDeletionHold(string name)
    {
        if (!deletionHolds.Remove(name))
            throw new ArgumentException("specified deletion hold doesn't exist", nameof(name));
    }

    public void Register(Node node)
    {
        nodesToDelete.Add(node);
    }

    public Node Release(Node node)
    {
        if (nodesToDelete.Remove(node))
            return node;

        return null;
    }

    public override void _Process(float delta)
    {
        if (HoldDeletion)
            return;

        foreach (var node in nodesToDelete)
        {
            node.QueueFree();
        }

        nodesToDelete.Clear();
    }
}
