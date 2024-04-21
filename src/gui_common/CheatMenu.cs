using System;

/// <summary>
///   Handles the opening, closing and operations of the cheat menus
/// </summary>
[GodotAbstract]
public partial class CheatMenu : CustomWindow
{
    protected CheatMenu()
    {
    }

    /// <summary>
    ///   Whether the cheat menu may be opened or not
    /// </summary>
    public static bool CanOpenMenu => Settings.Instance.CheatsEnabled;

    /// <summary>
    ///   Current visibility of the cheat menu
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   Thrown when you try to open the cheat menu and <see cref="CanOpenMenu">CanOpen</see> is false
    /// </exception>
    public bool IsMenuOpen
    {
        get => Visible;
        set
        {
            // Is closed and cheats disabled
            if (!CanOpenMenu && value)
                throw new InvalidOperationException("Cheats must be enabled in the settings to open the cheat menu");

            if (value)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
    }

    public override void _Ready()
    {
        ReloadGUI();
        base._Ready();
    }

    public override void _EnterTree()
    {
        InputManager.RegisterReceiver(this);
        CheatManager.OnHideCheatMenus += OnHideCheatMenus;
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);
        CheatManager.OnHideCheatMenus -= OnHideCheatMenus;
        base._ExitTree();
    }

    [RunOnKeyDown("g_cheat_menu")]
    public bool ToggleCheatMenu()
    {
        // Is closed and cheats disabled
        if (!CanOpenMenu && !IsMenuOpen)
            return false;

        IsMenuOpen = !IsMenuOpen;
        return true;
    }

    public void SetInfiniteMP(bool value)
    {
        CheatManager.InfiniteMP = value;
    }

    public void SetMoveToAnyPatch(bool value)
    {
        CheatManager.MoveToAnyPatch = value;
    }

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

    public void SetLockTime(bool value)
    {
        CheatManager.LockTime = value;
    }

    public void SetDayNightFraction(float value)
    {
        CheatManager.DayNightFraction = value;
    }

    public void SetManuallySetTime(bool value)
    {
        CheatManager.ManuallySetTime = value;
    }

    public void SetSpeed(float value)
    {
        CheatManager.Speed = value;
    }

    /// <summary>
    ///   Applies the currently applied cheats to the GUI.
    /// </summary>
    public virtual void ReloadGUI()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    private void OnHideCheatMenus(object? s, EventArgs e)
    {
        Hide();
    }
}
