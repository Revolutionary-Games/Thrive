namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Explains how to use the full cell body plan layout view
/// </summary>
public class CellBodyPlanLayoutTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string layoutTab = nameof(CellBodyPlanEditorComponent.SelectionMenuTab.Layout);

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMulticellularCellBodyPlanLayout;

    public override string ClosedByName => nameof(CellBodyPlanLayoutTutorial);

    public override void ApplyGUIState(MulticellularEditorTutorialGUI gui)
    {
        gui.CellBodyPlanLayoutTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (eventType == TutorialEventType.MulticellularBodyPlanEditorTabChanged &&
            ((StringEventArgs)args).Data == layoutTab &&
            !HasBeenShown && CanTrigger && !overallState.TutorialActive())
        {
            Show();
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
