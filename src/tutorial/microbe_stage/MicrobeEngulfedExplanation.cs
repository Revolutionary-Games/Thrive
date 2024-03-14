namespace Tutorial;

using System;

public class MicrobeEngulfedExplanation : TutorialPhase
{
    public override string ClosedByName => "MicrobeEngulfedExplanation";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.EngulfedExplanationVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerIsEngulfed:
            {
                if (!HasBeenShown && !overallState.TutorialActive())
                    Show();

                break;
            }
        }

        return false;
    }

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        if (Time > Constants.HIDE_MICROBE_ENGULFED_TUTORIAL_AFTER)
        {
            Hide();
        }
    }
}
