namespace Tutorial;

using System;

/// <summary>
///   Tutorial about opening the tolerances-tab. In case the player hasn't viewed it.
/// </summary>
public class OpenTolerancesTabTutorial : CellEditorEntryCountingTutorial
{
    private readonly string tolerancesTab = CellEditorComponent.SelectionMenuTab.Tolerance.ToString();

    public override string ClosedByName => "OpenTolerancesTabTutorial";

    protected override int TriggersOnNthEditorSession => 6;

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.CellEditorTabChanged:
            {
                // Make this tutorial not trigger once the player has opened the tolerances-tab, as this is just about
                // reminding to open it
                if ((!HasBeenShown || ShownCurrently) && ((StringEventArgs)args).Data == tolerancesTab)
                {
                    CanTrigger = false;
                    HasBeenShown = true;
                    Hide();
                }

                break;
            }
        }

        return false;
    }

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.OpenTolerancesTabTutorialVisible = ShownCurrently;
    }
}
