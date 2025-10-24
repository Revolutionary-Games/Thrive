namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Tells the player how unbinding works
/// </summary>
public class MicrobeUnbind : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "MicrobeUnbind";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMicrobeUnbind;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.UnbindTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerUnbindEnabled:
            {
                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                    return true;
                }

                break;
            }

            case TutorialEventType.MicrobePlayerDied:
            {
                if (ShownCurrently)
                {
                    Hide();
                    HasBeenShown = false;
                }

                break;
            }

            case TutorialEventType.MicrobePlayerUnbound:
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
