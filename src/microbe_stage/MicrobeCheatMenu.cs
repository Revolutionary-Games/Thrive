/// <summary>
///   Handles the microbe cheat menu
/// </summary>
public class MicrobeCheatMenu : CheatMenu
{
    public void InfCompounds_toggled(bool button_pressed)
    {
        CheatManager.InfCompounds = button_pressed;
    }

    public void Godmode_toggled(bool button_pressed)
    {
        CheatManager.Godmode = button_pressed;
    }

    public void DisableAI_toggled(bool button_pressed)
    {
        CheatManager.NoAI = button_pressed;
    }

    public void SpeedSlider_value_changed(float value)
    {
        CheatManager.Speed = value;
    }
}
