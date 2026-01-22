namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Informs the player where to place flagella and how they work
/// </summary>
public class FlagellumPlacementTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly OrganelleDefinition flagellum = SimulationParameters.Instance.GetOrganelleType("flagellum");

    public override string ClosedByName => "FlagellumPlacementTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialFlagellumPlacementTutorial;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.FlagellumPlacementTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState,
        TutorialEventType eventType, EventArgs args, object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorOrganellePlaced:
            {
                if (args is OrganellePlacedEventArgs organellePlacedArgs)
                {
                    if (organellePlacedArgs.Definition.InternalName != flagellum.InternalName)
                        break;

                    if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
                    {
                        Show();
                        return true;
                    }
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
