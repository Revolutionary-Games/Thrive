using System.Linq;
using System.Text.RegularExpressions;
using Godot;

public class FossilisationDialog : CustomDialog
{
    [Export]
    public NodePath NameEditPath = null!;

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    [Export]
    public NodePath HexPreviewPath = null!;

    [Export]
    public NodePath FossiliseButtonPath = null!;

    [Export]
    public NodePath SpeciesDetailsLabelPath = null!;

    [Export]
    public NodePath OverwriteNameConfirmationDialogPath = null!;

    private LineEdit speciesNameEdit = null!;
    private SpeciesPreview speciesPreview = null!;
    private CellHexesPreview hexesPreview = null!;
    private CustomRichTextLabel speciesDetailsLabel = null!;
    private Button fossiliseButton = null!;
    private CustomConfirmationDialog overwriteNameConfirmationDialog = null!;

    private Species selectedSpecies = null!;

    /// <summary>
    ///   True when one of our (name related) Controls is hovered. This needs to be known to know if a click happened
    ///   outside the name editing controls, for detecting when the name needs to be validated.
    /// </summary>
    private bool controlsHoveredOver;

    public Species SelectedSpecies
    {
        get => selectedSpecies;
        set
        {
            selectedSpecies = (Species)value.Clone();

            SetNewName(selectedSpecies.FormattedName);
            UpdateSpeciesPreview();
            UpdateSpeciesDetails();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        speciesNameEdit = GetNode<LineEdit>(NameEditPath);
        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexesPreview = GetNode<CellHexesPreview>(HexPreviewPath);
        speciesDetailsLabel = GetNode<CustomRichTextLabel>(SpeciesDetailsLabelPath);
        fossiliseButton = GetNode<Button>(FossiliseButtonPath);
        overwriteNameConfirmationDialog = GetNode<CustomConfirmationDialog>(OverwriteNameConfirmationDialogPath);
    }

    public void SetNewName(string name)
    {
        speciesNameEdit.Text = name;

        // Callback is manually called because the function isn't called automatically here
        OnNameTextChanged(name);
    }

    public void ReportValidityOfName(bool valid)
    {
        if (valid)
        {
            GUICommon.MarkInputAsValid(speciesNameEdit);
            fossiliseButton.Disabled = false;

            SelectedSpecies.UpdateNameIfValid(speciesNameEdit.Text);
        }
        else
        {
            GUICommon.MarkInputAsInvalid(speciesNameEdit);
            fossiliseButton.Disabled = true;
        }
    }

    public void OnClickedOffName()
    {
        var focused = GetFocusOwner();

        // Ignore if the species name line edit wasn't focused or if one of our controls is hovered
        if (focused != speciesNameEdit || controlsHoveredOver)
            return;

        PerformValidation(speciesNameEdit.Text);
    }

    private void OnNameTextChanged(string newText)
    {
        ReportValidityOfName(Regex.IsMatch(newText, Constants.SPECIES_NAME_REGEX));
    }

    private void OnNameTextEntered(string newText)
    {
        PerformValidation(newText);
    }

    private void PerformValidation(string text)
    {
        // Only defocus if the name is valid to indicate invalid namings to the player
        if (Regex.IsMatch(text, Constants.SPECIES_NAME_REGEX))
        {
            speciesNameEdit.ReleaseFocus();
        }
        else
        {
            // Prevents user from doing other actions with an invalid name
            GetTree().SetInputAsHandled();

            // TODO: Make the popup appear at the top of the line edit instead of at the last mouse position
            ToolTipManager.Instance.ShowPopup(TranslationServer.Translate("INVALID_SPECIES_NAME_POPUP"), 2.5f);

            speciesNameEdit.GetNode<AnimationPlayer>("AnimationPlayer").Play("invalidSpeciesNameFlash");
        }
    }

    private void OnRandomizeNamePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var nameGenerator = SimulationParameters.Instance.NameGenerator;
        var randomizedName = nameGenerator.GenerateNameSection() + " " +
            nameGenerator.GenerateNameSection(null, true);

        speciesNameEdit.Text = randomizedName;
        OnNameTextChanged(randomizedName);
    }

    private void UpdateSpeciesPreview()
    {
        speciesPreview.PreviewSpecies = SelectedSpecies;

        switch (SelectedSpecies)
        {
            case MicrobeSpecies microbeSpecies:
            {
                hexesPreview.PreviewSpecies = microbeSpecies;
                break;
            }
        }
    }

    private void UpdateSpeciesDetails()
    {
        speciesDetailsLabel.ExtendedBbcode = TranslationServer.Translate("SPECIES_DETAIL_TEXT").FormatSafe(
            SelectedSpecies.FormattedNameBbCode,
            SelectedSpecies.ID,
            SelectedSpecies.Generation,
            SelectedSpecies.Population,
            SelectedSpecies.Colour.ToHtml(),
            string.Join("\n  ", SelectedSpecies.Behaviour.Select(b => b.Key + ": " + b.Value)));

        switch (SelectedSpecies)
        {
            case MicrobeSpecies microbeSpecies:
            {
                speciesDetailsLabel.ExtendedBbcode += "\n" +
                    TranslationServer.Translate("MICROBE_SPECIES_DETAIL_TEXT").FormatSafe(
                        microbeSpecies.MembraneType.Name, microbeSpecies.MembraneRigidity,
                        microbeSpecies.BaseSpeed, microbeSpecies.BaseRotationSpeed, microbeSpecies.BaseHexSize);
                break;
            }
        }
    }

    private void OnCancelPressed()
    {
        Hide();
    }

    private void OnFossilisePressed()
    {
        GD.Print("Saving species " + SelectedSpecies.FormattedName);

        if (FossilisedSpecies.CreateListOfSaves()
            .Any(s => s == SelectedSpecies.FormattedName + Constants.FOSSIL_EXTENSION_WITH_DOT))
        {
            overwriteNameConfirmationDialog.DialogText =
                TranslationServer.Translate("OVERWRITE_SPECIES_NAME_CONFIRMATION");
            overwriteNameConfirmationDialog.PopupCenteredShrink();
            return;
        }

        FossiliseSpecies();
    }

    private void FossiliseSpecies()
    {
        var savedSpecies = new FossilisedSpecies { Name = SelectedSpecies.FormattedName, Species = SelectedSpecies };
        savedSpecies.SaveToFile();

        Hide();
    }

    private void OnControlMouseEntered()
    {
        controlsHoveredOver = true;
    }

    private void OnControlMouseExited()
    {
        controlsHoveredOver = false;
    }
}
