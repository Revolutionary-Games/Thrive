namespace Tutorial;

public class StaySmallTutorial : CellEditorEntryCountingTutorial
{
    public override string ClosedByName => "StaySmallTutorial";

    protected override int TriggersOnNthEditorSession => 4;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.StaySmallTutorialVisible = ShownCurrently;
    }
}
