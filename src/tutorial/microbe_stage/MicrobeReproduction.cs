namespace Tutorial;

using System;

/// <summary>
///   Tells the player how to reproduce if they have taken a long while
/// </summary>
public class MicrobeReproduction : TutorialPhase
{
    public override string ClosedByName => "MicrobeReproduction";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.ReproductionTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerReadyToEdit:
                if (ShownCurrently)
                    Hide();

                break;
            case TutorialEventType.EnteredMicrobeEditor:
                if (ShownCurrently)
                    Hide();

                // Show this next time after the editor in case the player got to the editor early
                if (!HasBeenShown)
                    ReportPreviousTutorialComplete();

                break;
        }

        return false;
    }

    public override void Hide()
    {
        base.Hide();
        ProcessWhileHidden = false;
    }

    public void ReportPreviousTutorialComplete()
    {
        if (ProcessWhileHidden)
            return;

        ProcessWhileHidden = true;
        Time = 0;
    }

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        if (Time > Constants.MICROBE_REPRODUCTION_TUTORIAL_DELAY && !HasBeenShown && !overallState.TutorialActive())
        {
            Show();
        }
    }
}
