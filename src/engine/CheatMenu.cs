using System;
using Godot;

/// <summary>
///   Handles the opening, closing and operations of the cheat menus
/// </summary>
public abstract class CheatMenu : Popup
{
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
                Popup_();
            else
                Hide();
        }
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

    public override void _EnterTree()
    {
        InputManager.RegisterReceiver(this);
        CheatManager.OnHideCheatMenus += OnHideCheatMenus;
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);
        CheatManager.OnHideCheatMenus -= OnHideCheatMenus;
    }

    private void OnHideCheatMenus(object s, EventArgs e)
    {
        Hide();
    }
}
