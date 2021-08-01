using Godot;

/// <summary>
///   Custom dialog base.
/// </summary>
public class CustomAcceptDialog : AcceptDialog
{
    /// <summary>
    ///   Determine if to regard it as a model one.
    /// </summary>
    protected bool isExclusive = true;

    /// <summary>
    ///   Determine if to attach InputManager.
    /// </summary>
    protected bool isEscapeCloseable = true;

    private static int exclusiveDialogOpenCount;

    public static bool HasExclusiveDialogOpen => exclusiveDialogOpenCount > 0;

    public virtual void OnAboutToShow()
    {
        if (isExclusive)
            exclusiveDialogOpenCount++;
    }

    public virtual void OnHide()
    {
        if (isExclusive)
        {
            if (exclusiveDialogOpenCount <= 0)
            {
                GD.PrintErr("Should not exist any other open dialogs.");
                return;
            }

            exclusiveDialogOpenCount--;
        }
    }

    public virtual void OnConfirmed()
    {
        GUICommon.Instance.PlayButtonPressSound();
    }

    public override void _EnterTree()
    {
        InputManager.RegisterReceiver(this);
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);
        base._ExitTree();
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.WINDOW_DIALOG_CANCEL_PRIORITY)]
    public bool OnEscapePressed()
    {
        if (!Visible || !isEscapeCloseable)
        {
            return false;
        }

        Hide();
        return true;
    }
}
