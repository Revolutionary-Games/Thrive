using Godot;

/// <summary>
///   Helper class to contain the OnMouseEnter and OnMouseExit callback for the custom tooltips
/// </summary>
public class ToolTipCallbackData : Reference
{
    public ICustomToolTip ToolTip { get; private set; }

    public ToolTipCallbackData(ICustomToolTip tooltip)
    {
        ToolTip = tooltip;
    }

    public void OnMouseEnter()
    {
        ToolTipManager.Instance.MainToolTip = ToolTip;
        ToolTipManager.Instance.Display = true;
    }

    public void OnMouseExit()
    {
        ToolTipManager.Instance.Display = false;
    }
}
