namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Tutorial about the compound balances display. Triggers eventually or when placing an organelle that warrants
///   knowing about this.
/// </summary>
public class CompoundBalancesTutorial : CellEditorEntryCountingTutorial
{
    private readonly OrganelleDefinition thylakoid =
        SimulationParameters.Instance.GetOrganelleType("chromatophore");

    private readonly OrganelleDefinition chemoProteins =
        SimulationParameters.Instance.GetOrganelleType("chemoSynthesizingProteins");

    public override string ClosedByName => "CompoundBalancesTutorial";

    [JsonIgnore]
    public Control? CompoundBalanceControl { get; set; }

    protected override int TriggersOnNthEditorSession => 7;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        if (gui.CompoundBalanceHighlight == null)
            throw new InvalidOperationException($"{nameof(gui.CompoundBalanceHighlight)} has not been set");

        gui.CompoundBalanceHighlight.TargetControl = CompoundBalanceControl;
        gui.CompoundBalanceHighlight.Visible = ShownCurrently;

        gui.CompoundBalanceTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorOrganellePlaced:
            {
                if (args is not OrganellePlacedEventArgs organellePlacedEventArgs)
                    break;

                // Trigger when placing an organelle that warrants explaining this early
                if (organellePlacedEventArgs.Definition == thylakoid ||
                    organellePlacedEventArgs.Definition == chemoProteins)
                {
                    if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
                        Show();
                }

                break;
            }
        }

        return false;
    }
}
