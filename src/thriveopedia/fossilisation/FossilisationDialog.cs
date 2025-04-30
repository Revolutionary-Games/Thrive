using System;
using System.Text.RegularExpressions;
using Godot;

/// <summary>
///   Dialog for fossilising (saving) a given species.
/// </summary>
public partial class FossilisationDialog : CustomWindow
{
#pragma warning disable CA2213
    [Export]
    private LineEdit speciesNameEdit = null!;

    [Export]
    private SpeciesDetailsPanel speciesDetailsPanel = null!;

    [Export]
    private Button fossiliseButton = null!;

    [Export]
    private CustomConfirmationDialog overwriteNameConfirmationDialog = null!;

    [Export]
    private CustomConfirmationDialog fossilisationFailedDialog = null!;

    private SpeciesPreview speciesPreview = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   The species currently open in the dialog.
    /// </summary>
    private Species selectedSpecies = null!;

    /// <summary>
    ///   Name of the fossilised species. Separate to the species itself as the player can change it in the dialog.
    /// </summary>
    private string speciesName = null!;

    private bool saveQueued;

    [Signal]
    public delegate void OnSpeciesFossilisedEventHandler();

    /// <summary>
    ///   The species currently open in the dialog.
    /// </summary>
    public Species SelectedSpecies
    {
        get => selectedSpecies;
        set
        {
            selectedSpecies = value;
            speciesDetailsPanel.PreviewSpecies = value;

            SetNewName(selectedSpecies.FormattedName);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        speciesPreview = speciesDetailsPanel.SpeciesPreview;
    }

    public override void _Process(double delta)
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
            GetViewport().SetInputAsHandled();

            // TODO: Make the popup appear at the top of the line edit instead of at the last mouse position
            ToolTipManager.Instance.ShowPopup(Localization.Translate("INVALID_SPECIES_NAME_POPUP"), 2.5f);

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
                Localization.Translate("OVERWRITE_SPECIES_NAME_CONFIRMATION");
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

        EmitSignal(SignalName.OnSpeciesFossilised);

        Hide();
    }
}
