﻿using System;
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
            {
                Show();
            }
            else
            {
                Hide();
            }
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

    public override void _Ready()
    {
        ReloadGUI();
        base._Ready();
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

    public void SetInfiniteMP(bool value)
    {
        CheatManager.InfiniteMP = value;
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

    public void SetSpeed(float value)
    {
        CheatManager.Speed = value;
    }

    /// <summary>
    ///   Applies the currently applied cheats to the GUI.
    /// </summary>
    public abstract void ReloadGUI();

    private void OnHideCheatMenus(object s, EventArgs e)
    {
        Hide();
    }
}
