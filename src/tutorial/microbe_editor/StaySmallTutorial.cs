namespace Tutorial;

public class StaySmallTutorial : EditorEntryCountingTutorial
{
    public StaySmallTutorial()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => "StaySmallTutorial";

    protected override int TriggersOnNthEditorSession => 3;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.StaySmallTutorialVisible = ShownCurrently;
    }
}
