namespace Tutorial;

using System;
using SharedBase.Archive;

public class MicrobeEngulfmentStorageFull : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "MicrobeEngulfmentStorageFull";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMicrobeEngulfmentStorageFull;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.EngulfmentFullCapacityVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerEngulfmentFull:
            {
                if (!HasBeenShown && !overallState.TutorialActive())
                    Show();

                break;
            }

            case TutorialEventType.MicrobePlayerEngulfmentNotFull:
            {
                if (ShownCurrently && Time >= 15)
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

        base.ReadPropertiesFromArchive(reader, 1);
    }
}
