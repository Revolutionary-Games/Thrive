using System.Collections.Generic;
using Godot;
using Godot.Collections;
using static CustomWindow;

public class AddWindowReorderingSupportToSiblings : Control
{
    [Export]
    public Array<NodePath> WindowReorderingSupportPaths = new();

    /// <summary>
    ///   Ignored when window reordering paths are not empty
    /// </summary>
    [Export]
    public int AutomaticWindowReorderingSupportDepth = 0;

    [Export]
    public bool ReorderWindows = true;

#pragma warning disable CA2213
    private Array<(AddWindowReorderingSupportToSiblings Node, Node Sibling)> windowReorderingAncestors = new();

    private Node? topSibling;
    private Node parent = null!;

#pragma warning restore CA2213

    // used to save top nodes of windows
    private System.Collections.Generic.Dictionary<CustomWindow, Node> connectedWindows = new();
    private HashSet<Node> connectedSiblings = new();

    public static IEnumerable<(AddWindowReorderingSupportToSiblings Node, Node Sibling)> GetWindowReorderingAncestors(
        Node startingNode, int maxSearchDepth, Array<NodePath>? paths)
    {
        if (paths is null || paths.Count == 0)
        {
            foreach (var windowReorderingSupport in GetWindowReorderingAncestors(startingNode, maxSearchDepth))
                yield return windowReorderingSupport;
        }
        else
        {
            foreach (var windowReorderingSupport in GetWindowReorderingAncestors(startingNode, paths))
                yield return windowReorderingSupport;
        }
    }

    public override void _Ready()
    {
        parent = GetParent();

        var windowReorderingAncestorsIEnumerable = GetWindowReorderingAncestors(this,
            AutomaticWindowReorderingSupportDepth, WindowReorderingSupportPaths);

        foreach (var windowReorderingAncestor in windowReorderingAncestorsIEnumerable)
            windowReorderingAncestors.Add(windowReorderingAncestor);
    }

    public void ConnectWindow(CustomWindow window, Node topNode)
    {
        for (int i = 0; i < windowReorderingAncestors.Count; i++)
        {
            windowReorderingAncestors[i].Node.ConnectWindow(window, windowReorderingAncestors[i].Sibling);
        }

        if (!ReorderWindows)
            return;

        window.Connect(nameof(Dragged), this, nameof(OnWindowReorder));

        connectedWindows.Add(window, topNode);
        connectedSiblings.Add(topNode);

        if (topSibling is null || topSibling.GetIndex() < topNode.GetIndex())
            topSibling = topNode;
    }

    public void DisconnectWindow(CustomWindow window)
    {
        for (int i = 0; i < windowReorderingAncestors.Count; i++)
        {
            windowReorderingAncestors[i].Node.DisconnectWindow(window);
        }

        if (!ReorderWindows)
            return;

        window.Disconnect(nameof(Dragged), this, nameof(OnWindowReorder));

        var windowSibling = connectedWindows[window];
        connectedWindows.Remove(window);

        // Find if another connected window has the same sibling
        bool foundSibling = false;
        foreach (var sibling in connectedSiblings)
        {
            if (sibling == windowSibling)
            {
                foundSibling = true;
                break;
            }
        }

        if (!foundSibling)
        {
            // No other window has the same sibling so it can be removed
            connectedSiblings.Remove(windowSibling);
        }
    }

    public void UpdateTopSibling(Array siblings)
    {
        if (topSibling is not null)
            return;

        for (int i = siblings.Count - 1; i >= 0; i--)
        {
            var sibling = (Node)siblings[i];
            if (!connectedSiblings.Contains(sibling))
                continue;

            topSibling = sibling;
            break;
        }
    }

    public void OnWindowReorder(CustomDialog window)
    {
        Node targetSibling = connectedWindows[window];
        var siblings = parent.GetChildren();

        UpdateTopSibling(siblings);

        int topSiblingIndex = topSibling!.GetIndex();
        int targetSiblingIndex = targetSibling.GetIndex();

        if (topSiblingIndex == targetSiblingIndex)
        {
            // This window is already on the top
            return;
        }

        // Put the sibling on the top
        parent.MoveChild(targetSibling, topSiblingIndex);

        topSibling = targetSibling;

        // For unexplained reasons this has to be here to update the order visually
        for (int i = targetSiblingIndex + 1; i <= topSiblingIndex; i++)
        {
            parent.MoveChild(siblings[i] as Node, topSiblingIndex);
        }

        // For unexplained reasons this has to be here to update the order visually
        parent.MoveChild(targetSibling, topSiblingIndex);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var path in WindowReorderingSupportPaths)
                path.Dispose();
        }

        base.Dispose(disposing);
    }

    private static IEnumerable<(AddWindowReorderingSupportToSiblings Node, Node Sibling)> GetWindowReorderingAncestors(
        Node startingNode, int maxSearchDepth)
    {
        Node childOfAncestor = startingNode;
        Node ancestor = childOfAncestor.GetParent();

        for (int i = 0; i < maxSearchDepth; ++i)
        {
            if (ancestor is null)
                break;

            var windowReorderingSupportNode = ancestor.GetNodeOrNull
                <AddWindowReorderingSupportToSiblings>(nameof(AddWindowReorderingSupportToSiblings));

            if (windowReorderingSupportNode != null)
            {
                yield return (windowReorderingSupportNode, childOfAncestor);

                break;
            }

            childOfAncestor = ancestor;
            ancestor = childOfAncestor.GetParent();
        }
    }

    private static IEnumerable<(AddWindowReorderingSupportToSiblings Node, Node Sibling)> GetWindowReorderingAncestors(
        Node startingNode, Array<NodePath> windowReorderingNodePaths)
    {
        Node childOfAncestor = startingNode;
        Node ancestor = childOfAncestor.GetParent();

        System.Collections.Generic.Dictionary
            <Node, AddWindowReorderingSupportToSiblings> windowReorderingNodesWithParents = new();

        foreach (var path in windowReorderingNodePaths)
        {
            var windowReorderingNode = startingNode.GetNode<AddWindowReorderingSupportToSiblings>(path);
            windowReorderingNodesWithParents.Add(
                windowReorderingNode.GetParent(), windowReorderingNode);
        }

        while (ancestor is not null)
        {
            if (windowReorderingNodesWithParents.TryGetValue(ancestor, out var windowReorderingNode))
            {
                windowReorderingNodesWithParents.Remove(ancestor);

                yield return (windowReorderingNode, childOfAncestor);

                if (windowReorderingNodesWithParents.Count == 0)
                {
                    // connected to every window reordering node
                    break;
                }
            }

            childOfAncestor = ancestor;
            ancestor = childOfAncestor.GetParent();
        }
    }
}
