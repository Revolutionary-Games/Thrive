using System;
using Godot;

/// <summary>
///   Utility for universally accessible Thriveopedia actions, such as opening a page by its name.
/// </summary>
public class ThriveopediaManager
{
    private static readonly ThriveopediaManager ManagerInstance = new();

    public static ThriveopediaManager Instance => ManagerInstance;

    /// <summary>
    ///   Action to open the Thriveopedia in the current game context, e.g. from the main menu or from the pause menu.
    /// </summary>
    public Action<string>? OpenCallback { get; set; }

    /// <summary>
    ///   Opens a page in the Thriveopedia via the appropriate menu context. Name must match the PageName property
    ///   of the desired page.
    /// </summary>
    public static void OpenPage(string pageName)
    {
        if (Instance.OpenCallback == null)
        {
            GD.PrintErr($"Attempted to open page {pageName} before Thriveopedia was initialised");
            return;
        }

        Instance.OpenCallback(pageName);
    }
}
