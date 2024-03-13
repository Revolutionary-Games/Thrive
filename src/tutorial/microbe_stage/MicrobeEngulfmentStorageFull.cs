namespace Tutorial;

using System;

public class MicrobeEngulfmentStorageFull : TutorialPhase
{
    public override string ClosedByName => "MicrobeEngulfmentStorageFull";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.EngulfmentFullCapacityVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerEngulfmentFull:
            {
                if (!HasBeenShown && !overallState.TutorialActive())
                    Show();

                break;
            }
        }

        return false;
    }
}
