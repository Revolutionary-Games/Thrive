namespace Tutorial;

public class ResourcesAfterSplitTutorial : SwimmingAroundCountingTutorial
{
    public override string ClosedByName => "ResourcesAfterSplitTutorial";

    protected override int TriggersOnNthSwimmingSession => 5;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.ResourceSplitTutorialVisible = ShownCurrently;
    }

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        if (Time > Constants.HIDE_MICROBE_RESOURCE_SPLIT_TUTORIAL_AFTER)
        {
            Hide();
        }
    }
}
