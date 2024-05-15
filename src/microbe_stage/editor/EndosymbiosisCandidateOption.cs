using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Info on a single endosymbiosis candidate
/// </summary>
public partial class EndosymbiosisCandidateOption : VBoxContainer
{
    private readonly List<(OrganelleDefinition Organelle, int Cost)> shownChoices = new();

#pragma warning disable CA2213
    [Export]
    private Container organelleChoicesContainer = null!;

    [Export]
    private Label speciesNameLabel = null!;

    [Export]
    private SpeciesPreview speciesPreview = null!;

    [Export]
    private Button selectButton = null!;
#pragma warning restore CA2213

    private OrganelleDefinition? selected;
    private int selectedCost;

    [Signal]
    public delegate void OnOrganelleTypeSelectedEventHandler(string organelleType, int cost);

    public void SetSpecies(Species species)
    {
        speciesNameLabel.Text = species.FormattedName;
        speciesPreview.PreviewSpecies = species;
    }

    public void UpdateChoices(List<(OrganelleDefinition Organelle, int Cost)> organelleChoices)
    {
        shownChoices.Clear();

        // Disable start button until an organelle type is selected
        selectButton.Disabled = true;
        selected = null;

        if (organelleChoices.Count < 1)
        {
            organelleChoicesContainer.AddChild(new Label
            {
                Text = Localization.Translate("ENDOSYMBIOSIS_NO_CANDIDATE_ORGANELLES"),
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(100, 0),
            });
            return;
        }

        foreach (var (organelle, cost) in organelleChoices)
        {
            if (shownChoices.Any(c => c.Organelle == organelle))
            {
                GD.PrintErr("Duplicate endosymbiosis choice: ", organelle.InternalName);
                continue;
            }

            shownChoices.Add((organelle, cost));

            throw new System.NotImplementedException();
        }
    }

    private void SelectPressed()
    {
        if (selected == null)
        {
            GD.PrintErr("No organelle selected for endosymbiosis candidate");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(SignalName.OnOrganelleTypeSelected, selected.InternalName, selectedCost);
    }
}
