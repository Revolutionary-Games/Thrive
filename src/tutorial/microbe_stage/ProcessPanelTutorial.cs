namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Tutorial explaining the process panel once it is opened
/// </summary>
public class ProcessPanelTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "ProcessPanelTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialProcessPanelTutorial;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.ProcessPanelTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.ProcessPanelOpened:
            {
                if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
                {
                    Show();
                }

                break;
            }

            case TutorialEventType.ProcessPanelProcessEnabled:
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

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);
    }
}
