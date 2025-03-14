namespace Tutorial;

using System;

public class EarlyGameGoalTutorial : EditorEntryCountingTutorial
{
    private readonly string reportTab = EditorTab.Report.ToString();

    public override string ClosedByName => "EarlyGameGoalTutorial";

    protected override int TriggersOnNthEditorSession => 2;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.EarlyGameGoalVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return false;

        switch (eventType)
        {
            case TutorialEventType.EnteredMicrobeEditor:
            {
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
