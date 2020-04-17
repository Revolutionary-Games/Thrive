using System;
using Godot;

public class PauseMenu : Control
{
    public void ResumePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        GetTree().Paused = false;
        Hide();
    }

    public void ExitPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        GetTree().Quit();
    }
}
