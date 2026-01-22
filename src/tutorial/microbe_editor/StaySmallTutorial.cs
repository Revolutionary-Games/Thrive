namespace Tutorial;

using SharedBase.Archive;

public class StaySmallTutorial : CellEditorEntryCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "StaySmallTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialStaySmallTutorial;

    protected override int TriggersOnNthEditorSession => 4;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.StaySmallTutorialVisible = ShownCurrently;
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);
    }
}
