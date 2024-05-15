using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   GUI for the player to manage endosymbiosis process
/// </summary>
public partial class EndosymbiosisPopup : CustomWindow
{
#pragma warning disable CA2213
    [Export]
    private Label generalExplanationLabel = null!;

    [Export]
    private Label inProgressAdviceLabel = null!;

    [Export]
    private Label prokaryoteFullLabel = null!;

    [Export]
    private Container choicesContainer = null!;

    [Export]
    private Container progressContainer = null!;

    private PackedScene candidateGUIScene = null!;
    private PackedScene organelleButtonScene = null!;
#pragma warning restore CA2213

    private EndosymbiosisData? endosymbiosisData;

    private bool limited;

    [Signal]
    public delegate void OnSpeciesSelectedEventHandler(int speciesId, string organelleType, int cost);

    public override void _Ready()
    {
        base._Ready();

        candidateGUIScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/EndosymbiosisCandidateOption.tscn");
        organelleButtonScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/MicrobePartSelection.tscn");
    }

    public void UpdateData(EndosymbiosisData endosymbiosis, bool isSpeciesProkaryote)
    {
        endosymbiosisData = endosymbiosis;

        var existingCount = endosymbiosis.Endosymbionts?.Count ?? 0;

        limited = isSpeciesProkaryote && existingCount >= Constants.ENDOSYMBIOSIS_MAX_FOR_PROKARYOTE;
        UpdateGUIState();
    }

    /// <summary>
    ///   Rebuilds the GUI state to show
    /// </summary>
    private void UpdateGUIState()
    {
        if (endosymbiosisData == null)
            throw new InvalidOperationException("No data set");

        // Reset old state
        generalExplanationLabel.Visible = false;
        inProgressAdviceLabel.Visible = false;
        prokaryoteFullLabel.Visible = false;

        choicesContainer.Visible = false;
        choicesContainer.QueueFreeChildren();

        progressContainer.Visible = false;
        progressContainer.QueueFreeChildren();

        if (endosymbiosisData.StartedEndosymbiosis != null)
        {
            inProgressAdviceLabel.Visible = true;
            ShowInProgressData(endosymbiosisData.StartedEndosymbiosis);
        }
        else if (limited)
        {
            prokaryoteFullLabel.Visible = true;
        }
        else
        {
            generalExplanationLabel.Visible = true;
            ShowDataToStartNew(endosymbiosisData.EngulfedSpecies);
        }
    }

    private void ShowInProgressData(EndosymbiosisData.InProgressEndosymbiosis startedData)
    {
        _ = startedData;
        throw new NotImplementedException();
    }

    private void ShowDataToStartNew(Dictionary<Species, int> candidates)
    {
        bool any = false;

        var tempSymbionts = new List<(OrganelleDefinition Organelle, int Cost)>();

        // Order the most engulfed things first
        foreach (var candidate in candidates.OrderByDescending(p => p.Value))
        {
            // Skip things that haven't been actually engulfed
            if (candidate.Value < 1)
                continue;

            // Also skip anything that is extinct to not show useless choices
            if (candidate.Key.Obsolete || candidate.Key.IsExtinct)
                continue;

            if (candidate.Key is not MicrobeSpecies microbeSpecies)
                continue;

            // TODO: could in the future show if a species is only present in a different patch than current one
            // to give some warning

            var choice = candidateGUIScene.Instantiate<EndosymbiosisCandidateOption>();

            MicrobeInternalCalculations.CalculatePossibleEndosymbiontsFromSpecies(microbeSpecies, tempSymbionts);

            // TODO: should engulfed things that have no potential organelles be shown at all? (they are shown now
            // with a text saying there's no candidate organelles)

            choice.SetSpecies(microbeSpecies);
            choice.UpdateChoices(tempSymbionts);
            tempSymbionts.Clear();

            choice.Connect(EndosymbiosisCandidateOption.SignalName.OnOrganelleTypeSelected,
                Callable.From((string name, int cost) => OnEndosymbiosisStarted(microbeSpecies, name, cost)));

            choicesContainer.AddChild(choice);

            any = true;
        }

        if (!any)
        {
            choicesContainer.AddChild(new Label
            {
                Text = Localization.Translate("ENDOSYMBIOSIS_NOTHING_ENGULFED"),
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(100, 0),
            });
        }

        choicesContainer.Visible = true;
    }

    private void OnEndosymbiosisStarted(Species species, string organelleName, int cost)
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(SignalName.OnSpeciesSelected, species.ID, organelleName, cost);

        // Don't close automatically to make it clearer what happened, the one handling the signal can close this if
        // desired
    }
}
