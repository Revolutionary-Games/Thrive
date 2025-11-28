namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Tutorial for the patch map tab
/// </summary>
public class PatchMap : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string patchMapTab = nameof(EditorTab.PatchMap);
    private readonly string cellEditorTab = nameof(EditorTab.CellEditor);

    public override string ClosedByName => "PatchMap";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.TutorialPatchMap;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.PatchMapVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                if (!HasBeenShown && CanTrigger && tab == patchMapTab)
                {
                    Show();
                }

                if (ShownCurrently && tab == cellEditorTab)
                {
                    Hide();
                }

                break;
            }

            case TutorialEventType.MicrobeEditorPatchSelected:
            {
                if (ShownCurrently && ((PatchEventArgs)args).Patch != null)
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
