using Godot;

/// <summary>
///   Common helper operations for Nodes
/// </summary>
public static class NodeHelpers
{
    /// <summary>
    ///   Safely frees a Node. Detaches from parent if attached to not leave disposed objects in scene tree.
    ///   This should always be preferred over Free, except when multiple children should be deleted.
    ///   For that see <see cref="NodeHelpers.FreeChildren"/>
    /// </summary>
    public static void SafeFree(this Node node)
    {
        var parent = node.GetParent();
        parent?.RemoveChild(node);

        node.Free();
    }

    /// <summary>
    ///   Safely queues a Node free. Detaches from parent if attached to not leave disposed objects in scene tree.
    ///   This should always be preferred over QueueFree, except when multiple children should be deleted.
    ///   For that see <see cref="NodeHelpers.QueueFreeChildren"/>
    /// </summary>
    public static void SafeQueueFree(this Node node)
    {
        var parent = node.GetParent();
        parent?.RemoveChild(node);

        node.QueueFree();
    }

    /// <summary>
    ///   Call QueueFree on all Node children
    /// </summary>
    /// <param name="node">Node to delete children of</param>
    /// <param name="detach">If true the children are also removed from the parent</param>
    public static void QueueFreeChildren(this Node node, bool detach = true)
    {
        while (true)
        {
            int count = node.GetChildCount();

            if (count < 1)
                break;

            var child = node.GetChild(count - 1);

            if (detach)
                node.RemoveChild(child);

            child.QueueFree();
        }
    }

    /// <summary>
    ///   Call Free on all Node children
    /// </summary>
    /// <param name="node">Node to delete children of</param>
    /// <param name="detach">If true the children are also removed from the parent</param>
    public static void FreeChildren(this Node node, bool detach = true)
    {
        while (true)
        {
            int count = node.GetChildCount();

            if (count < 1)
                break;

            var child = node.GetChild(count - 1);

            if (detach)
                node.RemoveChild(child);

            child.Free();
        }
    }
}
