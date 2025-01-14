namespace Tutorial;

using System;

/// <summary>
///   Welcome to multicellular explaining budding to reduce the amount of complaints about it not working
/// </summary>
public class MulticellularWelcome : TutorialPhase
{
    public MulticellularWelcome()
    {
        Pauses = true;
    }

    public override string ClosedByName => "EarlyMulticellularWelcome";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.MulticellularWelcomeVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.EnteredMulticellularStage:
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
