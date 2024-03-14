using System;
using Godot;

/// <summary>
///   Thriveopedia page displaying fossilised (saved) organisms.
/// </summary>
public partial class ThriveopediaMuseumPage : ThriveopediaPage, IThriveopediaPage
{
    [Export]
    public NodePath? CardContainerPath;

    [Export]
    public NodePath WelcomeLabelPath = null!;

    [Export]
    public NodePath SpeciesPreviewContainerPath = null!;

    [Export]
    public NodePath SpeciesPreviewPanelPath = null!;

    [Export]
    public NodePath LeaveGameConfirmationDialogPath = null!;

    [Export]
    public NodePath FossilDirectoryWarningBoxPath = null!;

    [Export]
    public NodePath DeleteConfirmationDialogPath = null!;

    [Export]
    public NodePath DeletionFailedDialogPath = null!;

#pragma warning disable CA2213
    private HFlowContainer cardContainer = null!;
    private Control welcomeLabel = null!;
    private VBoxContainer speciesPreviewContainer = null!;
    private SpeciesDetailsPanel speciesPreviewPanel = null!;
    private CustomConfirmationDialog leaveGameConfirmationDialog = null!;
    private CustomConfirmationDialog fossilDirectoryWarningBox = null!;
    private CustomConfirmationDialog deleteConfirmationDialog = null!;
    private CustomConfirmationDialog deletionFailedDialog = null!;
    private PackedScene museumCardScene = null!;
#pragma warning restore CA2213

    private MuseumCard? cardToBeDeleted;

    public string PageName => "Museum";
    public string TranslatedPageName => Localization.Translate("THRIVEOPEDIA_MUSEUM_PAGE_TITLE");

    public string? ParentPageName => null;

    public override void _Ready()
    {
        base._Ready();

        cardContainer = GetNode<HFlowContainer>(CardContainerPath);
        welcomeLabel = GetNode<Control>(WelcomeLabelPath);
        speciesPreviewContainer = GetNode<VBoxContainer>(SpeciesPreviewContainerPath);
        speciesPreviewPanel = GetNode<SpeciesDetailsPanel>(SpeciesPreviewPanelPath);
        leaveGameConfirmationDialog = GetNode<CustomConfirmationDialog>(LeaveGameConfirmationDialogPath);
        fossilDirectoryWarningBox = GetNode<CustomConfirmationDialog>(FossilDirectoryWarningBoxPath);
        deleteConfirmationDialog = GetNode<CustomConfirmationDialog>(DeleteConfirmationDialogPath);
        deletionFailedDialog = GetNode<CustomConfirmationDialog>(DeletionFailedDialogPath);

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
            if (CardContainerPath != null)
            {
                CardContainerPath.Dispose();
                WelcomeLabelPath.Dispose();
                SpeciesPreviewContainerPath.Dispose();
                SpeciesPreviewPanelPath.Dispose();
                LeaveGameConfirmationDialogPath.Dispose();
                FossilDirectoryWarningBoxPath.Dispose();
                DeleteConfirmationDialogPath.Dispose();
                DeletionFailedDialogPath.Dispose();
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
        foreach (MuseumCard otherCard in cardContainer.GetChildren())
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
        EmitSignal(SignalName.OnSceneChanged);

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
