using System;
using Godot;

public class CustomDialog : WindowDialog
{
    protected static int exclusiveDialogOpenCount;

    protected bool isExclusive = true;

    protected bool isEscapeCloseable = true;

    public static bool HasExclusiveDialogOpen { get => exclusiveDialogOpenCount > 0; }

    public new void Show()
    {
        if (isExclusive)
            exclusiveDialogOpenCount++;

        base.Show();
    }

    public new void Hide()
    {
        if (exclusiveDialogOpenCount <= 0)
            throw new InvalidOperationException("Should not be any other open dialogs.");

        exclusiveDialogOpenCount--;
    }

    [RunOnKeyDown("ui_cancel")]
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
