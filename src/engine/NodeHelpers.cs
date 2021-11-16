using Godot;

/// <summary>
///   Common helper operations for Nodes
/// </summary>
public static class NodeHelpers
{
    /// <summary>
    ///   Properly destroys a game entity. In addition to the normal Godot Free, Destroy must be called
    /// </summary>
    public static void DestroyDetachAndQueueFree(this IEntity entity)
    {
        entity.OnDestroyed();
        entity.EntityNode.DetachAndQueueFree();
    }

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

    /// <summary>
    ///   Changes parent of this Node to a new parent. The node needs to already have parent to use this.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This will likely be obsolete once https://github.com/godotengine/godot/pull/36301 is merged and
    ///     available in upcoming Godot versions.
    ///   </para>
    /// </remarks>
    public static void ReParent(this Node node, Node newParent)
    {
        if (node.GetParent() == null)
        {
            GD.PrintErr("Node needs parent to be re-parented");
            return;
        }

        node.GetParent().RemoveChild(node);
        newParent.AddChild(node);
    }

    /// <summary>
    ///   Get the material of this scenes model.
    /// </summary>
    /// <param name="node">Node to get material from.</param>
    /// <param name="modelPath">Path to model within the scene. If null takes scene root as model.</param>
    /// <returns>ShaderMaterial or null if not found.</returns>
    public static ShaderMaterial GetMaterial(this Node node, string modelPath = null)
    {
        GeometryInstance geometry;

        // Fetch the actual model from the scene
        if (string.IsNullOrEmpty(modelPath))
        {
            geometry = node as GeometryInstance;
        }
        else
        {
            geometry = node.GetNode<GeometryInstance>(modelPath);
        }

        return geometry?.MaterialOverride as ShaderMaterial;
    }
}
