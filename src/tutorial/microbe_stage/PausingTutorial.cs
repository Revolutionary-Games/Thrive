namespace Tutorial;

using System;

/// <summary>
///   A simple tutorial about pausing the game
/// </summary>
public class PausingTutorial : SwimmingAroundCountingTutorial
{
    public override string ClosedByName => "PausingTutorial";

    /// <summary>
    ///   Wants to trigger as soon as possible so that the player knows about pausing early on
    /// </summary>
    protected override int TriggersOnNthSwimmingSession => 1;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.PausingTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.GameResumedByPlayer:
            {
                if (ShownCurrently)
                {
                    Hide();
                }
                else if (!HasBeenShown)
                {
                    // Player has resumed the game so they don't need a pausing tutorial as they found the pause button
                    Inhibit();
                }

                break;
            }
        }

        return false;
    }
}
