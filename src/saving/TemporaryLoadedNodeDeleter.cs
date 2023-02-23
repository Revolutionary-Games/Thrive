using System;
using System.Collections.Generic;
using Godot;

public class TemporaryLoadedNodeDeleter : Node
{
    private static TemporaryLoadedNodeDeleter? instance;

    private readonly List<Node> nodesToDelete = new();
    private readonly HashSet<string> deletionHolds = new();

    private TemporaryLoadedNodeDeleter()
    {
        instance = this;
    }

    public static TemporaryLoadedNodeDeleter Instance => instance ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   If true skips deleting things, used to apply saves in steps
    /// </summary>
    public bool HoldDeletion => deletionHolds.Count > 0;

    public override void _Ready()
    {
        PauseMode = PauseModeEnum.Process;
    }

    public override void _Process(float delta)
    {
        if (HoldDeletion)
            return;

        foreach (var node in nodesToDelete)
        {
            try
            {
                if (node.GetParent() != null)
                {
                    GD.PrintErr("TemporaryLoadedNodeDeleter was given a node with a parent, not deleting it");
                    continue;
                }

                node.QueueFree();
            }
            catch (ObjectDisposedException)
            {
                GD.PrintErr("TemporaryLoadedNodeDeleter failed to delete a node because it was already disposed");
            }
        }

        nodesToDelete.Clear();
    }

    /// <summary>
    ///   Adds a deletion hold.
    /// </summary>
    /// <param name="name">Key of the deletion hold, use the same key to remove it later</param>
    /// <exception cref="ArgumentException">If the hold already exists</exception>
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

    public Node? Release(Node node)
    {
        if (nodesToDelete.Remove(node))
            return node;

        return null;
    }

    /// <summary>
    ///   Release all holds, should only be called by the main menu, and loading a save
    /// </summary>
    internal void ReleaseAllHolds()
    {
        deletionHolds.Clear();
    }
}
