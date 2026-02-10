namespace Tutorial;

using System;
using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   Second tutorial for the patch map screen
/// </summary>
public class MigrationTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string patchMapTab = nameof(EditorTab.PatchMap);
    private readonly string cellEditorTab = nameof(EditorTab.CellEditor);

    public MigrationTutorial()
    {
        CanTrigger = false;
    }

    [JsonIgnore]
    public override string ClosedByName => "MigrationTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMigrationTutorial;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.MigrationTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                if (!HasBeenShown && CanTrigger && tab == patchMapTab && !overallState.TutorialActive())
                {
                    Show();
                }

                if (ShownCurrently && tab == cellEditorTab)
                {
                    Hide();
                }

                break;
            }

            case TutorialEventType.EnteredMicrobeEditor:
            {
                CanTrigger = overallState.PatchMap.Complete;
                break;
            }

            case TutorialEventType.EditorMigrationCreated:
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
