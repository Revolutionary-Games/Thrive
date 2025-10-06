using System;

/// <summary>
///   Static class that holds information about currently activated cheats
/// </summary>
public static class CheatManager
{
    private static bool infiniteCompounds;
    private static bool godMode;
    private static bool noAI;
    private static bool unlimitedGrowthSpeed;
    private static bool lockTime;
    private static bool manuallySetTime;
    private static float speed;
    private static bool infiniteMP;
    private static bool moveToAnyPatch;

    static CheatManager()
    {
        DisableAllCheats();
    }

    public static event EventHandler<EventArgs>? OnHideCheatMenus;

    /// <summary>
    ///   Fired whenever the player uses the "Duplicate Player" cheat
    /// </summary>
    public static event EventHandler<EventArgs>? OnPlayerDuplicationCheatUsed;

    /// <summary>
    ///   Fired whenever the player uses the "Spawn Enemy" cheat
    /// </summary>
    public static event EventHandler<EventArgs>? OnSpawnEnemyCheatUsed;

    /// <summary>
    ///   Fired whenever the player uses the "Despawn All Entities" cheat
    /// </summary>
    public static event EventHandler<EventArgs>? OnDespawnAllEntitiesCheatUsed;

    /// <summary>
    ///   Fired whenever the player uses the "Reveal All Patches" cheat
    /// </summary>
    public static event EventHandler<EventArgs>? OnRevealAllPatches;

    /// <summary>
    ///   Fired whenever the player uses the "Unlock All Organelles" cheat
    /// </summary>
    public static event EventHandler<EventArgs>? OnUnlockAllOrganelles;

    /// <summary>
    ///   You automatically have 100% of all compounds
    /// </summary>
    public static bool InfiniteCompounds
    {
        get => infiniteCompounds;
        set
        {
            infiniteCompounds = value;

            if (value)
                AchievementsManager.ReportCheatsUsed();
        }
    }

    /// <summary>
    ///   You cannot take damage
    /// </summary>
    public static bool GodMode
    {
        get => godMode;
        set
        {
            godMode = value;

            if (value)
                AchievementsManager.ReportCheatsUsed();
        }
    }

    /// <summary>
    ///   Disables the AI
    /// </summary>
    public static bool NoAI
    {
        get => noAI;
        set
        {
            noAI = value;

            if (value)
                AchievementsManager.ReportCheatsUsed();
        }
    }

    /// <summary>
    ///   Disables limiting growth speed
    /// </summary>
    public static bool UnlimitedGrowthSpeed
    {
        get => unlimitedGrowthSpeed;
        set
        {
            unlimitedGrowthSpeed = value;

            if (value)
                AchievementsManager.ReportCheatsUsed();
        }
    }

    /// <summary>
    ///   Stops the time of day from changing
    /// </summary>
    public static bool LockTime
    {
        get => lockTime;
        set
        {
            lockTime = value;

            if (value)
                AchievementsManager.ReportCheatsUsed();
        }
    }

    /// <summary>
    ///   Time of day to be set if <see cref="ManuallySetTime"/> is enabled
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This setting doesn't do anything by itself, so this doesn't call
    ///     <see cref="AchievementsManager.ReportCheatsUsed"/>
    ///   </para>
    /// </remarks>
    public static float DayNightFraction { get; set; }

    /// <summary>
    ///   Manually sets the time to <see cref="DayNightFraction"/>
    /// </summary>
    public static bool ManuallySetTime
    {
        get => manuallySetTime;
        set
        {
            manuallySetTime = value;

            if (value)
                AchievementsManager.ReportCheatsUsed();
        }
    }

    /// <summary>
    ///   Speed modifier for the player
    /// </summary>
    public static float Speed
    {
        get => speed;
        set
        {
            speed = value;

            if (Math.Abs(value - 1) > 0.01f)
                AchievementsManager.ReportCheatsUsed();
        }
    }

    /// <summary>
    ///   Infinite MP in the editor.
    ///   Freebuild has infinite MP anyway regardless of this variable
    /// </summary>
    public static bool InfiniteMP
    {
        get => infiniteMP;
        set
        {
            infiniteMP = value;

            if (value)
                AchievementsManager.ReportCheatsUsed();
        }
    }

    /// <summary>
    ///   Can move to any patch in the editor.
    /// </summary>
    public static bool MoveToAnyPatch
    {
        get => moveToAnyPatch;
        set
        {
            moveToAnyPatch = value;

            if (value)
                AchievementsManager.ReportCheatsUsed();
        }
    }

    /// <summary>
    ///   Forces the player microbe to duplicate without going to the editor
    /// </summary>
    public static void PlayerDuplication()
    {
        OnPlayerDuplicationCheatUsed?.Invoke(null, EventArgs.Empty);
        AchievementsManager.ReportCheatsUsed();
    }

    /// <summary>
    ///   Spawns a random enemy
    /// </summary>
    public static void SpawnEnemy()
    {
        OnSpawnEnemyCheatUsed?.Invoke(null, EventArgs.Empty);
        AchievementsManager.ReportCheatsUsed();
    }

    public static void DespawnAllEntities()
    {
        OnDespawnAllEntitiesCheatUsed?.Invoke(null, EventArgs.Empty);
        AchievementsManager.ReportCheatsUsed();
    }

    public static void RevealAllPatches()
    {
        OnRevealAllPatches?.Invoke(null, EventArgs.Empty);
        AchievementsManager.ReportCheatsUsed();
    }

    public static void UnlockAllOrganelles()
    {
        OnUnlockAllOrganelles?.Invoke(null, EventArgs.Empty);
        AchievementsManager.ReportCheatsUsed();
    }

    public static void DisableAllCheats()
    {
        InfiniteCompounds = false;
        GodMode = false;
        NoAI = false;
        UnlimitedGrowthSpeed = false;
        LockTime = false;
        Speed = 1.0f;

        ManuallySetTime = false;
        DayNightFraction = 0.0f;

        InfiniteMP = false;
        MoveToAnyPatch = false;
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
