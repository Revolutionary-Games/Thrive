namespace Tutorial;

using System;

public class MicrobeSprintExplanation : TutorialPhase
{
    /// <summary>
    ///    Keep track of when the player starts sprinting. However the tutorial is only completed when the player stops sprinting.
    /// </summary>
    private bool startedSprinting = false;

    public override string ClosedByName => "MicrobeSprintExplanation";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.SprintExplanationVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            // Something is near by that you might want to catch
            case TutorialEventType.MicrobeChunksNearPlayer:
                {
                    var data = (EntityPositionEventArgs)args;

                    if (!HasBeenShown && data.EntityPosition.HasValue && CanTrigger && !overallState.TutorialActive())
                    {
                        Show();
                    }

                    if (ShownCurrently)
                    {
                        return true;
                    }

                    break;
                }

            case TutorialEventType.MicrobePlayerStartSprint:
                {
                    if (!ShownCurrently)
                        break;

                    // Set a flag, but only end the tutorial when the player has stopped sprinting
                    startedSprinting = true;
                    return true;
                }

            case TutorialEventType.MicrobePlayerEndSprint:
                {
                    if (!ShownCurrently || !startedSprinting)
                        break;

                    // Tutorial is now complete
                    Hide();
                    return true;
                }
        }

        return false;
    }
}
