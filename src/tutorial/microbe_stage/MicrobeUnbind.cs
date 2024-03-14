namespace Tutorial;

using System;

/// <summary>
///   Tells the player how unbinding works
/// </summary>
public class MicrobeUnbind : TutorialPhase
{
    public override string ClosedByName => "MicrobeUnbind";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.UnbindTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerUnbindEnabled:
            {
                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                    return true;
                }

                break;
            }

            case TutorialEventType.MicrobePlayerDied:
            {
                if (ShownCurrently)
                {
                    Hide();
                    HasBeenShown = false;
                }

                break;
            }

            case TutorialEventType.MicrobePlayerUnbound:
            {
                if (ShownCurrently)
                {
                    Hide();
                }

                break;
            }
        }

        return false;
    }
}
