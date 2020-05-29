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
        foreach (var node in nodesToDelete)
        {
            node.QueueFree();
        }

        nodesToDelete.Clear();
    }
}
