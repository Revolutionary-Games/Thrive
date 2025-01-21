namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Tutorial pointing glucose collection out to the player
/// </summary>
public class GlucoseCollecting : TutorialPhase
{
    [JsonProperty]
    private Vector3? glucosePosition;

    /// <summary>
    ///   Holds the next tutorial we should notify we are done.
    /// </summary>
    [JsonProperty]
    private MicrobeReproduction? nextTutorial;

    public GlucoseCollecting()
    {
        UsesPlayerPositionGuidance = true;
        CanTrigger = false;
    }

    public override string ClosedByName => "GlucoseCollecting";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.GlucoseTutorialVisible = ShownCurrently;
    }

    public override void Hide()
    {
        // Whenever this is hidden we want to let the next tutorial know it can start
        if (ShownCurrently)
        {
            if (nextTutorial != null)
            {
                nextTutorial.ReportPreviousTutorialComplete();
                nextTutorial = null;
            }
        }

        base.Hide();
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerCompounds:
            {
                var compounds = ((CompoundBagEventArgs)args).Compounds;

                if (!HasBeenShown && !CanTrigger &&
                    compounds.GetCompoundAmount(Compound.Glucose) < compounds.GetCapacityForCompound(Compound.Glucose) -
                    Constants.GLUCOSE_TUTORIAL_TRIGGER_ENABLE_FREE_STORAGE_SPACE)
                {
                    CanTrigger = true;
                    return true;
                }

                break;
            }

            case TutorialEventType.MicrobeCompoundsNearPlayer:
            {
                var data = (EntityPositionEventArgs)args;

                if (!HasBeenShown && data.EntityPosition.HasValue && CanTrigger && !overallState.TutorialActive())
                {
                    nextTutorial = overallState.MicrobeReproduction;
                    Show();
                }

                if (ShownCurrently)
                {
                    glucosePosition = data.EntityPosition;
                    return true;
                }

                break;
            }

            case TutorialEventType.MicrobePlayerTotalCollected:
            {
                if (!ShownCurrently)
                    break;

                var compounds = ((CompoundEventArgs)args).Compounds;

                if (compounds.TryGetValue(Compound.Glucose, out var amount) &&
                    amount >= Constants.GLUCOSE_TUTORIAL_COLLECT_BEFORE_COMPLETE)
                {
                    // Tutorial is now complete
                    Hide();
                    return true;
                }

                break;
            }
        }

        return false;
    }

    public override Vector3? GetPositionGuidance()
    {
        return glucosePosition;
    }
}
