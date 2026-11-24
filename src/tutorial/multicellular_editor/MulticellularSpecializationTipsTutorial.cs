namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Provides tips for increasing specialization in the multicellular editor
/// </summary>
public class MulticellularSpecializationTipsTutorial : MulticellularEditorEntryCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string cellEditorTab = nameof(EditorTab.CellEditor);

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMulticellularSpecializationTips;

    public override string ClosedByName => nameof(MulticellularSpecializationTipsTutorial);

    protected override int TriggersOnNthEditorSession => 7;

    public override void ApplyGUIState(MulticellularEditorTutorialGUI gui)
    {
        gui.SpecializationTipsTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return false;

        if (eventType == TutorialEventType.MulticellularEditorTabChanged)
        {
            var tab = ((StringEventArgs)args).Data;

            if (tab == cellEditorTab && CanTrigger && overallState.MulticellularCellCavityTutorial.Complete &&
                !overallState.TutorialActive())
            {
                Show();
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
