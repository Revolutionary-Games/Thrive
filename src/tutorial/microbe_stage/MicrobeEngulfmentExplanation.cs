namespace Tutorial;

using System;
using Godot;
using SharedBase.Archive;

public class MicrobeEngulfmentExplanation : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    private Vector3? chunkPosition;

    public MicrobeEngulfmentExplanation()
    {
        UsesPlayerPositionGuidance = true;
    }

    public override string ClosedByName => "MicrobeEngulfmentExplanation";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMicrobeEngulfmentExplanation;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.EngulfmentExplanationVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeChunksNearPlayer:
            {
                var data = (EntityPositionEventArgs)args;

                if (!HasBeenShown && data.EntityPosition.HasValue && CanTrigger && !overallState.TutorialActive())
                {
                    Show();
                }

                if (ShownCurrently)
                {
                    chunkPosition = data.EntityPosition;
                    return true;
                }

                break;
            }

            case TutorialEventType.MicrobePlayerEngulfing:
            {
                if (!ShownCurrently)
                    break;

                // Tutorial is now complete
                Hide();
                return true;
            }
        }

        return false;
    }

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        base.WritePropertiesToArchive(writer);

        if (chunkPosition != null)
        {
            writer.WriteAnyRegisteredValueAsObject(chunkPosition.Value);
        }
        else
        {
            writer.WriteNullObject();
        }
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base always uses version 1 currently
        base.ReadPropertiesFromArchive(reader, 1);

        chunkPosition = reader.ReadObject<Vector3>();
    }

    public override Vector3? GetPositionGuidance()
    {
        return chunkPosition;
    }
}
