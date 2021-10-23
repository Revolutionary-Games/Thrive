using System;

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
    ///   Fired whenever the user uses the "Duplicate Player" cheat
    /// </summary>
    public static event EventHandler<EventArgs> OnPlayerDuplicationCheatUsed;

    /// <summary>
    ///   Fired whenever the user uses the "Spawn Enemy" cheat
    /// </summary>
    public static event EventHandler<EventArgs> OnSpawnEnemyCheatUsed;

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

    /// <summary>
    ///   Forces the player microbe to duplicate without going to the editor
    /// </summary>
    public static void PlayerDuplication()
    {
        OnPlayerDuplicationCheatUsed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    ///   Spawns a random enemy
    /// </summary>
    public static void SpawnEnemy()
    {
        OnSpawnEnemyCheatUsed?.Invoke(null, EventArgs.Empty);
    }

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
