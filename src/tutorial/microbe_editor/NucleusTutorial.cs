namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Prompts the player to eventually place a nucleus for further progression
/// </summary>
public class NucleusTutorial : EditorEntryCountingTutorial
{
    private readonly string cellEditorTab = EditorTab.CellEditor.ToString();

    private readonly OrganelleDefinition nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");

    [JsonProperty]
    private bool hasNucleus;

    public NucleusTutorial()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => "NucleusTutorial";

    protected override int TriggersOnNthEditorSession => 10;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.NucleusTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return false;

        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorOrganellePlaced:
            {
                if (args is not OrganellePlacedEventArgs eventArgs)
                {
                    GD.PrintErr("Organelle placed event has wrong argument type, this will break many tutorials!");
                    break;
                }

                var isNucleus = eventArgs.Definition.InternalName == nucleus.InternalName;

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

            case TutorialEventType.EditorUndo:
            {
                var editorActionEventArgs = (EditorActionEventArgs)args;

                foreach (var data in editorActionEventArgs.Action.Data)
                {
                    if (data is not OrganellePlacementActionData organellePlacementData)
                        continue;

                    if (organellePlacementData.PlacedHex.Definition.InternalName == nucleus.InternalName)
                    {
                        hasNucleus = false;
                    }
                }

                break;
            }

            case TutorialEventType.EditorRedo:
            {
                var editorActionEventArgs = (EditorActionEventArgs)args;

                foreach (var data in editorActionEventArgs.Action.Data)
                {
                    if (data is not OrganellePlacementActionData organellePlacementData)
                        continue;

                    if (organellePlacementData.PlacedHex.Definition.InternalName == nucleus.InternalName)
                    {
                        hasNucleus = true;
                    }
                }

                break;
            }

            case TutorialEventType.MicrobeEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                if (tab == cellEditorTab && !hasNucleus && CanTrigger)
                {
                    Show();
                }

                break;
            }
        }

        return false;
    }
}
