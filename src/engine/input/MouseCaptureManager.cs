using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles the mouse capture state to allow multiple components to do it intelligently and also resets the capture
///   when not really wanted (for example return to menu)
/// </summary>
public static class MouseCaptureManager
{
    private static readonly HashSet<string> ActiveCapturePreventions = new();

    private static bool queuedUpdate;

    private static bool gameplayWantsCapture;
    private static bool mouseHideWanted;

    /// <summary>
    ///   True when the mouse is captured by Godot. Note that after calling methods in this class there's one frame
    ///   delay before the changes are reflected in this variable.
    /// </summary>
    public static bool Captured { get; private set; }

    public static void SetGameStateWantedCaptureState(bool captureWanted)
    {
        if (captureWanted == gameplayWantsCapture)
            return;

        gameplayWantsCapture = captureWanted;
        UpdateMouseCaptureState();
    }

    public static void SetMouseHideState(bool hidden)
    {
        if (mouseHideWanted == hidden)
            return;

        mouseHideWanted = hidden;
        UpdateMouseCaptureState();
    }

    /// <summary>
    ///   Reports that there's now a game element that wants to prevent mouse capture
    /// </summary>
    /// <param name="key">The key (should be unique)</param>
    public static void ReportOpenCapturePrevention(string key)
    {
        if (!ActiveCapturePreventions.Add(key))
            GD.PrintErr("Duplicate mouse capture prevention tried to be added: ", key);

        UpdateMouseCaptureState();
    }

    public static void ReportClosedCapturePrevention(string key)
    {
        if (!ActiveCapturePreventions.Remove(key))
        {
            GD.PrintErr("Mouse capture prevention ended but it was not even active: ", key);
        }

        UpdateMouseCaptureState();
    }

    /// <summary>
    ///   Called when returning to the menu or loading a save to force reset the mouse capture state
    /// </summary>
    public static void ForceDisableCapture()
    {
        if (ActiveCapturePreventions.Count > 0)
        {
            GD.Print($"Force clearing {ActiveCapturePreventions.Count} mouse capture preventions:");
            foreach (var capturePrevention in ActiveCapturePreventions)
            {
                GD.Print($" - {capturePrevention}");
            }

            ActiveCapturePreventions.Clear();
        }

        // Reset also the gameplay flags to get the mouse always fully visible
        gameplayWantsCapture = false;
        mouseHideWanted = false;

        UpdateMouseCaptureState();
    }

    /// <summary>
    ///   Queues mouse state update to run the next frame, ensures that multiple things happening on the same frame
    ///   will only apply all at once (for example if something releases an override another thing during the same
    ///   frame can put the override back on without it being off momentarily)
    /// </summary>
    private static void UpdateMouseCaptureState()
    {
        if (queuedUpdate)
        {
            return;
        }

        queuedUpdate = true;

        Invoke.Instance.Queue(DoMouseStateUpdate);
    }

    private static void DoMouseStateUpdate()
    {
        queuedUpdate = false;

        // TODO: if we use controller input mouse should be Input.MouseModeEnum.Hidden (after a few seconds)
        // See: https://github.com/Revolutionary-Games/Thrive/issues/4005

        // TODO: Confined might be nice mode for the strategy stages of the game for edge panning

        if (ActiveCapturePreventions.Count > 0)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            Captured = false;
            return;
        }

        if (gameplayWantsCapture)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            Captured = true;
            return;
        }

        if (mouseHideWanted)
        {
            Input.MouseMode = Input.MouseModeEnum.Hidden;
            Captured = false;
            return;
        }

        Input.MouseMode = Input.MouseModeEnum.Visible;
        Captured = false;
    }
}
