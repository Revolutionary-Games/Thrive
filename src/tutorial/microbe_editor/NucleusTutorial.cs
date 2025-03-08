namespace Tutorial;

using System;
using Newtonsoft.Json;

/// <summary>
///   Prompts the player to eventually place a nucleus for further progression
/// </summary>
public class NucleusTutorial : EditorEntryCountingTutorial
{
    private readonly string cellEditorTab = EditorTab.CellEditor.ToString();

    [JsonProperty]
    private bool hasNucleus;

    public NucleusTutorial()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => "NucleusTutorial";

    protected override int TriggersOnNthEditorSession => 2;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.NucleusTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (!hasNucleus)
        {
            if (base.CheckEvent(overallState, eventType, args, sender))
                return true;
        }

        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorOrganellePlaced:
            {
                if (args is not OrganellePlacedEventArgs eventArgs)
                {
                    break;
                }

                var isNucleus = eventArgs.Definition.InternalName == "nucleus";

                if (isNucleus)
                {
                    if (ShownCurrently)
                    {
                        Hide();
                    }

                    hasNucleus = true;
                }

                break;
            }

            case TutorialEventType.MicrobeEditorUndo:
            {
                var eventArgs = (EditorEventArgs)args;
                var combinedAction = (CombinedEditorAction)eventArgs.Action;

                foreach (var data in combinedAction.Data)
                {
                    if (data is OrganellePlacementActionData { PlacedHex.Definition.InternalName: "nucleus" })
                    {
                        hasNucleus = false;
                    }
                }

                break;
            }

            case TutorialEventType.MicrobeEditorRedo:
            {
                var eventArgs = (EditorEventArgs)args;
                var combinedAction = (CombinedEditorAction)eventArgs.Action;

                foreach (var data in combinedAction.Data)
                {
                    if (data is OrganellePlacementActionData { PlacedHex.Definition.InternalName: "nucleus" })
                    {
                        hasNucleus = true;
                    }
                }

                break;
            }
        }

        return false;
    }
}
