namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Introduction to the familiar cell editor tab
/// </summary>
public class MulticellularCellEditorTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMulticellularCellEditor;

    public override string ClosedByName => "MulticellularCellEditorTutorial";

    public override void ApplyGUIState(MulticellularEditorTutorialGUI gui)
    {
        gui.MulticellularCellEditorTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        // This doesn't work as a cell type to edit might not be selected so the editor might not initialize.
        /*if (eventType == TutorialEventType.MulticellularEditorTabChanged)
        {
            var tab = ((StringEventArgs)args).Data;

            if (tab == cellEditorTab && CanTrigger && !overallState.TutorialActive())
            {
                Show();
            }
        }*/

        // Instead, we check for this event to know when the tab is actually usable.
        if (eventType == TutorialEventType.MulticellularCellTypeEditStarted)
        {
            if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
            {
                Show();
            }
        }

        if (eventType == TutorialEventType.MicrobeEditorOrganellePlaced)
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
