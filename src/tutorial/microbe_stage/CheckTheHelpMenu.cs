﻿namespace Tutorial;

public class CheckTheHelpMenu : SwimmingAroundCountingTutorial
{
    public const string TUTORIAL_NAME = "CheckTheHelpMenu";

    public CheckTheHelpMenu()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => TUTORIAL_NAME;

    protected override int TriggersOnNthSwimmingSession => 7;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.CheckTheHelpMenuVisible = ShownCurrently;
    }
}
