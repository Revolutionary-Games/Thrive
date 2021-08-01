using System;
using Godot;

public class CustomAcceptDialog : AcceptDialog
{
    protected bool isExclusive = true;

    protected bool isEscapeCloseable = true;

    private static int exclusiveDialogOpenCount;

    public static bool HasExclusiveDialogOpen { get => exclusiveDialogOpenCount > 0; }

    public void OnAboutToShow()
    {
        if (isExclusive)
            exclusiveDialogOpenCount++;
    }

    public void OnHide()
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
