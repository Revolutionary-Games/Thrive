using System;
using System.Linq;
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

    private GridContainer cardContainer = null!;
    private Control welcomeLabel = null!;
    private VBoxContainer speciesPreviewContainer = null!;
    private SpeciesPreview speciesPreview = null!;
    private CellHexesPreview hexesPreview = null!;
    private CustomRichTextLabel speciesDetailsLabel = null!;
    private CustomConfirmationDialog leaveGameConfirmationDialog = null!;
    private PackedScene museumCardScene = null!;

    public override string PageName => "Museum";
    public override string TranslatedPageName => TranslationServer.Translate("THRIVEOPEDIA_MUSEUM_PAGE_TITLE");

    public override void _Ready()
    {
        base._Ready();

        cardContainer = GetNode<GridContainer>(CardContainerPath);
        welcomeLabel = GetNode<Control>(WelcomeLabelPath);
        speciesPreviewContainer = GetNode<VBoxContainer>(SpeciesPreviewContainerPath);
        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexesPreview = GetNode<CellHexesPreview>(HexesPreviewPath);
        speciesDetailsLabel = GetNode<CustomRichTextLabel>(SpeciesDetailsLabelPath);
        leaveGameConfirmationDialog = GetNode<CustomConfirmationDialog>(LeaveGameConfirmationDialogPath);

        museumCardScene = GD.Load<PackedScene>("res://src/thriveopedia/fossilisation/MuseumCard.tscn");
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationVisibilityChanged && Visible)
        {
            // For now, rebuild the card list entirely each time we open the page. Very unoptimised, but it keeps
            // the museum up to date with the player's new fossilisations in a game. A possible next step would be
            // to rebuild only when the Thriveopedia as a whole is opened.
            cardContainer.QueueFreeChildren();

            foreach (var speciesName in FossilisedSpecies.CreateListOfFossils(true))
            {
                var card = (MuseumCard)museumCardScene.Instance();

                var savedSpecies = FossilisedSpecies.LoadSpeciesFromFile(speciesName);

                if (savedSpecies is not MicrobeSpecies)
                {
                    GD.PrintErr("Loading non-microbe species is not yet implemented");
                }

                card.SavedSpecies = FossilisedSpecies.LoadSpeciesFromFile(speciesName);
                card.Connect(nameof(MuseumCard.OnSpeciesSelected), this, nameof(UpdateSpeciesPreview));
                cardContainer.AddChild(card);
            }
        }
    }

    public override void UpdateCurrentWorldDetails()
    {
    }

    public override void OnNavigationPanelSizeChanged(bool collapsed)
    {
        // Change the number of columns to reflect the space the museum takes up
        cardContainer.Columns = collapsed ? 3 : 4;
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
}
