namespace Tutorial;

using System;
using Newtonsoft.Json;

public class CheckTheHelpMenu : TutorialPhase
{
    public const string TUTORIAL_NAME = "CheckTheHelpMenu";

    private const int TriggersOnNthSwimmingSession = 3;

    public CheckTheHelpMenu()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => TUTORIAL_NAME;

    [JsonProperty]
    public int NumberOfMicrobeStageEntries { get; set; }

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.CheckTheHelpMenuVisible = ShownCurrently;
    }

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
                    if (NumberOfMicrobeStageEntries >= TriggersOnNthSwimmingSession &&
                        !overallState.TutorialActive())
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
