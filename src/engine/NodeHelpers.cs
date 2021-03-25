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
    /// <remarks>
    ///   <para>
    ///     TODO: should this be removed now that there is (and Free isn't actually bugged):
    ///     https://github.com/Revolutionary-Games/Thrive/pull/2028
    ///   </para>
    /// </remarks>
    public static void DetachAndFree(this Node node)
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
    public static void DetachAndQueueFree(this Node node)
    {
        var parent = node.GetParent();
        parent?.RemoveChild(node);

        node.QueueFree();
    }

    /// <summary>
    ///   Call QueueFree on all Node children
    /// </summary>
    /// <param name="node">Node to delete children of</param>
    /// <param name="detach">If true the children are immediately removed from the parent</param>
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
    /// <param name="detach">
    ///   If true the children are also removed from the parent. Shouldn't actually have an effect.
    ///   <see cref="DetachAndFree"/>
    /// </param>
    public static void FreeChildren(this Node node, bool detach = false)
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
