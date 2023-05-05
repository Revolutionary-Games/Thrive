using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using static CustomDialog;

/// <summary>
///   Reorders windows by reordering their ancestors that are on the same level as this node.
/// </summary>
/// <remarks>
///   <para>
///     The windows are the ones who ask to establish the connection. If the window asks for a recursive
///     connection then the connection gets passed to window reordering nodes in ancestors too.
///   </para>
///   <para>
///     IMPORTANT: Don't attach this class to any node, always use the existing scene with the same name as this class.
///     The reason for this is because the name of the node is used when establishing connections.
///   </para>
/// </remarks>
public class AddWindowReorderingSupportToSiblings : Control
{
    /// <summary>
    ///   Paths to window reordering nodes in ancestors.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This overrides automatic search.
    ///   </para>
    /// </remarks>
    [Export]
    public Array<NodePath> WindowReorderingPaths = new();

    /// <summary>
    ///   Tries to finds first window reordering node in ancestors to connect to
    ///   up to the specified depth.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Ignored when window reordering paths are not empty.
    ///   </para>
    /// </remarks>
    [Export]
    public int AutomaticWindowReorderingDepth = 5;

    /// <summary>
    ///   Pairs containing window reordering nodes and what sibling of theirs is an ancestor of this class.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Used to pass the connections from windows.
    ///   </para>
    /// </remarks>
    private readonly List<(AddWindowReorderingSupportToSiblings ReorderingNode, Node Sibling)>
        windowReorderingAncestors = new();

#pragma warning disable CA2213

    /// <summary>
    ///   A sibling that is an ancestor of a window that is currently on top of others.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Used to know if current window is already on the top.
    ///   </para>
    ///   <para>
    ///     If the window isn't on the top then the topSibling's index is used to know where to move the window's
    ///     ancestor in order for it to appear on the top without affecting other nodes that are not part of the
    ///     reordering system.
    ///   </para>
    /// </remarks>
    private Node? topSibling;

    private Node parent = null!;

#pragma warning restore CA2213

    private bool connectionsEstablished;

    /// <summary>
    ///   Used to save what node to reorder when a window requires to be reordered.
    /// </summary>
    private System.Collections.Generic.Dictionary<CustomDialog, Node> connectedWindows = new();

    /// <summary>
    ///   Used to save what siblings are part of the reordering system to prevent reordering more than is necessary.
    /// </summary>
    private HashSet<Node> connectedSiblings = new();

    /// <summary>
    ///   Finds window reordering nodes in ancestors, looks for manual paths if set, otherwise uses automatic search to
    ///   find first one.
    /// </summary>
    /// <param name="startingNode">
    ///   A node to start searching from. The search itself starts from the node's parent.
    ///   ReorderingNodePaths must be relative to this node.
    ///   If this node is reordering node then search automatically skips one step to not return itself.
    /// </param>
    /// <param name="maxSearchDepth">
    ///   Specifies how deep to search. Ignored when reorderingNodePaths is not null.
    /// </param>
    /// <param name="reorderingNodePaths">
    ///   Relative paths to window reordering nodes in ancestors. Tries to find every window reordering node
    ///   from the paths no matter how deep they are.
    /// </param>
    /// <returns>
    ///   Pairs containing window reordering nodes and what sibling of theirs is an ancestor of this class.
    /// </returns>
    public static IEnumerable<(AddWindowReorderingSupportToSiblings ReorderingNode, Node Sibling)>
        GetWindowReorderingAncestors(Node startingNode, int maxSearchDepth, Array<NodePath>? reorderingNodePaths)
    {
        if (reorderingNodePaths == null || reorderingNodePaths.Count == 0)
        {
            return GetWindowReorderingAncestors(startingNode, maxSearchDepth);
        }

        return GetWindowReorderingAncestors(startingNode, reorderingNodePaths);
    }

    public override void _ExitTree()
    {
        connectionsEstablished = false;
        windowReorderingAncestors.Clear();
        connectedWindows.Clear();
        connectedSiblings.Clear();
        topSibling = null;
    }

    public void ConnectWindow(CustomDialog window, Node topNode, bool recursive)
    {
        // Make sure to be connected to other window reordering nodes before trying to connect the window
        Setup();

        if (recursive)
        {
            // Connection is recursive, connect to other window reordering nodes
            foreach (var ancestor in windowReorderingAncestors)
            {
                ancestor.ReorderingNode.ConnectWindow(window, ancestor.Sibling, recursive);
            }
        }

        if (window.IsConnected(nameof(Dragged), this, nameof(OnWindowReorder)))
        {
            // This window is already connected here
            GD.PrintErr($"A window {window.Name}({window}) tried to connect to {Name}{this} multiple times");
            return;
        }

        window.Connect(nameof(Dragged), this, nameof(OnWindowReorder));
        window.Connect(nameof(Opened), this, nameof(OnWindowReorder));

        connectedWindows.Add(window, topNode);
        connectedSiblings.Add(topNode);

        // Update top sibling
        if (topSibling == null || topSibling.GetIndex() < topNode.GetIndex())
            topSibling = topNode;
    }

    public void DisconnectWindow(CustomDialog window, bool recursive)
    {
        if (recursive)
        {
            // Connection was recursive, disconnect from other window reordering nodes
            foreach (var ancestor in windowReorderingAncestors)
            {
                ancestor.ReorderingNode.DisconnectWindow(window, recursive);
            }
        }

        if (!window.IsConnected(nameof(Dragged), this, nameof(OnWindowReorder)))
        {
            // This window has already been disconnected from here
            GD.PrintErr($"A window {window.Name}({window}) tried to disconnect from {Name}{this} multiple times");
            return;
        }

        window.Disconnect(nameof(Dragged), this, nameof(OnWindowReorder));
        window.Disconnect(nameof(Opened), this, nameof(OnWindowReorder));

        var windowSibling = connectedWindows[window];
        connectedWindows.Remove(window);

        if (!connectedSiblings.Contains(windowSibling))
        {
            // No other window has the same sibling so it can be removed
            connectedSiblings.Remove(windowSibling);

            if (topSibling == windowSibling)
                topSibling = null;
        }
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
    private static IEnumerable<(AddWindowReorderingSupportToSiblings ReorderingNode, Node Sibling)>
        GetWindowReorderingAncestors(Node startingNode, int maxSearchDepth)
    {
        Node childOfAncestor = startingNode;
        Node ancestor = childOfAncestor.GetParent();

        if (startingNode is AddWindowReorderingSupportToSiblings)
        {
            // The node is reordering node, skip one step
            childOfAncestor = ancestor;
            ancestor = childOfAncestor.GetParent();
        }

        for (int i = 0; i < maxSearchDepth; ++i)
        {
            if (ancestor == null)
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
    private static IEnumerable<(AddWindowReorderingSupportToSiblings ReorderingNode, Node Sibling)>
        GetWindowReorderingAncestors(Node startingNode, Array<NodePath> reorderingNodePaths)
    {
        var startingNodePath = startingNode.GetPath();
        var startingNodePathString = startingNodePath.ToString();

        foreach (var path in reorderingNodePaths)
        {
            if (path.GetName(path.GetNameCount() - 1) != nameof(AddWindowReorderingSupportToSiblings))
            {
                GD.PrintErr($"Path {path} doesn't end with {nameof(AddWindowReorderingSupportToSiblings)}" +
                    $", reordering connection asked from {startingNode.Name}{startingNode}");
            }

            var reorderingNode = startingNode.GetNodeOrNull<AddWindowReorderingSupportToSiblings>(path);

            if (reorderingNode == null)
                continue;

            var reorderingNodeParent = reorderingNode.GetParent();
            var reorderingNodeParentPath = reorderingNodeParent.GetPath();

            if (!startingNodePathString.Contains(reorderingNodeParentPath))
            {
                GD.PrintErr($"Path {path} not found in ancestors, reordering connection asked from" +
                    $" {startingNode.Name}{startingNode}");
                continue;
            }

            var siblingPath = startingNodePath.GetName(reorderingNodeParentPath.GetNameCount());
            var sibling = reorderingNodeParent.GetNode(siblingPath);

            yield return (reorderingNode, sibling);
        }
    }

    /// <summary>
    ///   Finds whose windows ancestor is currently on the top
    /// </summary>
    private void UpdateTopSibling()
    {
        if (topSibling != null)
        {
            // Top sibling is already current
            return;
        }

        var childCount = parent.GetChildCount();

        // Search for the first sibling that is part of the reordering system
        for (int i = childCount - 1; i >= 0; --i)
        {
            var sibling = parent.GetChild(i);
            if (!connectedSiblings.Contains(sibling))
                continue;

            topSibling = sibling;
            break;
        }

        if (topSibling == null)
            throw new Exception($"{Name}({this}) tried update a top sibling, but wasn't able to find any");
    }

    /// <summary>
    ///   Reoders a window by setting its ancestor to the position of current top window, making it appear on top of
    ///   others.
    /// </summary>
    private void OnWindowReorder(CustomDialog window)
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
        bool isSetAsToplevel = window.IsSetAsToplevel();
        window.SetAsToplevel(!isSetAsToplevel);
        window.SetAsToplevel(isSetAsToplevel);
    }

    /// <summary>
    ///   This is used to setup a connection with other window reordering nodes so it knows to who it will connect the
    ///   window.
    /// </summary>
    private void Setup()
    {
        if (connectionsEstablished)
            return;

        parent = GetParent();

        var windowReorderingAncestorsIEnumerable = GetWindowReorderingAncestors(this,
            AutomaticWindowReorderingDepth, WindowReorderingPaths);

        foreach (var windowReorderingAncestor in windowReorderingAncestorsIEnumerable)
        {
            windowReorderingAncestor.ReorderingNode.Setup();
            windowReorderingAncestors.Add(windowReorderingAncestor);
        }

        connectionsEstablished = true;
    }
}
