using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Info on a single endosymbiosis candidate
/// </summary>
public partial class EndosymbiosisCandidateOption : VBoxContainer
{
    private readonly List<(OrganelleDefinition Organelle, int Cost)> shownChoices = new();

    private readonly Dictionary<string, MicrobePartSelection> selectionButtons = new();

#pragma warning disable CA2213
    [Export]
    private Container organelleChoicesContainer = null!;

    [Export]
    private Label speciesNameLabel = null!;

    [Export]
    private SpeciesPreview speciesPreview = null!;

    [Export]
    private Button selectButton = null!;

    [Export]
    private LabelSettings noValidOrganellesFont = null!;
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

    public void UpdateChoices(List<(OrganelleDefinition Organelle, int Cost)> organelleChoices,
        PackedScene organelleSelectionScene)
    {
        shownChoices.Clear();
        organelleChoicesContainer.FreeChildren();
        selectionButtons.Clear();

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
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(100, 0),
                LabelSettings = noValidOrganellesFont,
            });

            return;
        }

        var buttonGroup = new ButtonGroup();

        foreach (var (organelle, cost) in organelleChoices)
        {
            if (shownChoices.Any(c => c.Organelle == organelle) || selectionButtons.ContainsKey(organelle.InternalName))
            {
                GD.PrintErr("Duplicate endosymbiosis choice: ", organelle.InternalName);
                continue;
            }

            shownChoices.Add((organelle, cost));

            var choice = organelleSelectionScene.Instantiate<MicrobePartSelection>();

            choice.SelectionGroup = buttonGroup;
            choice.PartName = organelle.UntranslatedName;
            choice.Name = organelle.InternalName;
            choice.PartIcon = organelle.LoadedIcon;

            // TODO: far in the future this might be nice to have its own icon
            choice.ShowMPIcon = false;
            choice.MPCost = cost;

            choice.Connect(MicrobePartSelection.SignalName.OnPartSelected,
                new Callable(this, nameof(OrganelleTypeSelected)));

            // TODO: tooltips. For some reason this doesn't work:
            // // Loan the editor tooltips for these as well
            // choice.RegisterToolTipForControl(organelle.InternalName, "organelleSelection");
            choice.AlwaysShowLabel = true;

            selectionButtons[organelle.InternalName] = choice;

            organelleChoicesContainer.AddChild(choice);
        }
    }

    private void OrganelleTypeSelected(string name)
    {
        // Deselect everything
        foreach (var partSelection in selectionButtons)
        {
            partSelection.Value.Selected = false;
        }

        foreach (var (organelle, cost) in shownChoices)
        {
            if (organelle.InternalName != name)
                continue;

            // Select the right button again
            var control = selectionButtons[name];
            control.Selected = true;

            selected = organelle;
            selectedCost = cost;
            selectButton.Disabled = false;

            // TODO: maybe this could be made to immediately trigger? Would require one less click but might be a bit
            // too confusing for the player to happen so suddenly.
            return;
        }

        GD.PrintErr("Selected unknown organelle type for endosymbiosis candidate");
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
