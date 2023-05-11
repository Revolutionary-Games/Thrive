using System;
using Godot;

/// <summary>
///   Thriveopedia page displaying fossilised (saved) organisms.
/// </summary>
public class ThriveopediaMuseumPage : ThriveopediaPage
{
    [Export]
    public NodePath CardContainerPath = null!;

    [Export]
    public NodePath WelcomeLabelPath = null!;

    [Export]
    public NodePath SpeciesPreviewContainerPath = null!;

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    [Export]
    public NodePath HexesPreviewPath = null!;

    [Export]
    public NodePath SpeciesDetailsLabelPath = null!;

    [Export]
    public NodePath LeaveGameConfirmationDialogPath = null!;

    [Export]
    public NodePath DeleteConfirmationDialogPath = null!;

    [Export]
    public NodePath DeletionFailedDialogPath = null!;

    private HFlowContainer cardContainer = null!;
    private Control welcomeLabel = null!;
    private VBoxContainer speciesPreviewContainer = null!;
    private SpeciesPreview speciesPreview = null!;
    private CellHexesPreview hexesPreview = null!;
    private CustomRichTextLabel speciesDetailsLabel = null!;
    private CustomConfirmationDialog leaveGameConfirmationDialog = null!;
    private CustomConfirmationDialog deleteConfirmationDialog = null!;
    private CustomConfirmationDialog deletionFailedDialog = null!;
    private PackedScene museumCardScene = null!;
    private MuseumCard? cardToBeDeleted;

    public override string PageName => "Museum";
    public override string TranslatedPageName => TranslationServer.Translate("THRIVEOPEDIA_MUSEUM_PAGE_TITLE");

    public override void _Ready()
    {
        base._Ready();

        cardContainer = GetNode<HFlowContainer>(CardContainerPath);
        welcomeLabel = GetNode<Control>(WelcomeLabelPath);
        speciesPreviewContainer = GetNode<VBoxContainer>(SpeciesPreviewContainerPath);
        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexesPreview = GetNode<CellHexesPreview>(HexesPreviewPath);
        speciesDetailsLabel = GetNode<CustomRichTextLabel>(SpeciesDetailsLabelPath);
        leaveGameConfirmationDialog = GetNode<CustomConfirmationDialog>(LeaveGameConfirmationDialogPath);
        deleteConfirmationDialog = GetNode<CustomConfirmationDialog>(DeleteConfirmationDialogPath);
        deletionFailedDialog = GetNode<CustomConfirmationDialog>(DeletionFailedDialogPath);

        museumCardScene = GD.Load<PackedScene>("res://src/thriveopedia/fossilisation/MuseumCard.tscn");
    }

    public override void OnThriveopediaOpened()
    {
        cardContainer.QueueFreeChildren();

        foreach (var speciesName in FossilisedSpecies.CreateListOfFossils(true))
        {
            var card = (MuseumCard)museumCardScene.Instance();

            var savedSpecies = FossilisedSpecies.LoadSpeciesFromFile(speciesName);

            card.FossilName = savedSpecies.Name;
            card.SavedSpecies = savedSpecies.Species;
            card.FossilPreviewImage = savedSpecies.PreviewImage;
            card.Connect(nameof(MuseumCard.OnSpeciesSelected), this, nameof(UpdateSpeciesPreview));
            card.Connect(nameof(MuseumCard.OnSpeciesDeleted), this, nameof(DeleteSpecies));
            cardContainer.AddChild(card);
        }
    }

    public override void UpdateCurrentWorldDetails()
    {
    }

    private void UpdateSpeciesPreview(MuseumCard card)
    {
        if (!speciesPreviewContainer.Visible)
        {
            welcomeLabel.Visible = false;
            speciesPreviewContainer.Visible = true;
        }

        // Deselect all other cards to prevent highlights hanging around.
        foreach (MuseumCard otherCard in cardContainer.GetChildren())
        {
            if (otherCard != card)
                otherCard.Pressed = false;
        }

        var species = card.SavedSpecies;

        if (species == null)
        {
            GD.PrintErr("Attempted to load a null species");
            return;
        }

        speciesPreview.PreviewSpecies = species;

        if (species is MicrobeSpecies microbeSpecies)
        {
            hexesPreview.PreviewSpecies = microbeSpecies;
        }
        else
        {
            GD.PrintErr("Unknown species type to preview: ", species);
        }

        UpdateSpeciesDetail(species);
    }

    private void UpdateSpeciesDetail(Species species)
    {
        speciesDetailsLabel.ExtendedBbcode = species.GetDetailString();
    }

    private void OnOpenInFreebuildPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (speciesPreview.PreviewSpecies == null)
            return;

        // If we're opening from a game in progress, warn the player
        if (CurrentGame != null)
        {
            leaveGameConfirmationDialog.DialogText = TranslationServer.Translate("OPEN_FOSSIL_IN_FREEBUILD_WARNING");
            leaveGameConfirmationDialog.PopupCenteredShrink();
            return;
        }

        if (speciesPreview.PreviewSpecies is not MicrobeSpecies)
        {
            GD.PrintErr("Loading non-microbe species is not yet implemented");
            return;
        }

        TransitionToFreebuild(speciesPreview.PreviewSpecies);
    }

    private void OnOpenInFreebuildConfirmPressed()
    {
        if (speciesPreview.PreviewSpecies == null)
            return;

        TransitionToFreebuild(speciesPreview.PreviewSpecies);
    }

    private void TransitionToFreebuild(Species startingSpecies)
    {
        EmitSignal(nameof(OnSceneChanged));

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, () =>
        {
            // Instantiate a new editor scene
            var editor = (MicrobeEditor)SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor).Instance();

            // Start freebuild game with the selected species
            editor.CurrentGame = GameProperties.StartNewMicrobeGame(
                new WorldGenerationSettings(), true, (Species)startingSpecies.Clone());

            // Switch to the editor scene
            SceneManager.Instance.SwitchToScene(editor);
        }, false);
    }

    private void DeleteSpecies(MuseumCard card)
    {
        cardToBeDeleted = card;

        deleteConfirmationDialog.PopupCenteredShrink();
    }

    private void OnConfirmDelete()
    {
        if (cardToBeDeleted == null)
        {
            GD.PrintErr("Museum card to confirm delete is null");
            return;
        }

        var fossilName = cardToBeDeleted.FossilName;

        if (fossilName == null)
        {
            GD.PrintErr("Attempted to delete a fossil with a null file name");
            return;
        }

        try
        {
            FossilisedSpecies.DeleteFossilFile(fossilName + Constants.FOSSIL_EXTENSION_WITH_DOT);
        }
        catch (Exception e)
        {
            deletionFailedDialog.PopupCenteredShrink();

            GD.PrintErr("Failed to delete fossil file: ", e);
            return;
        }

        // If the species we just deleted was being displayed in the sidebar
        if (speciesPreview.PreviewSpecies == cardToBeDeleted.SavedSpecies)
        {
            // Revert back to the welcome message
            welcomeLabel.Visible = true;
            speciesPreviewContainer.Visible = false;
        }

        cardToBeDeleted.DetachAndQueueFree();
        cardToBeDeleted = null;
    }
}
