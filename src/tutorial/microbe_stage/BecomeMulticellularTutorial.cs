namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Tells the player about the "Become multicellular" button
/// </summary>
public class BecomeMulticellularTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "BecomeMulticellularTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialBecomeMulticellularTutorial;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.BecomeMulticellularTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeBecomeMulticellularAvailable:
            {
                if (!Complete && !ProcessWhileHidden && CanTrigger)
                {
                    ProcessWhileHidden = true;
                    Time = 0;
                }

                break;
            }

            // Make sure the tutorial doesn't trigger after it is no longer relevant
            case TutorialEventType.EnteredMulticellularStage:
            case TutorialEventType.EnteredMulticellularEditor:
            {
                Inhibit();
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
        if (Time > Constants.OPEN_MICROBE_BECOME_MULTICELLULAR_TUTORIAL_AFTER && !HasBeenShown &&
            !overallState.TutorialActive())
        {
            Show();
        }
    }
}
