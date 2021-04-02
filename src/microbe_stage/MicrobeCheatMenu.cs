/// <summary>
///   Handles the microbe cheat menu
/// </summary>
public class MicrobeCheatMenu : CheatMenu
{
    public void SetInfiniteCompounds(bool value)
    {
        CheatManager.InfiniteCompounds = value;
    }

    public void SetGodMode(bool value)
    {
        CheatManager.GodMode = value;
    }

    public void SetDisableAI(bool value)
    {
        CheatManager.NoAI = value;
    }

    public void SetSpeed(float value)
    {
        CheatManager.Speed = value;
    }
}
