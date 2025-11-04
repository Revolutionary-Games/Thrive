namespace Tutorial;

using System;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Prompts the player to open the process panel
/// </summary>
public class OpenProcessPanelTutorial : SwimmingAroundCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    public OpenProcessPanelTutorial()
    {
        // This needs to pause to ensure the player doesn't die while this overlay is open
        Pauses = true;
    }

    public override string ClosedByName => "OpenProcessPanelTutorial";

    public Control? ProcessPanelButtonControl { get; set; }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialOpenProcessPanelTutorial;

    protected override int TriggersOnNthSwimmingSession => 2;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        if (gui.ProcessPanelButtonHighlight == null)
            throw new InvalidOperationException($"{nameof(gui.ProcessPanelButtonHighlight)} has not been set");

        gui.ProcessPanelButtonHighlight.TargetControl = ShownCurrently ? ProcessPanelButtonControl : null;

        gui.OpenProcessPanelTutorialVisible = ShownCurrently;
        gui.ProcessPanelButtonHighlight.Visible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.ProcessPanelOpened:
            {
                if (ShownCurrently)
                {
                    Hide();
                }
                else if (!HasBeenShown)
                {
                    CanTrigger = false;

                    // Permanently inhibit only if the process panel tutorial has been shown
                    if (overallState.ProcessPanelTutorial.HasBeenShown || overallState.ProcessPanelTutorial.Complete)
                    {
                        Inhibit();
                    }
                }

                break;
            }

            case TutorialEventType.ProcessPanelProcessEnabled:
            {
                // Another check for the player going through the process panel tutorial
                if (overallState.ProcessPanelTutorial.HasBeenShown)
                {
                    Inhibit();
                }

                break;
            }
        }

        return false;
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);
    }
}
