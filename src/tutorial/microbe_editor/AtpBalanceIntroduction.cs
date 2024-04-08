namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Tells the player about the ATP balance bar functionality (must trigger before the negative ATP balance
///   tutorial will work)
/// </summary>
public class AtpBalanceIntroduction : EditorEntryCountingTutorial
{
    [JsonProperty]
    private bool shouldEnableNegativeATPTutorial;

    public override string ClosedByName => nameof(AtpBalanceIntroduction);

    [JsonIgnore]
    public Control? ATPBalanceBarControl { get; set; }

    protected override int TriggersOnNthEditorSession => 2;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        if (gui.AtpBalanceBarHighlight == null)
            throw new InvalidOperationException($"{nameof(gui.AtpBalanceBarHighlight)} has not been set");

        gui.AtpBalanceBarHighlight.TargetControl = ATPBalanceBarControl;

        gui.AtpBalanceIntroductionVisible = ShownCurrently;
        gui.HandleShowingATPBarHighlight();
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorPlayerEnergyBalanceChanged:
            {
                // This event is fine enough for detecting when the player changes something to highlight the
                // ATP balance bar, could be changed in the future to use organelle placement

                if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
                {
                    Show();
                    shouldEnableNegativeATPTutorial = true;

                    return true;
                }

                break;
            }

            case TutorialEventType.EnteredMicrobeEditor:
            {
                if (shouldEnableNegativeATPTutorial)
                {
                    overallState.NegativeAtpBalanceTutorial.CanTrigger = true;
                    shouldEnableNegativeATPTutorial = false;
                    HandlesEvents = false;
                }

                break;
            }
        }

        return false;
    }

    public override void Hide()
    {
        base.Hide();

        // This needs to be done so that this keeps getting the microbe enter events and can make the next
        // tutorial trigger
        HandlesEvents = true;
    }
}
