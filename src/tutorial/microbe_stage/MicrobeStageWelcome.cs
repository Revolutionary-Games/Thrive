namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   A welcome popup to the stage
/// </summary>
public class MicrobeStageWelcome : TutorialPhase, IArchiveUpdatable
{
    public const ushort SERIALIZATION_VERSION = 1;

    private Action? patchNamePopup;

    private WorldGenerationSettings.LifeOrigin gameLifeOrigin;

    private WorldGenerationSettings.LifeOrigin appliedGUILifeOrigin = WorldGenerationSettings.LifeOrigin.Vent;

    public MicrobeStageWelcome()
    {
        Pauses = true;
    }

    public override string ClosedByName => "MicrobeStageWelcome";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMicrobeStageWelcome;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        if (ShownCurrently && gameLifeOrigin != appliedGUILifeOrigin)
        {
            gui.SetWelcomeTextForLifeOrigin(gameLifeOrigin);
            appliedGUILifeOrigin = gameLifeOrigin;
        }

        gui.MicrobeWelcomeVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.EnteredMicrobeStage:
            {
                foreach (var eventArg in ((AggregateEventArgs)args).Args)
                {
                    if (eventArg is CallbackEventArgs callbackEventArgs)
                    {
                        patchNamePopup = callbackEventArgs.Data;
                    }
                    else if (eventArg is GameWorldEventArgs gameWorldEventArgs)
                    {
                        gameLifeOrigin = gameWorldEventArgs.World.WorldSettings.Origin;
                    }
                }

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
        patchNamePopup?.Invoke();
        base.Hide();
    }

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        base.WritePropertiesToArchive(writer);

        writer.Write((int)gameLifeOrigin);
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        // Base version is not our version
        base.ReadPropertiesFromArchive(reader, 1);

        gameLifeOrigin = (WorldGenerationSettings.LifeOrigin)reader.ReadInt32();
    }
}
