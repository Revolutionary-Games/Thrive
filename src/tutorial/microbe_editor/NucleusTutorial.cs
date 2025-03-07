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

    protected override int TriggersOnNthEditorSession { get; }

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.NucleusTutorialVisible = ShownCurrently;
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

                var isNucleus = eventArgs.Definition.InternalName == "Nucleus";

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
                var eventArgs = (UndoEventArgs)args;
                var combinedAction = (CombinedEditorAction)eventArgs.Action;

                foreach (var data in combinedAction.Data)
                {
                    if (data is OrganellePlacementActionData { PlacedHex.Definition.InternalName: "Nucleus" })
                    {
                        hasNucleus = false;
                    }
                }

                break;
            }

            case TutorialEventType.MicrobeEditorRedo:
            {
                var eventArgs = (RedoEventArgs)args;
                var combinedAction = (CombinedEditorAction)eventArgs.Action;

                foreach (var data in combinedAction.Data)
                {
                    if (data is OrganellePlacementActionData { PlacedHex.Definition.InternalName: "Nucleus" })
                    {
                        hasNucleus = true;
                    }
                }

                break;
            }

            case TutorialEventType.EnteredMicrobeEditor:
            {
                CanTrigger = NumberOfEditorEntries >= TriggersOnNthEditorSession;

                break;
            }

            case TutorialEventType.MicrobeEditorTabChanged:
            {
                if (!HasBeenShown && CanTrigger && ((StringEventArgs)args).Data == cellEditorTab && !hasNucleus)
                {
                    Show();
                }

                if ((ShownCurrently && ((StringEventArgs)args).Data != cellEditorTab) || hasNucleus)
                {
                    Hide();
                }

                break;
            }
        }

        return false;
    }
}
