/// <summary>
///   Handles the microbe editor cheat menu
/// </summary>
public class MicrobeEditorCheatMenu : CheatMenu
{
    public void InfMP_toggled(bool button_pressed)
    {
        CheatManager.InfMP = button_pressed;
    }
}
