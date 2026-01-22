namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Introduction to the cell editor
/// </summary>
public class CellEditorIntroduction : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string cellEditorTab = nameof(EditorTab.CellEditor);

    public override string ClosedByName => "CellEditorIntroduction";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialCellEditorIntroduction;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.CellEditorIntroductionVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorTabChanged:
            {
                if (!HasBeenShown && CanTrigger && ((StringEventArgs)args).Data == cellEditorTab)
                {
                    Show();
                }

                break;
            }

            case TutorialEventType.MicrobeEditorOrganellePlaced:
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
