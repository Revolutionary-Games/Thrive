namespace Tutorial;

using System;

/// <summary>
///   Tells the player how to stay alive
/// </summary>
public class MicrobeStayingAlive : TutorialPhase
{
    public override string ClosedByName => "MicrobeStayingAlive";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.StayingAliveVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerDied:
            {
                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                    return true;
                }

                break;
            }
        }

        return false;
    }

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        // Hide if elapsed too long
        if (Time > Constants.HIDE_MICROBE_STAYING_ALIVE_TUTORIAL_AFTER)
        {
            Hide();
        }
    }
}
