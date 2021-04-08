﻿using System;

/// <summary>
///   Static class that holds information about currently activated cheats
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
    ///   Infinite MP in the editor.
    ///   Freebuild has infinite MP anyway regardless of this variable
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
        OnHideCheatMenus?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    ///   Turns off all cheats and closes the cheat menus
    /// </summary>
    public static void OnCheatsDisabled()
    {
        DisableAllCheats();
        HideCheatMenus();
    }
}
