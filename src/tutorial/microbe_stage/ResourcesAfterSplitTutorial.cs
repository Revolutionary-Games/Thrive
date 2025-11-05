namespace Tutorial;

using SharedBase.Archive;

public class ResourcesAfterSplitTutorial : SwimmingAroundCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "ResourcesAfterSplitTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialResourcesAfterSplitTutorial;

    protected override int TriggersOnNthSwimmingSession => 6;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.ResourceSplitTutorialVisible = ShownCurrently;
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);
    }

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        if (Time > Constants.HIDE_MICROBE_RESOURCE_SPLIT_TUTORIAL_AFTER)
        {
            Hide();
        }
    }
}
