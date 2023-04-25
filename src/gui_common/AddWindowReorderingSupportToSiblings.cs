using System.Collections.Generic;
using Godot;
using static CustomWindow;

public class AddWindowReorderingSupportToSiblings : Control
{
    [Export]
    public Godot.Collections.Array<NodePath> WindowReorderingSupportPaths = new();

    [Export]
    public bool ReorderNodes = true;

#pragma warning disable CA2213
    private Godot.Collections.Array<AddWindowReorderingSupportToSiblings> ancestorWindowReorderingNodes = new();
    private Godot.Collections.Array<Node> ancestorWindowReorderingNodeSiblings = new();

    private Node? topSibling = null;
    private Node parent = null!;

#pragma warning restore CA2213

    // used to save top nodes of windows
    private Dictionary<CustomWindow, Node> connectedWindows = new();
    private HashSet<Node> connectedSiblings = new();

    public override void _Ready()
    {
        parent = GetParent();

        for (int i = 0; i < ancestorWindowReorderingNodes.Count; i++)
        {
            // TODO : rewrite this to use WindowReorderingSupportPaths node instead of its sibling
            ancestorWindowReorderingNodeSiblings[i] = GetNode(WindowReorderingSupportPaths[i]);
            ancestorWindowReorderingNodes[i] = ancestorWindowReorderingNodeSiblings[i].GetParent().GetNode
                <AddWindowReorderingSupportToSiblings>(nameof(AddWindowReorderingSupportToSiblings));
        }
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

    public void ConnectWindow(CustomWindow window, Node topNode)
    {
        window.Connect(nameof(Dragged), this, nameof(OnWindowReorder));
        connectedWindows.Add(window, topNode);

        for (int i = 0; i < ancestorWindowReorderingNodes.Count; i++)
            ancestorWindowReorderingNodes[i].ConnectWindow(window, ancestorWindowReorderingNodeSiblings[i]);

        if (topSibling is null || topSibling.GetIndex() < topNode.GetIndex())
            topSibling = topNode;
    }

    public void UpdateTopSibling(Godot.Collections.Array siblings)
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

        // This window is already on the top
        if (topSiblingIndex == targetSiblingIndex)
            return;

        // Put the sibling on the top
        parent.MoveChild(targetSibling, topSiblingIndex);

        topSibling = targetSibling;

        // For unexplained reasons this has to be here to update the order visually
        for (int i = targetSiblingIndex+1; i <= topSiblingIndex; i++)
            parent.MoveChild(siblings[i] as Node, topSiblingIndex);
        parent.MoveChild(targetSibling, topSiblingIndex);
    }
}
