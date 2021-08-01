using Godot;
using System;

public class CustomConfirmationDialog : CustomAcceptDialog
{
    private Button cancelButton;

    public override void _EnterTree()
    {
        cancelButton = AddButton("Cancel");
        base._EnterTree();
    }

    public virtual void OnCustomAction(string buttonPressed)
    {
        if (buttonPressed == cancelButton.Name)
        {
            GUICommon.Instance.PlayButtonPressSound();
            Hide();
        }
    }
}
