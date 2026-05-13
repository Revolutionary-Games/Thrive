namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Explains the specialization mechanic of the multicellular editor
/// </summary>
public class MulticellularSpecializationTutorial : MulticellularEditorEntryCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string cellEditorTab = nameof(EditorTab.CellEditor);

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMulticellularSpecializationTutorial;

    public override string ClosedByName => "MulticellularSpecializationTutorial";

    protected override int TriggersOnNthEditorSession => 3;

    public override void ApplyGUIState(MulticellularEditorTutorialGUI gui)
    {
        gui.SpecializationTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return false;

        if (eventType == TutorialEventType.MulticellularEditorTabChanged)
        {
            var tab = ((StringEventArgs)args).Data;

            if (tab == cellEditorTab && CanTrigger)
            {
                Show();
            }
        }

        if (eventType == TutorialEventType.MulticellularEditorCellPlaced)
        {
            if (ShownCurrently)
            {
                Hide();
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
