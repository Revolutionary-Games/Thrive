using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Helper class for all dialog escapes to be handled here.
/// </summary>
public class DialogInputHelper : NodeWithInput
{
    private static bool isHelperLoaded;

    /// <summary>
    ///   If this is not the first helper in Tree, disable this helper.
    /// </summary>
    private bool isDuplicate;

    private List<WindowDialog> dialogs = new List<WindowDialog>();

    public override void _EnterTree()
    {
        isDuplicate = isHelperLoaded;
        if (!isHelperLoaded)
        {
            isHelperLoaded = true;
        }

        base._EnterTree();
    }

    public override void _ExitTree()
    {
        if (!isDuplicate)
        {
            isHelperLoaded = false;
        }

        base._ExitTree();
    }

    /// <summary>
    ///   When all the nodes are ready search for all dialog windows in the Tree.
    /// </summary>
    public override void _Ready()
    {
        if (!isDuplicate)
        {
            var nodes = new List<Node>();
            nodes.AddRange(GetTree().Root.GetChildren().OfType<Node>());

            while (nodes.Count != 0)
            {
                var currentNode = nodes[0];

                nodes.RemoveAt(0);

                if (currentNode is WindowDialog asDialog)
                {
                    // Skip ProcessPanel and CheatMenu and OrganellePopup
                    if (asDialog.Name == "ProcessPanel"
                        || asDialog.Name == "CheatMenu"
                        || asDialog.Name == "OrganellePopup")
                    {
                        continue;
                    }

                    dialogs.Add(asDialog);
                }

                nodes.AddRange(currentNode.GetChildren().OfType<Node>());
            }
        }

        base._Ready();
    }

    /// <summary>
    ///   When Escape is pressed check if any dialog window is open.
    /// </summary>
    [RunOnKeyDown("ui_cancel", Priority = Constants.DIALOG_WINDOW_CANCEL_PRIORITY)]
    public bool OnEscapePressedInWindowDialog()
    {
        if (isDuplicate)
        {
            return false;
        }

        foreach (var dialog in dialogs)
        {
            if (dialog.Visible)
            {
                dialog.Hide();
                return true;
            }
        }

        return false;
    }
}
