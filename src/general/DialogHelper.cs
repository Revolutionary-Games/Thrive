using System.Collections.Generic;
using System.Linq;
using Godot;

public class DialogHelper : NodeWithInput
{
    [RunOnKeyDown("ui_cancel", Priority = 1)]
    public bool OnEscapePressedInWindowDialog()
    {
        var nodes = new List<Node>();
        nodes.AddRange(GetTree().Root.GetChildren().OfType<Node>());

        while (nodes.Count != 0)
        {
            var currentNode = nodes[0];

            nodes.RemoveAt(0);

            if (currentNode is WindowDialog asDialog && asDialog.Visible)
            {
                asDialog.Hide();
                return true;
            }

            nodes.AddRange(currentNode.GetChildren().OfType<Node>());
        }

        return false;
    }
}
