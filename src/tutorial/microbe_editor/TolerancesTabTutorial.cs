namespace Tutorial;

using System;

public class TolerancesTabTutorial : TutorialPhase
{
    private readonly string tolerancesTab = CellEditorComponent.SelectionMenuTab.Tolerance.ToString();

    public override string ClosedByName => "TolerancesTabTutorial";

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.CellEditorTabChanged:
            {
                if (!HasBeenShown && CanTrigger && ((StringEventArgs)args).Data == tolerancesTab &&
                    !overallState.TutorialActive())
                {
                    Show();
                }

                break;
            }

            case TutorialEventType.MicrobeEditorTolerancesModified:
            {
                if (ShownCurrently)
                {
                    Hide();
                }

                break;
            }
        }

        return false;
    }

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.TolerancesTabTutorialVisible = ShownCurrently;
    }
}
