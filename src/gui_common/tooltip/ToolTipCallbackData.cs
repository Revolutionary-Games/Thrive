using Godot;

/// <summary>
///   Helper class to contain callbacks for the custom tooltips to make them react to things.
/// </summary>
public class ToolTipCallbackData : Reference
{
    public ToolTipCallbackData(Control toolTipable, ICustomToolTip tooltip, bool autoUnregisterOnTreeExit)
    {
        ToolTipable = toolTipable;
        ToolTip = tooltip;
        AutoUnregisterOnTreeExit = autoUnregisterOnTreeExit;
    }

    public Control ToolTipable { get; private set; }

    public ICustomToolTip ToolTip { get; private set; }

    public bool AutoUnregisterOnTreeExit { get; set; }

    internal bool Unregistered { get; set; }

    public void OnMouseEnter()
    {
        ToolTipManager.Instance.MainToolTip = ToolTip;
        ToolTipManager.Instance.Display = true;
    }

    public void OnMouseExit()
    {
        // This used to always unset the main tooltip, but this now only unsets the tooltip if no one else had touched
        // it in the meantime
        if (ToolTipManager.Instance.MainToolTip == ToolTip)
        {
            ToolTipManager.Instance.MainToolTip = null;
            ToolTipManager.Instance.Display = false;
        }
    }

    public void OnExitingTree()
    {
        OnMouseExit();

        // To allow nodes with tooltips to exit and re-enter the scene, tooltips are now only unregistered by the code
        // that registered them. Or if using auto unregistering, then this is also done
        if (AutoUnregisterOnTreeExit)
            Unregister();
    }

    /// <summary>
    ///   Unregisters this tooltip data
    /// </summary>
    public void Unregister()
    {
        ToolTipable.UnRegisterToolTipForControl(ToolTip);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!Unregistered)
            {
                GD.PrintErr("A tooltip callback data was not properly unregistered before it was disposed");
            }
        }

        base.Dispose(disposing);
    }
}
