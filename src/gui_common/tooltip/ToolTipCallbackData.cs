using Godot;

/// <summary>
///   Helper class to contain callbacks for the custom tooltips to make them react to things.
/// </summary>
public class ToolTipCallbackData : Reference
{
    public ToolTipCallbackData(Control toolTipable, ICustomToolTip tooltip)
    {
        ToolTipable = toolTipable;
        ToolTip = tooltip;
    }

    public Control ToolTipable { get; private set; }

    public ICustomToolTip ToolTip { get; private set; }

    public void OnMouseEnter()
    {
        ToolTipManager.Instance.MainToolTip = ToolTip;
        ToolTipManager.Instance.Display = true;
    }

    public void OnMouseExit()
    {
        ToolTipManager.Instance.MainToolTip = null;
        ToolTipManager.Instance.Display = false;
    }

    public void OnExitingTree()
    {
        OnMouseExit();

        // This is to avoid premature unregistration
        if (!ToolTipable.IsReParenting())
        {
            // Control is exiting the tree due for deletion, valid for unregistration
            ToolTipable.UnRegisterToolTipForControl(ToolTip);
        }
    }
}
