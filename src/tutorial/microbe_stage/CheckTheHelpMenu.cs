namespace Tutorial;

using SharedBase.Archive;

public class CheckTheHelpMenu : SwimmingAroundCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;
    public const string TUTORIAL_NAME = "CheckTheHelpMenu";

    public CheckTheHelpMenu()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => TUTORIAL_NAME;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialCheckTheHelpMenu;

    protected override int TriggersOnNthSwimmingSession => 7;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.CheckTheHelpMenuVisible = ShownCurrently;
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);
    }
}
