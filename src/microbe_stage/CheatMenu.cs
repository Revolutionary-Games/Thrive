using System;

/// <summary>
///   Handles the opening, closing and operations of the cheat menu
/// </summary>
public class CheatMenu : ControlWithInput
{
    /// <summary>
    ///   Whether the cheat menu may be opened or not
    /// </summary>
    public static bool CanOpen => Settings.Instance.CheatsEnabled;

    /// <summary>
    ///   Current visibility of the cheat menu
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   Thrown when you try to open the cheat menu and <see cref="CanOpen">CanOpen</see> is false
    /// </exception>
    public bool IsOpen
    {
        get => Visible;
        set
        {
            // Is closed and cheats disabled
            if (!CanOpen && value)
                throw new InvalidOperationException("Cheats must be enabled in the settings to open the cheat menu");

            Visible = value;
        }
    }

    public override void _Ready()
    {
        IsOpen = false;
        base._Ready();
    }

    [RunOnKeyDown("g_cheat_menu")]
    public bool ToggleCheatMenu()
    {
        // Is closed and cheats disabled
        if (!CanOpen && !IsOpen)
            return false;

        IsOpen = !IsOpen;
        return true;
    }
}
