namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Explanation popup after the move key prompts have been visible for a while
/// </summary>
public class MicrobeMovementExplanation : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "MicrobeMovementExplain";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMicrobeMovementExplanation;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.MicrobeMovementPopupVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        return false;
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        base.ReadPropertiesFromArchive(reader, 1);
    }
}
