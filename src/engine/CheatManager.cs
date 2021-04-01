using System;

/// <summary>
///   Static class that holds infos about currently activated cheats
/// </summary>
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
    public static bool InfiniteCompounds { get; set; }

    /// <summary>
    ///   You cannot take damage
    /// </summary>
    public static bool GodMode { get; set; }

    /// <summary>
    ///   Disables the AI
    /// </summary>
    public static bool NoAI { get; set; }

    /// <summary>
    ///   Speed modifier for the player
    /// </summary>
    public static float Speed { get; set; }

    /// <summary>
    ///   Infinite MP in the editor. Obsolete in freebuild
    /// </summary>
    public static bool InfiniteMP { get; set; }

    public static void DisableAllCheats()
    {
        InfiniteCompounds = false;
        GodMode = false;
        NoAI = false;
        Speed = 1f;

        InfiniteMP = false;
    }

    public static void HideCheatMenus()
    {
        OnHideCheatMenus?.Invoke(null, new EventArgs());
    }
}
