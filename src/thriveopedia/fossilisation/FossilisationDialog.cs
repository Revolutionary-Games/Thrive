using System;
using System.Text.RegularExpressions;
using Godot;

/// <summary>
///   Dialog for fossilising (saving) a given species.
/// </summary>
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

    [Export]
    public NodePath FossilisationFailedDialogPath = null!;

    private LineEdit speciesNameEdit = null!;
    private SpeciesPreview speciesPreview = null!;
    private CellHexesPreview hexesPreview = null!;
    private CustomRichTextLabel speciesDetailsLabel = null!;
    private Button fossiliseButton = null!;
    private CustomConfirmationDialog overwriteNameConfirmationDialog = null!;
    private CustomConfirmationDialog fossilisationFailedDialog = null!;

    /// <summary>
    ///   The species currently open in the dialog.
    /// </summary>
    private Species selectedSpecies = null!;

    /// <summary>
    ///   Name of the fossilised species. Separate to the species itself as the player can change it in the dialog.
    /// </summary>
    private string speciesName = null!;

    private bool saveQueued;

    /// <summary>
    ///   The species currently open in the dialog.
    /// </summary>
    public Species SelectedSpecies
    {
        get => selectedSpecies;
        set
        {
            selectedSpecies = value;

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
        fossilisationFailedDialog = GetNode<CustomConfirmationDialog>(FossilisationFailedDialogPath);

        // For saving a preview image of the species we need this preview object to keep hold of the raw rendered image
        speciesPreview.KeepPlainImageInMemory = true;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!Visible)
            return;

        // Trigger queued save if we didn't have the species image yet
        if (saveQueued)
            FossiliseSpecies();
    }

    /// <summary>
    ///   Updates the name of the species to be fossilised.
    /// </summary>
    /// <param name="name">The species' new name</param>
    public void SetNewName(string name)
    {
        speciesNameEdit.Text = name;

        // Call the callback manually because the function isn't called automatically here
        OnNameTextChanged(name);
    }

    public void ReportValidityOfName(bool valid)
    {
        if (valid)
        {
            GUICommon.MarkInputAsValid(speciesNameEdit);
            fossiliseButton.Disabled = false;

            speciesName = speciesNameEdit.Text;
        }
        else
        {
            GUICommon.MarkInputAsInvalid(speciesNameEdit);
            fossiliseButton.Disabled = true;
        }
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
        speciesDetailsLabel.ExtendedBbcode = SelectedSpecies.GetDetailString();
    }

    private void OnCancelPressed()
    {
        Hide();
    }

    private void OnFossilisePressed()
    {
        GD.Print("Saving species " + SelectedSpecies.FormattedName);

        if (FossilisedSpecies.IsSpeciesAlreadyFossilised(speciesName))
        {
            overwriteNameConfirmationDialog.DialogText =
                TranslationServer.Translate("OVERWRITE_SPECIES_NAME_CONFIRMATION");
            overwriteNameConfirmationDialog.PopupCenteredShrink();
            return;
        }

        FossiliseSpecies();
    }

    /// <summary>
    ///   Creates a new file containing the currently selected species and closes the dialog.
    /// </summary>
    private void FossiliseSpecies()
    {
        // We can't save before the species image is generated
        var previewImage = speciesPreview.GetFinishedImageIfReady();

        if (previewImage == null)
        {
            saveQueued = true;
            return;
        }

        saveQueued = false;

        // Resize the preview image if it is too large
        if (previewImage.GetHeight() > Constants.FOSSILISED_PREVIEW_IMAGE_HEIGHT ||
            previewImage.GetWidth() > 2 * Constants.FOSSILISED_PREVIEW_IMAGE_HEIGHT)
        {
            float aspectRatio = previewImage.GetWidth() / (float)previewImage.GetHeight();
            previewImage.Resize((int)(Constants.FOSSILISED_PREVIEW_IMAGE_HEIGHT * aspectRatio),
                Constants.FOSSILISED_PREVIEW_IMAGE_HEIGHT);
        }

        // Clone the species in case the player added a new name, as we don't want to rename the species in-game
        var species = (Species)selectedSpecies.Clone();
        species.UpdateNameIfValid(speciesName);

        FossilisedSpecies savedSpecies;
        switch (species)
        {
            case MicrobeSpecies microbeSpecies:
                savedSpecies = new FossilisedSpecies(
                    new FossilisedSpeciesInformation(FossilisedSpeciesInformation.SpeciesType.Microbe),
                    microbeSpecies,
                    speciesName);
                break;
            default:
                throw new InvalidOperationException($"Unable to fossilise type {species.GetType()}");
        }

        savedSpecies.PreviewImage = previewImage;

        try
        {
            savedSpecies.FossiliseToFile();
        }
        catch (Exception e)
        {
            fossilisationFailedDialog.PopupCenteredShrink();

            GD.PrintErr("Failed to save fossil file: ", e);
            return;
        }

        Hide();
    }
}
