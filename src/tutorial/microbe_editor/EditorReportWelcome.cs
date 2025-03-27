namespace Tutorial;

using System;

/// <summary>
///   Welcome message and intro to the report tab
/// </summary>
public class EditorReportWelcome : EditorEntryCountingTutorial
{
    private readonly string reportTab = EditorTab.Report.ToString();

    public override string ClosedByName => "MicrobeEditorReport";

    /// <summary>
    ///   On the first time in the editor we go directly to the cell editor tab, so this tutorial triggers on the
    ///   second go of the editor
    /// </summary>
    protected override int TriggersOnNthEditorSession => 2;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.EditorEntryReportVisible = ShownCurrently;
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
