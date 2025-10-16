namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   A tutorial that counts how many times the editor has been entered and allows derived classes access to that
///   information
/// </summary>
public abstract class EditorEntryCountingTutorial : TutorialPhase
{
    protected EditorEntryCountingTutorial()
    {
        // Make this tutorial not trigger until the editor entry count is right
        CanTrigger = false;
    }

    public int NumberOfEditorEntries { get; set; }

    protected abstract int TriggersOnNthEditorSession { get; }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        CheckEventEditorEntryEvent(eventType);

        return false;
    }

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        base.WritePropertiesToArchive(writer);

        writer.Write(NumberOfEditorEntries);
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        base.ReadPropertiesFromArchive(reader, version);

        NumberOfEditorEntries = reader.ReadInt32();
    }

    private bool CheckEventEditorEntryEvent(TutorialEventType eventType)
    {
        if (eventType != TutorialEventType.EnteredMicrobeEditor)
            return false;

        if (!HasBeenShown)
        {
            ++NumberOfEditorEntries;
            CanTrigger = NumberOfEditorEntries >= TriggersOnNthEditorSession;
        }

        return true;
    }
}
