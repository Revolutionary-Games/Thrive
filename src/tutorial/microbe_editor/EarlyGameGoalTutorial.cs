namespace Tutorial;

using System;
using Newtonsoft.Json;

public class EarlyGameGoalTutorial : TutorialPhase
{
    private const int TriggersOnNthEditorSession = 2;

    private readonly string reportTab = EditorTab.Report.ToString();

    public EarlyGameGoalTutorial()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => "EarlyGameGoalTutorial";

    [JsonProperty]
    private int EditorEntryCount { get; set; }

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.EarlyGameGoalVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.EnteredMicrobeEditor:
            {
                ++EditorEntryCount;

                CanTrigger = EditorEntryCount >= TriggersOnNthEditorSession;

                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                }

                break;
            }

            case TutorialEventType.MicrobeEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                // Hide when switched to another tab
                if (tab != reportTab)
                {
                    if (ShownCurrently)
                    {
                        Hide();
                    }
                }

                break;
            }
        }

        return false;
    }
}
