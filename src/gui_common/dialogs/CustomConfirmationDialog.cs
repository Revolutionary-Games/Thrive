using Godot;
using System;

public class CustomConfirmationDialog : CustomAcceptDialog
{
    private Button cancelButton;

    public override void _EnterTree()
    {
        cancelButton = AddButton("Cancel", true, nameof(cancelButton));
        base._EnterTree();
    }

    public virtual void OnCustomAction(string buttonPressed)
    {
        if (buttonPressed == nameof(cancelButton))
        {
            GUICommon.Instance.PlayButtonPressSound();
            Hide();
        }
    }
}
