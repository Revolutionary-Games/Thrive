using Godot;

/// <summary>
///   Common helper operations for Nodes
/// </summary>
public static class NodeHelpers
{
    /// <summary>
    ///   Call QueueFree on all Node children
    /// </summary>
    /// <param name="node">Node to delete children of</param>
    /// <param name="detach">If true the children are also detached</param>
    public static void QueueFreeChildren(this Node node, bool detach = false)
    {
        if (node.GetChildCount() > 0)
        {
            foreach (Node child in node.GetChildren())
            {
                child.QueueFree();

                if (detach)
                    node.RemoveChild(child);
            }
        }
    }

    /// <summary>
    ///   Call Free on all Node children
    /// </summary>
    /// <param name="node">Node to delete children of</param>
    public static void FreeChildren(this Node node)
    {
        if (node.GetChildCount() > 0)
        {
            foreach (Node child in node.GetChildren())
            {
                child.Free();
            }
        }
    }
}
