namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Prompts the player to place binding agents if they have a nucleus but not binding agents.
///
///   This inherits from EditorEntryCountingTutorial for the session counting
///   functionality, but does not use the session count itself to trigger.
///   Instead, it triggers a few sessions after the player places a nucleus but not binding agents.
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

    [JsonProperty]
    private int sessionNucleusPlaced;

    public BindingAgentsTutorial()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => "BindingAgentsTutorial";

    /// <summary>
    ///   This is purposefully set to 0, as this tutorial does not rely on session count to trigger.
    /// </summary>
    protected override int TriggersOnNthEditorSession => 0;

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
                    sessionNucleusPlaced = NumberOfEditorEntries;
                }

                if (organelleName == bindingAgents.InternalName)
                {
                    hasBindingAgents = true;

                    if (ShownCurrently)
                    {
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

                var shouldTrigger = NumberOfEditorEntries - sessionNucleusPlaced >=
                    Constants.TRIGGER_BINDING_AGENTS_TUTORIAL_AFTER_SESSIONS_WITH_NUCLEUS;

                if (tab == cellEditorTab && !hasBindingAgents && hasNucleus && shouldTrigger)
                {
                    Show();
                }

                break;
            }
        }

        return false;
    }
}
