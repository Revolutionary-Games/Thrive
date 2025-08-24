namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Prompts the player to place binding agents if they have a nucleus but not binding agents
/// </summary>
public class BindingAgentsTutorial : EditorEntryCountingTutorial
{
    private readonly string cellEditorTab = EditorTab.CellEditor.ToString();

    private readonly OrganelleDefinition nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
    private readonly OrganelleDefinition bindingAgents = SimulationParameters.Instance.GetOrganelleType("bindingAgent");

    [JsonProperty]
    private bool hasNucleus;

    [JsonProperty]
    private bool hasBindingAgents;

    public BindingAgentsTutorial()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => "BindingAgentsTutorial";

    protected override int TriggersOnNthEditorSession => 11;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.BindingAgentsTutorialVisible = ShownCurrently;
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

                var organelleName = eventArgs.Definition.InternalName;

                if (organelleName == nucleus.InternalName)
                {
                    hasNucleus = true;
                }

                if (organelleName == bindingAgents.InternalName)
                {
                    hasBindingAgents = true;

                    if (ShownCurrently)
                    {
                        GD.Print("Hiding tutorial");
                        Hide();
                    }
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

                    var organelleName = organellePlacementData.PlacedHex.Definition.InternalName;

                    if (organelleName == nucleus.InternalName)
                        hasNucleus = false;

                    if (organelleName == bindingAgents.InternalName)
                        hasBindingAgents = false;
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

                    var organelleName = organellePlacementData.PlacedHex.Definition.InternalName;

                    if (organelleName == nucleus.InternalName)
                        hasNucleus = true;

                    if (organelleName == bindingAgents.InternalName)
                        hasBindingAgents = true;
                }

                break;
            }

            case TutorialEventType.MicrobeEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                if (tab == cellEditorTab && hasNucleus && !hasBindingAgents && CanTrigger)
                {
                    GD.Print("Showing tutorial");
                    Show();
                }

                break;
            }
        }

        return false;
    }
}
