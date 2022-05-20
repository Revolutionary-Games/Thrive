using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles pausing and resuming the game based on named pause locks
/// </summary>
public class PauseManager : Node
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
                GetTree().Paused = value;
                isPaused = value;
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

    /// <summary>
    ///   Force clears all locks. Should *only* be called in the main menu, where this is just an extra safety measure
    ///   against requiring the player to restart the game entirely if they get a pause state stuck issue
    /// </summary>
    public void ForceClear()
    {
        if (activeLocks.Count < 1)
            return;

        GD.Print("Force clearing remaining pause locks");
        activeLocks.Clear();
        ApplyPauseState();
    }

    private void ApplyPauseState()
    {
        Paused = activeLocks.Count > 0;
    }
}
