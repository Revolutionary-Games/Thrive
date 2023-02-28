﻿using Godot;

/// <summary>
///   Thriveopedia page displaying fossilised (saved) organisms.
/// </summary>
public class ThriveopediaMuseumPage : ThriveopediaPage
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

#pragma warning disable CA2213
    private HFlowContainer cardContainer = null!;
    private Control welcomeLabel = null!;
    private VBoxContainer speciesPreviewContainer = null!;
    private SpeciesDetailsPanel speciesPreviewPanel = null!;
    private CustomConfirmationDialog leaveGameConfirmationDialog = null!;
    private CustomConfirmationDialog fossilDirectoryWarningBox = null!;
    private PackedScene museumCardScene = null!;
#pragma warning restore CA2213

    public override string PageName => "Museum";
    public override string TranslatedPageName => TranslationServer.Translate("THRIVEOPEDIA_MUSEUM_PAGE_TITLE");

    public override void _Ready()
    {
        base._Ready();

        cardContainer = GetNode<HFlowContainer>(CardContainerPath);
        welcomeLabel = GetNode<Control>(WelcomeLabelPath);
        speciesPreviewContainer = GetNode<VBoxContainer>(SpeciesPreviewContainerPath);
        speciesPreviewPanel = GetNode<SpeciesDetailsPanel>(SpeciesPreviewPanelPath);
        leaveGameConfirmationDialog = GetNode<CustomConfirmationDialog>(LeaveGameConfirmationDialogPath);
        fossilDirectoryWarningBox = GetNode<CustomConfirmationDialog>(FossilDirectoryWarningBoxPath);

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

            var card = (MuseumCard)museumCardScene.Instance();
            card.SavedSpecies = savedSpecies.Species;
            card.FossilPreviewImage = savedSpecies.PreviewImage;
            card.Connect(nameof(MuseumCard.OnSpeciesSelected), this, nameof(UpdateSpeciesPreview));
            cardContainer.AddChild(card);
        }
    }

    public override void UpdateCurrentWorldDetails()
    {
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
                otherCard.Pressed = false;
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
            leaveGameConfirmationDialog.DialogText = TranslationServer.Translate("OPEN_FOSSIL_IN_FREEBUILD_WARNING");
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
        EmitSignal(nameof(OnSceneChanged));

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, () =>
        {
            MainMenu.OnEnteringGame();

            // Instantiate a new editor scene
            var editor = (MicrobeEditor)SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor).Instance();

            // Start freebuild game with the selected species
            editor.CurrentGame = GameProperties.StartNewMicrobeGame(
                new WorldGenerationSettings(), true, (Species)startingSpecies.Clone());

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
}
