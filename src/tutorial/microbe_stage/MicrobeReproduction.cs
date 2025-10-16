namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Tells the player how to reproduce if they have taken a long while
/// </summary>
public class MicrobeReproduction : TutorialPhase, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "MicrobeReproduction";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMicrobeReproduction;

    public bool CanBeReferencedInArchive => true;

    public static MicrobeReproduction ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MicrobeReproduction();
        instance.ReadPropertiesFromArchive(reader, version);
        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        WritePropertiesToArchive(writer);
    }

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.ReproductionTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerReadyToEdit:
                if (ShownCurrently)
                    Hide();

                break;
            case TutorialEventType.EnteredMicrobeEditor:
                if (ShownCurrently)
                    Hide();

                // Show this next time after the editor in case the player got to the editor early
                if (!HasBeenShown)
                    ReportPreviousTutorialComplete();

                break;
        }

        return false;
    }

    public override void Hide()
    {
        base.Hide();
        ProcessWhileHidden = false;
    }

    public void ReportPreviousTutorialComplete()
    {
        if (ProcessWhileHidden)
            return;

        ProcessWhileHidden = true;
        Time = 0;
    }

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        if (Time > Constants.MICROBE_REPRODUCTION_TUTORIAL_DELAY && !HasBeenShown && !overallState.TutorialActive())
        {
            Show();
        }
    }
}
