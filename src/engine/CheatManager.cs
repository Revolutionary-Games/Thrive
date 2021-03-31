using System;

public static class CheatManager
{
    static CheatManager()
    {
        DisableAllCheats();
    }

    public static event EventHandler<EventArgs> OnHideCheatMenus;

    /// <summary>
    ///   You automatically have 100% of all compounds
    /// </summary>
    public static bool InfCompounds { get; set; }

    /// <summary>
    ///   You cannot take damage
    /// </summary>
    public static bool Godmode { get; set; }

    /// <summary>
    ///   Disables the AI
    /// </summary>
    public static bool NoAI { get; set; }

    /// <summary>
    ///   Speed modifier for the player
    /// </summary>
    public static double Speed { get; set; }

    /// <summary>
    ///   Infinite MP in the editor. Obsolete in freebuild
    /// </summary>
    public static bool InfMP { get; set; }

    public static void DisableAllCheats()
    {
        InfCompounds = false;
        Godmode = false;
        NoAI = false;
        Speed = 1;

        InfMP = false;
    }

    public static void HideCheatMenus()
    {
        OnHideCheatMenus?.Invoke(null, new EventArgs());
    }
}
