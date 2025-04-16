using System;
using System.Linq;
using Godot;

/// <summary>
///   Thriveopedia page displaying fossilised (saved) organisms.
/// </summary>
public partial class ThriveopediaMuseumPage : ThriveopediaPage, IThriveopediaPage
{
#pragma warning disable CA2213
    [Export]
    private HFlowContainer cardContainer = null!;
    [Export]
    private Control welcomeLabel = null!;
    [Export]
    private VBoxContainer speciesPreviewContainer = null!;
    [Export]
    private SpeciesDetailsPanel speciesPreviewPanel = null!;
    [Export]
    private CustomConfirmationDialog leaveGameConfirmationDialog = null!;
    [Export]
    private CustomConfirmationDialog fossilDirectoryWarningBox = null!;
    [Export]
    private CustomConfirmationDialog deleteConfirmationDialog = null!;
    [Export]
    private CustomConfirmationDialog deletionFailedDialog = null!;
    private PackedScene museumCardScene = null!;

    private MuseumCard? cardToBeDeleted;
#pragma warning restore CA2213

    public string PageName => "Museum";
    public string TranslatedPageName => Localization.Translate("THRIVEOPEDIA_MUSEUM_PAGE_TITLE");

    public string? ParentPageName => null;

    public override void _Ready()
    {
        base._Ready();

        museumCardScene = GD.Load<PackedScene>("res://src/thriveopedia/fossilisation/MuseumCard.tscn");
    }

    public override void OnThriveopediaOpened()
    {
        cardContainer.QueueFreeChildren();

        foreach (var speciesName in FossilisedSpecies.CreateListOfFossils(true))
        {
            var savedSpecies = FossilisedSpecies.LoadSpeciesFromFile(speciesName);

            // Don't add cards for corrupt fossils
            if (savedSpecies == null)
                continue;

            var card = museumCardScene.Instantiate<MuseumCard>();
            card.FossilName = savedSpecies.Name;
            card.SavedSpecies = savedSpecies.Species;
            card.FossilPreviewImage = savedSpecies.PreviewImage;

            card.Connect(MuseumCard.SignalName.OnSpeciesSelected,
                new Callable(this, nameof(UpdateSpeciesPreview)));
            card.Connect(MuseumCard.SignalName.OnSpeciesDeleted, new Callable(this, nameof(DeleteSpecies)));

            cardContainer.AddChild(card);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            {
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateSpeciesPreview(MuseumCard card)
    {
        if (!speciesPreviewContainer.Visible)
        {
            welcomeLabel.Visible = false;
            speciesPreviewContainer.Visible = true;
        }

        // Deselect all other cards to prevent highlights hanging around.
        foreach (var otherCard in cardContainer.GetChildren().OfType<MuseumCard>())
        {
            if (otherCard != card)
                otherCard.ButtonPressed = false;
        }

        var species = card.SavedSpecies;

        speciesPreviewPanel.PreviewSpecies = species;
    }

    private void OnOpenInFreebuildPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (speciesPreviewPanel.PreviewSpecies == null)
            return;

        // If we're opening from a game in progress, warn the player
        if (CurrentGame != null)
        {
            leaveGameConfirmationDialog.DialogText = Localization.Translate("OPEN_FOSSIL_IN_FREEBUILD_WARNING");
            leaveGameConfirmationDialog.PopupCenteredShrink();
            return;
        }

        if (speciesPreviewPanel.PreviewSpecies is not MicrobeSpecies)
        {
            GD.PrintErr("Loading non-microbe species is not yet implemented");
            return;
        }

        TransitionToFreebuild(speciesPreviewPanel.PreviewSpecies);
    }

    private void OnOpenInFreebuildConfirmPressed()
    {
        if (speciesPreviewPanel.PreviewSpecies == null)
            return;

        TransitionToFreebuild(speciesPreviewPanel.PreviewSpecies);
    }

    private void TransitionToFreebuild(Species startingSpecies)
    {
        EmitSignal(ThriveopediaPage.SignalName.OnSceneChanged);

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, () =>
        {
            MainMenu.OnEnteringGame();

            // Instantiate a new editor scene
            var editor = SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor).Instantiate<MicrobeEditor>();

            // Start freebuild game with the selected species
            editor.CurrentGame = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings(), true,
                (Species)startingSpecies.Clone());

            // Switch to the editor scene
            SceneManager.Instance.SwitchToScene(editor);
        }, false);
    }

    private void OnOpenFossilFolder()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (!FolderHelpers.OpenFolder(Constants.FOSSILISED_SPECIES_FOLDER))
            fossilDirectoryWarningBox.PopupCenteredShrink();
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
        if (speciesPreviewPanel.PreviewSpecies == cardToBeDeleted.SavedSpecies)
        {
            // Revert back to the welcome message
            welcomeLabel.Visible = true;
            speciesPreviewContainer.Visible = false;
        }

        cardToBeDeleted.QueueFree();
        cardToBeDeleted = null;
    }
}
