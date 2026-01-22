namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Prompts the player to modify a placed organelle
/// </summary>
public class ModifyOrganelleTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => nameof(ModifyOrganelleTutorial);

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialModifyOrganelleTutorial;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.ModifyOrganelleTutorialVisible = ShownCurrently;
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

                var upgradable = organellePlacedEventArgs.Definition.AvailableUpgrades.Count > 0 ||
                    !string.IsNullOrEmpty(organellePlacedEventArgs.Definition.UpgradeGUI);

                if (!HasBeenShown && CanTrigger && upgradable && !overallState.TutorialActive())
                {
                    Show();
                }

                break;
            }

            // If undo is pressed, assume for now that it was the upgradable organelle, and close
            case TutorialEventType.EditorUndo:
            {
                if (ShownCurrently)
                {
                    Hide();
                }

                break;
            }

            case TutorialEventType.MicrobeEditorOrganelleModified:
            {
                if (ShownCurrently)
                {
                    Hide();
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
