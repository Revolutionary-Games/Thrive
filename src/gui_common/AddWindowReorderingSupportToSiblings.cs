using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

/// <summary>
///   Reorders windows by reordering their ancestors that are on the same level as this node.
/// </summary>
/// <remarks>
///   <para>
///     The windows are the ones who ask to establish the connection. If the window asks for a recursive
///     connection then the connection gets passed to window reordering nodes in ancestors of this node too.
///   </para>
///   <para>
///     IMPORTANT: Don't attach this class to any node, always use the existing scene with the same name as this class.
///     The reason for this is because the name of the node is used when establishing connections. This also means
///     that after adding the scene instance it may not be renamed.
///   </para>
///   <para>
///     WARNING: this node has to be in the hierarchy before any GUI nodes this is going to manage. Otherwise
///     unregistering errors will be triggered.
///   </para>
/// </remarks>
public partial class AddWindowReorderingSupportToSiblings : Control
{
    /// <summary>
    ///   Paths to window reordering nodes in ancestors.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This overrides automatic search.
    ///   </para>
    ///   <para>
    ///     NOTE: Changes take effect when this node enters a tree.
    ///   </para>
    /// </remarks>
    [Export]
    public Array<NodePath>? WindowReorderingPaths;

    /// <summary>
    ///   Tries to find first window reordering node in ancestors to connect to up to the specified depth.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Ignored when <see cref="WindowReorderingPaths"/> are not empty.
    ///   </para>
    ///   <para>
    ///     NOTE: Changes take effect when this node enters a tree.
    ///   </para>
    /// </remarks>
    [Export]
    public int AutomaticWindowReorderingDepth = 5;

    /// <summary>
    ///   Pairs containing window reordering nodes and what sibling of theirs is an ancestor of this class.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Used to pass the connections from windows along to recursive reorder nodes. This contains reorder nodes
    ///     that are our ancestors.
    ///   </para>
    /// </remarks>
    private readonly List<(AddWindowReorderingSupportToSiblings ReorderingNode, Node Sibling)>
        windowReorderingAncestors = new();

    /// <summary>
    ///   Used to save what node to reorder when a window requires to be reordered.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<CustomWindow, Node> connectedWindows = new();

    /// <summary>
    ///   Used to save what siblings are part of the reordering system to prevent reordering more than is necessary.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The reordering system is designed to not mess with nodes that are not part of the reordering system so that
    ///     this doesn't cause unintended problems.
    ///   </para>
    /// </remarks>
    private readonly HashSet<Node> connectedSiblings = new();

    /// <summary>
    ///   Used to save windows that are opened at once to preserve their order.
    /// </summary>
    private readonly List<CustomWindow> justOpenedWindows = new();

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

    private bool reorderOpenedWindowsQueued;

    /// <summary>
    ///   Finds window reordering nodes in ancestors, looks for manual paths if set, otherwise uses automatic search to
    ///   find first one.
    /// </summary>
    /// <param name="startingNode">
    ///   A node to start searching from. The search itself starts from the node's parent.
    ///   <see cref="reorderingNodePaths"/> must be relative to this node (or absolute).
    ///   If this node is a reordering node then search automatically skips one step to not return itself.
    /// </param>
    /// <param name="maxSearchDepth">
    ///   Specifies how deep to search. Ignored when <see cref="reorderingNodePaths"/> is not null or empty.
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
        base._ExitTree();

        connectionsEstablished = false;
        windowReorderingAncestors.Clear();
        connectedWindows.Clear();
        connectedSiblings.Clear();
        topSibling = null;
    }

    public void ConnectWindow(CustomWindow window, Node topNode, bool recursive)
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

        if (window.IsConnected(nameof(CustomWindow.DraggedEventHandler), new Callable(this, nameof(OnWindowReorder))))
        {
            // This window is already connected here
            GD.PrintErr($"A window {window.Name} ({window}) tried to connect to {Name} ({this}) multiple times");
            return;
        }

        if (connectedWindows.TryGetValue(window, out _))
        {
            GD.PrintErr($"A window {window.Name} ({window}) tried to connect to {Name} ({this}) " +
                "as duplicate reference");
            return;
        }

        window.Connect(nameof(CustomWindow.DraggedEventHandler), new Callable(this, nameof(OnWindowReorder)));

        var binds = new Array();
        binds.Add(window);
        window.Connect(nameof(TopLevelContainer.OpenedEventHandler), new Callable(this, nameof(OnWindowOpen)), binds);

        connectedWindows.Add(window, topNode);
        connectedSiblings.Add(topNode);

        // Update top sibling
        if (topSibling == null || topSibling.GetIndex() < topNode.GetIndex())
            topSibling = topNode;

#if DEBUG
        CheckThisNodeIsNotBelowRegistered(topNode);
#endif
    }

    public void DisconnectWindow(CustomWindow window, bool recursive)
    {
        if (recursive)
        {
            // Connection was recursive, disconnect from other window reordering nodes
            foreach (var ancestor in windowReorderingAncestors)
            {
                ancestor.ReorderingNode.DisconnectWindow(window, recursive);
            }
        }

        if (!window.IsConnected(nameof(CustomWindow.DraggedEventHandler), new Callable(this, nameof(OnWindowReorder))))
        {
            GD.PrintErr(
                $"A window {window.Name} ({window}) tried to disconnect from {Name} ({this}) but it wasn't connected");
            return;
        }

        window.Disconnect(nameof(CustomWindow.DraggedEventHandler), new Callable(this, nameof(OnWindowReorder)));
        window.Disconnect(nameof(TopLevelContainer.OpenedEventHandler), new Callable(this, nameof(OnWindowOpen)));

        if (!connectedWindows.TryGetValue(window, out var windowSibling))
        {
            GD.PrintErr(
                $"A window {window.Name} ({window}) tried to disconnect from {Name} ({this}) but it wasn't in " +
                "the connected window list. This may happen if the reorder node is not early enough in the node " +
                "hierarchy.");
            return;
        }

        connectedWindows.Remove(window);

        if (connectedWindows.All(w => w.Value != windowSibling))
        {
            // No other window has the same sibling so it can be removed
            connectedSiblings.Remove(windowSibling);

            if (topSibling == windowSibling)
                topSibling = null;
        }

        justOpenedWindows.Remove(window);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (WindowReorderingPaths != null)
            {
                foreach (var path in WindowReorderingPaths)
                    path.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Used to find window reordering nodes automatically.
    /// </summary>
    /// <returns>
    ///   Pairs containing window reordering nodes and what sibling of theirs is an ancestor of the starting node.
    /// </returns>
    private static IEnumerable<(AddWindowReorderingSupportToSiblings ReorderingNode, Node Sibling)>
        GetWindowReorderingAncestors(Node startingNode, int maxSearchDepth)
    {
        var childOfAncestor = startingNode;
        var ancestor = childOfAncestor.GetParent();

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
    ///   Pairs containing window reordering nodes and what sibling of theirs is an ancestor of the starting node.
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
                    $", reordering connection asked from {startingNode.Name} ({startingNode})");
            }

            var reorderingNode = startingNode.GetNodeOrNull<AddWindowReorderingSupportToSiblings>(path);

            if (reorderingNode == null)
            {
                GD.PrintErr($"Failed to find window reorder node at: {path}");
                continue;
            }

            var reorderingNodeParent = reorderingNode.GetParent();
            var reorderingNodeParentPath = reorderingNodeParent.GetPath();

            if (!startingNodePathString.StartsWith(reorderingNodeParentPath))
            {
                GD.PrintErr($"Path {path} not found in ancestors, reordering connection asked from" +
                    $" {startingNode.Name} ({startingNode})");
                continue;
            }

            // This finds the sibling that is part of the path from the starting node to the reorder node parent
            // Basically this finds the ancestor of the starting node that is in the reorderingNodeParent's children
            var siblingPath = startingNodePath.GetName(reorderingNodeParentPath.GetNameCount());
            var sibling = reorderingNodeParent.GetNode(siblingPath);

            yield return (reorderingNode, sibling);
        }
    }

    /// <summary>
    ///   Finds whose window's ancestor is currently on the top
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
            throw new Exception($"{Name} ({this}) tried update the top sibling, but wasn't able to find any");
    }

    /// <summary>
    ///   Reorders a window by setting its ancestor to the position of current top window, making it appear on top of
    ///   others.
    /// </summary>
    private void OnWindowReorder(CustomWindow window)
    {
        // Get a sibling that is an ancestor of this window
        var targetSibling = connectedWindows[window];

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
        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4349 fix this hack
        bool isSetAsToplevel = window.IsSetAsTopLevel();
        window.SetAsTopLevel(!isSetAsToplevel);
        window.SetAsTopLevel(isSetAsToplevel);
    }

    private void OnWindowOpen(CustomWindow window)
    {
        if (justOpenedWindows.Contains(window))
        {
            // This window is already queued to be opened
            return;
        }

        if (!reorderOpenedWindowsQueued)
        {
            // Tell the system that there is an opened window and wait in case more windows will be opened at once
            reorderOpenedWindowsQueued = true;
            Invoke.Instance.QueueForObject(ReorderOpenedWindows, this);
        }

        justOpenedWindows.Add(window);
    }

    private void ReorderOpenedWindows()
    {
        try
        {
            // Sort the windows to make sure they are updated in the right order
            justOpenedWindows.Sort((first, second) =>
            {
                return connectedWindows[first].GetIndex().CompareTo(connectedWindows[second].GetIndex());
            });
        }
        catch (Exception e)
        {
            GD.PrintErr($"Exception occurred in {Name} ({this}) in {nameof(ReorderOpenedWindows)}:\n{e}");

            // Remove invalid windows
            justOpenedWindows.RemoveAll(w => !IsInstanceValid(w) || !connectedWindows.ContainsKey(w));
        }

        // Reorder the windows
        foreach (CustomWindow window in justOpenedWindows)
        {
            OnWindowReorder(window);
        }

        justOpenedWindows.Clear();
        reorderOpenedWindowsQueued = false;
    }

    /// <summary>
    ///   This is used to setup a connection with other window reordering nodes so it knows to who it will connect the
    ///   windows that ask recursive connections.
    /// </summary>
    private void Setup()
    {
        if (connectionsEstablished)
            return;

        parent = GetParent();

        var foundAncestors = GetWindowReorderingAncestors(this,
            AutomaticWindowReorderingDepth, WindowReorderingPaths);

        foreach (var ancestor in foundAncestors)
        {
            ancestor.ReorderingNode.Setup();
            windowReorderingAncestors.Add(ancestor);
        }

        connectionsEstablished = true;
    }

    private void CheckThisNodeIsNotBelowRegistered(Node registeredNode)
    {
        if (GetIndex() >= registeredNode.GetIndex())
        {
            GD.PrintErr($"{nameof(AddWindowReorderingSupportToSiblings)} is higher index than a registered " +
                "window. The reordering node should be before any potential GUI nodes it needs to manage");
        }
    }
}
