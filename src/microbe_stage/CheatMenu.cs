using System;
using Godot;

/// <summary>
///   Handles the opening, closing and operations of the cheat menu
/// </summary>
public class CheatMenu : ControlWithInput
{
    [Export]
    public NodePath InfCompoundsPath;

    [Export]
    public NodePath GodmodePath;

    private CheckBox infCompounds;
    private CheckBox godmode;

    public static CheatMenu Instance { get; private set; }

    /// <summary>
    ///   Whether the cheat menu may be opened or not
    /// </summary>
    public static bool CanOpenMenu => Settings.Instance.CheatsEnabled;

    /// <summary>
    ///   You automatically have 100% of all compounds
    /// </summary>
    public bool InfCompounds
    {
        get => infCompounds.Pressed;
        set => infCompounds.Pressed = value;
    }

    /// <summary>
    ///   You cannot take damage
    /// </summary>
    public bool Godmode
    {
        get => godmode.Pressed;
        set => godmode.Pressed = value;
    }

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

            Visible = value;
        }
    }

    public override void _Ready()
    {
        Instance = this;

        infCompounds = GetNode<CheckBox>(InfCompoundsPath);
        godmode = GetNode<CheckBox>(GodmodePath);

        IsMenuOpen = false;
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
}
