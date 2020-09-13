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

        if (!gui.TutorialEnabledSelected)
        {
            gui.EventReceiver?.OnTutorialDisabled();
        }
    }

    public static void HandleCloseSpecificForGUI(ITutorialGUI gui, string closedThing)
    {
        if (gui.IsClosingAutomatically)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        gui.EventReceiver?.OnCurrentTutorialClosed(closedThing);
    }
}
