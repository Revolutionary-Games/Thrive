namespace Tutorial;

using System;

public class FoodChainTabTutorial : TutorialPhase
{
    private readonly string reportTab = EditorTab.Report.ToString();

    private readonly string foodChainTab = MicrobeEditorReportComponent.ReportSubtab.FoodChain.ToString();

    public override string ClosedByName => "FoodChainTabTutorial";

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.FoodChainTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                if (ShownCurrently && tab != reportTab)
                {
                    Hide();
                }

                break;
            }

            case TutorialEventType.ReportComponentSubtabChanged:
            {
                if (!HasBeenShown && CanTrigger && ((StringEventArgs)args).Data == foodChainTab &&
                    !overallState.TutorialActive())
                {
                    Show();
                }

                break;
            }
        }

        return false;
    }
}
