namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Base for all tutorials that track how many times the player has come back from the editor
/// </summary>
public abstract class SwimmingAroundCountingTutorial : TutorialPhase
{
    protected SwimmingAroundCountingTutorial()
    {
        CanTrigger = false;
    }

    public int NumberOfMicrobeStageEntries { get; set; }

    protected abstract int TriggersOnNthSwimmingSession { get; }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.EnteredMicrobeStage:
            {
                if (!HasBeenShown)
                {
                    ++NumberOfMicrobeStageEntries;
                    CanTrigger = NumberOfMicrobeStageEntries >= TriggersOnNthSwimmingSession;

                    if (CanTrigger && !overallState.TutorialActive())
                    {
                        Show();
                    }
                }

                break;
            }
        }

        return false;
    }

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        base.WritePropertiesToArchive(writer);

        writer.Write(NumberOfMicrobeStageEntries);
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        base.ReadPropertiesFromArchive(reader, version);

        NumberOfMicrobeStageEntries = reader.ReadInt32();
    }
}
