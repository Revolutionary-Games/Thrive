namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   A simple tutorial about pausing the game
/// </summary>
public class PausingTutorial : SwimmingAroundCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "PausingTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialPausingTutorial;

    /// <summary>
    ///   Wants to trigger as soon as possible so that the player knows about pausing early on
    /// </summary>
    protected override int TriggersOnNthSwimmingSession => 1;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.PausingTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.GameResumedByPlayer:
            {
                if (ShownCurrently)
                {
                    Hide();
                }
                else if (!HasBeenShown)
                {
                    // Player has resumed the game so they don't need a pausing tutorial as they found the pause button
                    Inhibit();
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
