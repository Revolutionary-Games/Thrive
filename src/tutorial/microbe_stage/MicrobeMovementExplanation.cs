namespace Tutorial;

using System;

/// <summary>
///   Explanation popup after the move key prompts have been visible for a while
/// </summary>
public class MicrobeMovementExplanation : TutorialPhase
{
    public override string ClosedByName => "MicrobeMovementExplain";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.MicrobeMovementPopupVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        return false;
    }
}
