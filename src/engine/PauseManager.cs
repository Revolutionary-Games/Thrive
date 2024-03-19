using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles pausing and resuming the game based on named pause locks
/// </summary>
[GodotAutoload]
public partial class PauseManager : Node
{
    private static PauseManager? instance;

    private readonly HashSet<string> activeLocks = new();

    private bool isPaused;

    private PauseManager()
    {
        instance = this;
    }

    public static PauseManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    public bool Paused
    {
        get => isPaused;
        private set
        {
            if (isPaused != value)
            {
                isPaused = value;

                var tree = GetTree();

                // If the game was closed while paused from a dialog, we get the unpause signal after the node tree
                // is no longer usable
                if (tree == null)
                {
                    GD.PrintErr("PauseManager can't apply paused state as node tree doesn't exist");
                    return;
                }

                tree.Paused = value;
            }
        }
    }

    public void AddPause(string pauseLockName)
    {
        if (activeLocks.Add(pauseLockName))
        {
            ApplyPauseState();
        }
        else
        {
            GD.PrintErr("Duplicate pause lock was added: ", pauseLockName);
        }
    }

    public void Resume(string pauseLockName)
    {
        if (activeLocks.Remove(pauseLockName))
        {
            ApplyPauseState();
        }
        else
        {
            GD.PrintErr("Pause lock that was not created was removed: ", pauseLockName);
        }
    }

    public bool HasLock(string pauseLockName)
    {
        return activeLocks.Contains(pauseLockName);
    }

    /// <summary>
    ///   Force clears all locks. Should be called in the main menu or when switching directly from one in-progress
    ///   game to another. This is an extra safety measure against requiring the player to restart the game entirely
    ///   if they get a pause state stuck issue.
    /// </summary>
    public void ForceClear()
    {
        if (activeLocks.Count < 1)
            return;

        GD.PrintErr("Force clearing remaining pause locks: ", string.Join(", ", activeLocks));
        activeLocks.Clear();
        ApplyPauseState();
    }

    private void ApplyPauseState()
    {
        Paused = activeLocks.Count > 0;
    }
}
