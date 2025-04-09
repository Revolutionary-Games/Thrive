namespace Tutorial;

using System;

public class SpeciesMemberDiedTutorial : TutorialPhase
{
    public override string ClosedByName => "SpeciesMemberDiedTutorial";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.SpeciesMemberDiedVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.PlayerSpeciesMemberDied:
            {
                if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
                {
                    Show();
                }

                break;
            }
        }

        return false;
    }

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        // Hide if elapsed too long
        if (Time > Constants.HIDE_MICROBE_SPECIES_MEMBER_DIED_AFTER)
        {
            Hide();
        }
    }
}
