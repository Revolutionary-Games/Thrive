namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Last words of the microbe editor tutorial (for the first editor cycle)
/// </summary>
public class EditorTutorialEnd : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public EditorTutorialEnd()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => "CellEditorClosingWords";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialEditorTutorialEnd;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.CellEditorClosingWordsVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.EditorRedo:
            {
                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                    return true;
                }

                break;
            }
        }

        return false;
    }

    public override void Hide()
    {
        if (ShownCurrently)
        {
            OnClosed?.Invoke();
        }

        base.Hide();
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);
    }
}
