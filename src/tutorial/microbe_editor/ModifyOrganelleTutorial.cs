namespace Tutorial;

using System;

/// <summary>
///   Prompts the player to modify a placed organelle
/// </summary>
public class ModifyOrganelleTutorial : TutorialPhase
{
    public override string ClosedByName => nameof(ModifyOrganelleTutorial);

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

                if (!HasBeenShown && CanTrigger && upgradable)
                {
                    Show();
                }

                break;
            }

            // If undo is pressed, assume for now that it was the upgradable organelle, and close
            case TutorialEventType.MicrobeEditorUndo:
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
}
