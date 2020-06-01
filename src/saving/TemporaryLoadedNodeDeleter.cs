using System.Collections.Generic;
using Godot;

public class TemporaryLoadedNodeDeleter : Node
{
    private static TemporaryLoadedNodeDeleter instance;

    private readonly List<Node> nodesToDelete = new List<Node>();

    private TemporaryLoadedNodeDeleter()
    {
        instance = this;
    }

    public static TemporaryLoadedNodeDeleter Instance => instance;

    /// <summary>
    ///   If true skips deleting things, used to apply saves in steps
    /// </summary>
    public bool HoldDeletion { get; set; } = false;

    public override void _Ready()
    {
        PauseMode = PauseModeEnum.Process;
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
