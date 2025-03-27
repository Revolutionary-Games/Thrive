/// <summary>
///   Helper methods for tutorials
/// </summary>
public static class TutorialHelper
{
    public static void HandleCloseAllForGUI(ITutorialGUI gui)
    {
        if (gui.IsClosingAutomatically)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        gui.EventReceiver?.OnTutorialClosed();

        if (!gui.AllTutorialsDesiredState)
        {
            // TODO: restoring this based on some conditions? Would be pretty hard to make as this would need to
            // remember which tutorials were disabled due to this
            gui.EventReceiver?.OnCompleteTutorialsAlreadySeen();
        }
    }

    public static void HandleCloseSpecificForGUI(ITutorialGUI gui, string closedThing)
    {
        if (gui.IsClosingAutomatically)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        gui.EventReceiver?.OnCurrentTutorialClosed(closedThing);
    }

    /// <summary>
    ///   Handles processing for all tutorial GUI-derived classes.
    ///   This passes time to the TutorialState as the tutorial GUI Node shouldn't stop processing on pause
    /// </summary>
    public static void ProcessTutorialGUI(ITutorialGUI gui, float delta)
    {
        // Just to make sure this is reset properly
        gui.IsClosingAutomatically = false;

        // Let the attached tutorial controller do stuff
        gui.EventReceiver?.Process(gui, delta);
    }
}
