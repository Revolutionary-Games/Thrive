﻿using System;
using System.Collections.Generic;
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
    ///   Properly destroys a game entity. In addition to the normal Godot Free, Destroy must be called.
    ///   This doesn't detach the entity from the scene tree so it'll be considered valid until the end of
    ///   the current frame.
    /// </summary>
    public static void DestroyAndQueueFree(this IEntity entity)
    {
        entity.OnDestroyed();
        entity.EntityNode.QueueFree();
    }

    /// <summary>
    ///   Detach a node from its parent
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Be very careful not to cause a resource leak if the node is detached but not freed.
    ///   </para>
    /// </remarks>
    public static void Detach(this Node node)
    {
        var parent = node.GetParent();
        parent?.RemoveChild(node);
    }

    /// <summary>
    ///   Safely queues a Node free. Detaches from parent if attached to not leave disposed objects in scene tree.
    ///   This should always be preferred over QueueFree, except when multiple children should be deleted.
    ///   For that see <see cref="NodeHelpers.QueueFreeChildren"/>
    /// </summary>
    public static void DetachAndQueueFree(this Node node)
    {
        node.Detach();
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
    ///   Changes parent of this <see cref="Node"/> to a new parent. The node needs to already have parent to use this.
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
    ///   Changes parent of this <see cref="Node3D"/> to a new parent, while keeping the global position. The node
    ///   needs to already have parent to use this.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This will likely be obsolete once https://github.com/godotengine/godot/pull/36301 is merged and
    ///     available in upcoming Godot versions.
    ///   </para>
    /// </remarks>
    public static void ReParentWithTransform(this Node3D node, Node newParent)
    {
        var temp = node.GlobalTransform;
        node.ReParent(newParent);
        node.GlobalTransform = temp;
    }

    /// <summary>
    ///   Converts a NodePath to a full path
    /// </summary>
    /// <param name="node">The node to use to resolve relative paths</param>
    /// <param name="potentiallyRelativePath">The path to resolve</param>
    /// <returns>The fully resolved path</returns>
    public static NodePath ResolveToAbsolutePath(this Node node, NodePath potentiallyRelativePath)
    {
        if (potentiallyRelativePath.IsEmpty || potentiallyRelativePath.IsAbsolute())
            return potentiallyRelativePath;

        return node.GetNode(potentiallyRelativePath).GetPath();
    }

    /// <summary>
    ///   Get the material of this scene's model. And handles <see cref="OrganelleMeshWithChildren"/> to get all
    ///   materials.
    /// </summary>
    /// <param name="node">Node to get material from.</param>
    /// <param name="result">Where to store the found materials</param>
    /// <param name="modelPath">Path to model within the scene. If null takes scene root as model.</param>
    /// <returns>True on success, false on problem</returns>
    public static bool GetMaterial(this Node node, List<ShaderMaterial> result, NodePath? modelPath = null)
    {
        GeometryInstance3D? geometry;

        try
        {
            // Fetch the actual model from the scene
            if (modelPath == null || modelPath.IsEmpty)
            {
                geometry = (GeometryInstance3D?)node;
            }
            else
            {
                geometry = node.GetNode<GeometryInstance3D>(modelPath);
            }
        }
        catch (InvalidCastException)
        {
            GD.PrintErr("Converting node to GeometryInstance3D for getting material failed, on node: ", node.GetPath(),
                " relative path: ", modelPath);
            return false;
        }

        if (geometry == null)
        {
            GD.PrintErr("Getting geometry instance for material fetch failed on node: ", node.GetPath(),
                " relative path: ", modelPath);
            return false;
        }

        bool success = false;

        try
        {
            if (geometry.MaterialOverride != null)
            {
                result.Add((ShaderMaterial)geometry.MaterialOverride);
                success = true;
            }
        }
        catch (InvalidCastException)
        {
            GD.PrintErr($"Converting material to {nameof(ShaderMaterial)} failed, on node: ", node.GetPath(),
                " relative path: ",
                modelPath);

            return false;
        }

        // Handle multipart mesh objects
        if (node is OrganelleMeshWithChildren nodeWithChildren)
        {
            nodeWithChildren.GetChildrenMaterials(result);
        }
        else if (geometry is OrganelleMeshWithChildren geometryWithChildren)
        {
            geometryWithChildren.GetChildrenMaterials(result);
        }

        if (success)
            return true;

        GD.PrintErr("Material override missing on node (", node, "): ", node.GetPath(),
            " relative path: ", modelPath);

        return false;
    }

    /// <summary>
    ///   Finds immediate children of node that are in a group
    /// </summary>
    /// <param name="node">The node to find children of</param>
    /// <param name="group">The group the children need to be in</param>
    /// <typeparam name="T">The type the children are cast to before returning</typeparam>
    /// <returns>Enumerable sequence of the children</returns>
    public static IEnumerable<T> GetChildrenToProcess<T>(this Node node, string group)
    {
        foreach (Node child in node.GetChildren())
        {
            if (!child.IsInGroup(group))
                continue;

            if (child is T casted)
            {
                yield return casted;
            }
            else
            {
                GD.PrintErr("A node has been put in the ", group, " group but it isn't derived from ", typeof(T).Name);
            }
        }
    }

    /// <summary>
    ///   Recursively counts the children of the given Node
    /// </summary>
    /// <param name="node">Where to start counting</param>
    /// <param name="includeInternal">If true internal Nodes are also counted</param>
    public static int RecursiveCountChildren(this Node node, bool includeInternal = false)
    {
        int childCount = node.GetChildCount(includeInternal);

        int count = 0;

        for (int i = 0; i < childCount; ++i)
        {
            var child = node.GetChild(i, includeInternal);

            if (child == null)
            {
                GD.PrintErr("Error getting child when counting recursively");
                break;
            }

            count += RecursiveCountChildren(child, includeInternal);
        }

        return childCount + count;
    }

    /// <summary>
    ///   Gets the parent of a Node as a Spatial in a way that actually works (Godot inbuilt method for this fails
    ///   randomly)
    /// </summary>
    /// <param name="node">The node to get the parent from</param>
    /// <returns>The parent Spatial or null</returns>
    public static Node3D? GetParentSpatialWorking(this Node node)
    {
        var parent = node.GetParent();

        return parent as Node3D;
    }

    /// <summary>
    ///   Looks through all parent nodes recursively of a node to find one of matching type
    /// </summary>
    /// <param name="node">The node which parent is the first node to search</param>
    /// <typeparam name="T">The type to look for</typeparam>
    /// <returns>The found node or null</returns>
    public static T? FirstAncestorOfType<T>(this Node node)
        where T : Node
    {
        var parent = node.GetParent();

        while (parent != null)
        {
            if (parent is T casted)
                return casted;

            parent = parent.GetParent();
        }

        return null;
    }

    /// <summary>
    ///   Looks through all descendant nodes recursively of a node to find one of matching type in a depth first order.
    ///   Only should be used in exceptional cases where a proper architecture can't be used (easily).
    /// </summary>
    /// <param name="node">
    ///   The node to start the search at (the node's children are searched first, not the node itself)
    /// </param>
    /// <typeparam name="T">The type to look for</typeparam>
    /// <returns>The found node or null</returns>
    /// <remarks>
    ///   <para>
    ///     This is a pretty dirty hack, but is useful in some prototype or "temporary" code situations where there
    ///     isn't an easy way to do things properly
    ///   </para>
    /// </remarks>
    public static T? FirstDescendantOfType<T>(this Node node)
        where T : Node
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is T casted)
                return casted;

            if (child.GetChildCount() > 0)
            {
                var recursiveResult = FirstDescendantOfType<T>(child);

                if (recursiveResult != null)
                    return recursiveResult;
            }
        }

        return null;
    }
}
