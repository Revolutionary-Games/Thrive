/// <summary>
///   Handles the microbe editor cheat menu
/// </summary>
public class MicrobeEditorCheatMenu : CheatMenu
{
    public void SetInfiniteMP(bool value)
    {
        CheatManager.InfiniteMP = value;
    }
}
