using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Helper class for managing creating child objects based on a key and caching them to not recreate them each frame
/// </summary>
public class ChildObjectCache<TKey, TNode>
    where TKey : class
    where TNode : Node
{
    private readonly Node parentObject;
    private readonly CreateNewChildNode childCreator;

    private readonly Dictionary<TKey, CreatedNode> createdChildren = new Dictionary<TKey, CreatedNode>();

    private int nextAccessOrder;

    public ChildObjectCache(Node parent, CreateNewChildNode childCreator)
    {
        parentObject = parent;
        this.childCreator = childCreator;

        if (parentObject == null)
            throw new NullReferenceException();
    }

    public delegate TNode CreateNewChildNode(TKey child);

    public void Clear()
    {
        foreach (var child in createdChildren.Keys.ToList())
            DeleteChild(child);
    }

    public void UnMarkAll()
    {
        foreach (var entry in createdChildren)
            entry.Value.Marked = false;

        nextAccessOrder = 0;
    }

    public void DeleteUnmarked()
    {
        foreach (var child in createdChildren.Where(p => !p.Value.Marked).Select(p => p.Key).ToList())
            DeleteChild(child);
    }

    public void DeleteChild(TKey child)
    {
        if (!createdChildren.ContainsKey(child))
            throw new ArgumentException("child not a child of this", nameof(child));

        var entry = createdChildren[child];
        var node = entry.Node;
        parentObject.RemoveChild(node);
        node.QueueFree();

        createdChildren.Remove(child);
    }

    public TNode GetChild(TKey child)
    {
        CreatedNode entry;

        if (createdChildren.ContainsKey(child))
        {
            entry = createdChildren[child];
            entry.Marked = true;
            entry.AccessOrder = nextAccessOrder++;
            return entry.Node;
        }

        var node = childCreator(child);
        entry = new CreatedNode(node) { AccessOrder = nextAccessOrder++ };
        createdChildren[child] = entry;

        parentObject.AddChild(node);

        return node;
    }

    /// <summary>
    ///   Makes the children be in the order they were last accessed with GetChild
    /// </summary>
    public void ApplyOrder()
    {
        foreach (var entry in createdChildren)
        {
            if (entry.Value.AccessOrder != entry.Value.Node.GetIndex())
            {
                parentObject.MoveChild(entry.Value.Node, entry.Value.AccessOrder);
            }
        }
    }

    private class CreatedNode
    {
        public readonly TNode Node;
        public bool Marked = true;
        public int AccessOrder;

        public CreatedNode(TNode node)
        {
            Node = node;
        }
    }
}
