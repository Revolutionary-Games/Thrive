namespace Tutorial;

using System;
using SharedBase.Archive;

public class MicrobeEngulfedExplanation : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "MicrobeEngulfedExplanation";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMicrobeEngulfedExplanation;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.EngulfedExplanationVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerIsEngulfed:
            {
                if (!HasBeenShown && !overallState.TutorialActive())
                    Show();

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

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        if (Time > Constants.HIDE_MICROBE_ENGULFED_TUTORIAL_AFTER)
        {
            Hide();
        }
    }
}
