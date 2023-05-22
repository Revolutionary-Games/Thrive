using System;
using Godot;

public class DescendConfirmationDialog : CustomConfirmationDialog
{
    private void OnConfirmed()
    {
        GD.Print("Switching to new game setup to finish descending");
        throw new NotImplementedException();
    }
}
