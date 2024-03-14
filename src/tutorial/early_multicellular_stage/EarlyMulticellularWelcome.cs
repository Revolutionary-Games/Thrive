namespace Tutorial;

using System;

/// <summary>
///   Welcome to early multicellular explaining budding to reduce the amount of complaints about it not working
/// </summary>
public class EarlyMulticellularWelcome : TutorialPhase
{
    public EarlyMulticellularWelcome()
    {
        Pauses = true;
    }

    public override string ClosedByName => "EarlyMulticellularWelcome";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.EarlyMulticellularWelcomeVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.EnteredEarlyMulticellularStage:
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
}
