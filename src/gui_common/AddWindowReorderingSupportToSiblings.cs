using System.Collections.Generic;
using Godot;
using Godot.Collections;
using static CustomWindow;

/// <summary>
///   Reorders windows by reordering their ancestors that are on the same level as this node.
/// </summary>
/// <remarks>
///   <para>
///     The windows are the ones who ask to establish the connection. If the window asks for a recursive
///     connection then the connection gets passed to window reordering nodes in ancestors too.
///   </para>
/// </remarks>
public class AddWindowReorderingSupportToSiblings : Control
{
    /// <summary>
    ///   Paths to window reordering nodes in ancestors.
    /// </summary>
    [Export]
    public Array<NodePath> WindowReorderingPaths = new();

    /// <summary>
    ///   Finds first window reordering node in ancestors to connect to.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Ignored when window reordering paths are not empty.
    ///   </para>
    /// </remarks>
    [Export]
    public int AutomaticWindowReorderingDepth = 5;

#pragma warning disable CA2213

    /// <summary>
    ///   Pairs containing window reordering nodes and what sibling of theirs is an ancestor of this class.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Used to pass the connections from windows.
    ///   </para>
    /// </remarks>
    private List<(AddWindowReorderingSupportToSiblings Node, Node Sibling)> windowReorderingAncestors = new();

    /// <summary>
    ///   A sibling that is an ancestor of a window that is currently on top of others.
    /// </summary>
    private Node? topSibling;

    private Node parent = null!;

#pragma warning restore CA2213

    private bool isReady;

    /// <summary>
    ///   Used to save what node to reorder when a window requires to be reordered.
    /// </summary>
    private System.Collections.Generic.Dictionary<CustomWindow, Node> connectedWindows = new();

    /// <summary>
    ///   Used to save what siblings are part of the reordering system to prevent reordering more than is necessary.
    /// </summary>
    private HashSet<Node> connectedSiblings = new();

    /// <summary>
    ///   Finds window reordering nodes in ancestors, looks for manual paths if set, otherwise uses automatic search to
    ///   find first one.
    /// </summary>
    /// <returns>
    ///   Pairs containing window reordering nodes and what sibling of theirs is an ancestor of this class.
    /// </returns>
    public static IEnumerable<(AddWindowReorderingSupportToSiblings Node, Node Sibling)> GetWindowReorderingAncestors(
        Node startingNode, int maxSearchDepth, Array<NodePath>? paths)
    {
        if (paths is null || paths.Count == 0)
        {
            foreach (var windowReorderingSupport in GetWindowReorderingAncestors(startingNode, maxSearchDepth))
            {
                yield return windowReorderingSupport;
            }
        }
        else
        {
            foreach (var windowReorderingSupport in GetWindowReorderingAncestors(startingNode, paths))
                yield return windowReorderingSupport;
        }
    }

    public override void _ExitTree()
    {
        windowReorderingAncestors.Clear();
        connectedWindows.Clear();
        connectedSiblings.Clear();
        isReady = false;
    }

    public void ConnectWindow(CustomWindow window, Node topNode, bool recursive)
    {
        // Make sure to be connected to other window reordering nodes before trying to connect the window
        Setup();

        if (recursive)
        {
            // Connection is recursive, connect to other window reordering nodes
            for (int i = 0; i < windowReorderingAncestors.Count; i++)
            {
                windowReorderingAncestors[i].Node.ConnectWindow(
                    window, windowReorderingAncestors[i].Sibling, recursive);
            }
        }

        window.Connect(nameof(Dragged), this, nameof(OnWindowReorder));

        connectedWindows.Add(window, topNode);
        connectedSiblings.Add(topNode);

        // Update top sibling
        if (topSibling is null || topSibling.GetIndex() < topNode.GetIndex())
            topSibling = topNode;
    }

    public void DisconnectWindow(CustomWindow window, bool recursive)
    {
        if (recursive)
        {
            // Connection was recursive, disconnect from other window reordering nodes
            for (int i = 0; i < windowReorderingAncestors.Count; i++)
            {
                windowReorderingAncestors[i].Node.DisconnectWindow(window, recursive);
            }
        }

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

    /// <summary>
    ///   Finds whose windows ancestor is currently on the top
    /// </summary>
    public void UpdateTopSibling()
    {
        if (topSibling is not null)
        {
            // Top sibling is already current
            return;
        }

        var siblings = parent.GetChildren();

        // Search for the first sibling that is part of the reordering system
        for (int i = siblings.Count - 1; i >= 0; i--)
        {
            var sibling = (Node)siblings[i];
            if (!connectedSiblings.Contains(sibling))
                continue;

            topSibling = sibling;
            break;
        }
    }

    /// <summary>
    ///   Reoders a window by setting its ancestor to the position of current top window, making it appear on top of
    ///   others.
    /// </summary>
    public void OnWindowReorder(CustomDialog window)
    {
        // Get a sibling that is an ancestor of this window
        Node targetSibling = connectedWindows[window];

        UpdateTopSibling();

        int topSiblingIndex = topSibling!.GetIndex();

        if (topSiblingIndex == targetSibling.GetIndex())
        {
            // This window is already on the top
            return;
        }

        // Put the sibling on the top
        parent.MoveChild(targetSibling, topSiblingIndex);

        topSibling = targetSibling;

        // For unexplained reasons this has to be here to update the order visually
        window.SetAsToplevel(false);
        window.SetAsToplevel(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var path in WindowReorderingPaths)
                path.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Used to find window reordering nodes automatically.
    /// </summary>
    /// <returns>
    ///   Pairs containing window reordering nodes and what sibling of theirs is an ancestor of this class.
    /// </returns>
    private static IEnumerable<(AddWindowReorderingSupportToSiblings Node, Node Sibling)> GetWindowReorderingAncestors(
        Node startingNode, int maxSearchDepth)
    {
        Node childOfAncestor = startingNode;
        Node ancestor = childOfAncestor.GetParent();

        for (int i = 0; i < maxSearchDepth; ++i)
        {
            if (ancestor is null)
                break;

            // Try to get the window reordering node
            var windowReorderingSupportNode = ancestor.GetNodeOrNull
                <AddWindowReorderingSupportToSiblings>(nameof(AddWindowReorderingSupportToSiblings));

            if (windowReorderingSupportNode != null)
            {
                // The window reordering node exists, return it
                yield return (windowReorderingSupportNode, childOfAncestor);

                // Stop searching
                break;
            }

            childOfAncestor = ancestor;
            ancestor = childOfAncestor.GetParent();
        }
    }

    /// <summary>
    ///   Used to find ancestors that are siblings of window reordering nodes set by manual paths.
    /// </summary>
    /// <returns>
    ///   Pairs containing window reordering nodes and what sibling of theirs is an ancestor of this class.
    /// </returns>
    private static IEnumerable<(AddWindowReorderingSupportToSiblings Node, Node Sibling)> GetWindowReorderingAncestors(
        Node startingNode, Array<NodePath> windowReorderingNodePaths)
    {
        Node childOfAncestor = startingNode;
        Node ancestor = childOfAncestor.GetParent();

        System.Collections.Generic.Dictionary
            <Node, AddWindowReorderingSupportToSiblings> windowReorderingNodesWithParents = new();

        // Get the window reordering nodes and their parents
        foreach (var path in windowReorderingNodePaths)
        {
            var windowReorderingNode = startingNode.GetNodeOrNull<AddWindowReorderingSupportToSiblings>(path);

            if (windowReorderingNode is null)
                continue;

            windowReorderingNodesWithParents.Add(
                windowReorderingNode.GetParent(), windowReorderingNode);
        }

        // Search for the ancestors that are parents of the window reordering nodes
        while (ancestor is not null)
        {
            if (windowReorderingNodesWithParents.TryGetValue(ancestor, out var windowReorderingNode))
            {
                windowReorderingNodesWithParents.Remove(ancestor);

                yield return (windowReorderingNode, childOfAncestor);

                if (windowReorderingNodesWithParents.Count == 0)
                {
                    // connected to every window reordering node, stop searching
                    break;
                }
            }

            childOfAncestor = ancestor;
            ancestor = childOfAncestor.GetParent();
        }
    }

    /// <summary>
    ///   This is used to setup a connection with other window reordering nodes so it knows to who it will connect the
    ///   window.
    /// </summary>
    private void Setup()
    {
        if (isReady)
            return;

        parent = GetParent();

        var windowReorderingAncestorsIEnumerable = GetWindowReorderingAncestors(parent,
            AutomaticWindowReorderingDepth, WindowReorderingPaths);

        foreach (var windowReorderingAncestor in windowReorderingAncestorsIEnumerable)
        {
            windowReorderingAncestor.Node.Setup();
            windowReorderingAncestors.Add(windowReorderingAncestor);
        }

        isReady = true;
    }
}
