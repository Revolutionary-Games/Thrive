namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Explains the digestion stat once something changes them
/// </summary>
public class DigestionStatTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly Enzyme rusticyanin = SimulationParameters.Instance.GetEnzyme("rusticyanin");

    public override string ClosedByName => "DigestionStatTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialDigestionStatTutorial;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.DigestionStatTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorOrganellePlaced:
            {
                if (args is not OrganellePlacedEventArgs organellePlacedEventArgs)
                    break;

                var digestionChanging = false;
                foreach (var enzyme in organellePlacedEventArgs.Definition.Enzymes.Keys)
                {
                    if (enzyme.InternalName == rusticyanin.InternalName)
                        continue;

                    digestionChanging = true;
                    break;
                }

                if (!HasBeenShown && CanTrigger && digestionChanging && !overallState.TutorialActive())
                {
                    Show();
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
