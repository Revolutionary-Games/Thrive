namespace Tutorial;

using System;
using Newtonsoft.Json;

/// <summary>
///   Base for all tutorials that track how many times the player has come back from the editor
/// </summary>
public abstract class SwimmingAroundCountingTutorial : TutorialPhase
{
    protected SwimmingAroundCountingTutorial()
    {
        CanTrigger = false;
    }

    [JsonProperty]
    public int NumberOfMicrobeStageEntries { get; set; }

    protected abstract int TriggersOnNthSwimmingSession { get; }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.EnteredMicrobeStage:
            {
                if (!HasBeenShown)
                {
                    ++NumberOfMicrobeStageEntries;
                    CanTrigger = NumberOfMicrobeStageEntries >= TriggersOnNthSwimmingSession;

                    if (CanTrigger && !overallState.TutorialActive())
                    {
                        Show();
                    }
                }

                break;
            }
        }

        return false;
    }
}
