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
                if (args is not OrganellePlacedEventArgs eventArgs)
                {
                    break;
                }

                var upgradable = eventArgs.Definition.AvailableUpgrades.Count > 0 ||
                    !string.IsNullOrEmpty(eventArgs.Definition.UpgradeGUI);

                if (!HasBeenShown && CanTrigger && upgradable)
                {
                    Show();
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
