namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Welcome to multicellular explaining budding to reduce the number of complaints about it not working
/// </summary>
public class MulticellularWelcome : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public MulticellularWelcome()
    {
        Pauses = true;
    }

    public override string ClosedByName => "EarlyMulticellularWelcome";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMulticellularWelcome;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.MulticellularWelcomeVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.EnteredMulticellularStage:
            {
                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                    return true;
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
