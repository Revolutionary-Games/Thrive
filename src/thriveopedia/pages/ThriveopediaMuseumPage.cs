using System;
using System.Linq;
using System.Threading.Tasks;
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

    [Export]
    private CustomConfirmationDialog fossilDataLoadFailedDialog = null!;

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
        // TODO: caching or something here would make this a lot more efficient as for people with a ton of fossils
        // the Thriveopedia likely will start to lag a lot
        cardContainer.QueueFreeChildren();

        foreach (var speciesName in FossilisedSpecies.CreateListOfFossils(true))
        {
            var (savedSpeciesInfo, image) = FossilisedSpecies.LoadSpeciesInfoFromFile(speciesName, out var plainName);

            // Don't add cards for corrupt fossils
            if (savedSpeciesInfo == null)
                continue;

            var card = museumCardScene.Instantiate<MuseumCard>();
            card.FossilName = plainName;
            card.SpeciesName = savedSpeciesInfo.FormattedName;
            card.OriginalName = speciesName;

            if (string.IsNullOrWhiteSpace(card.SpeciesName))
                card.SpeciesName = "UNKNOWN";

            card.FossilPreviewImage = image;
            card.Outdated = savedSpeciesInfo.IsInvalidOrOutdated;

            // Don't need to connect this if outdated as we cannot allow loading it
            if (!savedSpeciesInfo.IsInvalidOrOutdated)
            {
                card.Connect(MuseumCard.SignalName.OnSpeciesSelected,
                    new Callable(this, nameof(UpdateSpeciesPreview)));
            }

            card.Connect(MuseumCard.SignalName.OnSpeciesDeleted, new Callable(this, nameof(DeleteSpecies)));

            cardContainer.AddChild(card);
        }
    }

    private void UpdateSpeciesPreview(MuseumCard card)
    {
        if (!speciesPreviewContainer.Visible)
        {
            welcomeLabel.Visible = false;
            speciesPreviewContainer.Visible = true;
        }

        // Deselect all other cards to prevent highlights from hanging around.
        foreach (var otherCard in cardContainer.GetChildren().OfType<MuseumCard>())
        {
            if (otherCard != card)
                otherCard.ButtonPressed = false;
        }

        var fileName = card.OriginalName;

        if (string.IsNullOrEmpty(fileName))
        {
            fossilDataLoadFailedDialog.PopupCenteredShrink();
            speciesPreviewPanel.PreviewSpecies = null;
            return;
        }

        // Use this as a crude loading indicator
        speciesPreviewPanel.Modulate = Colors.Gray;

        // Load the data in a background thread and then display it
        TaskExecutor.Instance.AddTask(new Task(() =>
        {
            try
            {
                var loaded = FossilisedSpecies.LoadSpeciesFromFile(fileName) ??
                    throw new Exception("Could not load species data");

                Invoke.Instance.QueueForObject(() =>
                {
                    speciesPreviewPanel.Modulate = Colors.White;
                    speciesPreviewPanel.PreviewSpecies = loaded.Species;
                }, this);
            }
            catch (Exception e)
            {
                Invoke.Instance.QueueForObject(() =>
                {
                    GD.PrintErr("Failed to load fossilised species:");
                    GD.PrintErr(e);
                    speciesPreviewPanel.PreviewSpecies = null;
                    fossilDataLoadFailedDialog.PopupCenteredShrink();
                }, this);
            }
        }), true);
    }

    private void OnOpenInFreebuildPressed()
    {
        if (speciesPreviewPanel.PreviewSpecies == null)
            return;

        GUICommon.Instance.PlayButtonPressSound();

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
            MainMenu.OnEnteringGame(false);

            // Instantiate a new editor scene
            var editor = SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor).Instantiate<MicrobeEditor>();

            // Start a freebuild game with the selected species
            editor.CurrentGame = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings(), true,
                (Species)startingSpecies.Clone());
            AchievementsManager.ReportEnteredFreebuild();

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

        var fossilName = cardToBeDeleted.OriginalName;

        if (string.IsNullOrWhiteSpace(fossilName))
        {
            GD.PrintErr("Attempted to delete a fossil with a null file name");
            return;
        }

        try
        {
            FossilisedSpecies.DeleteFossilFile(fossilName);
        }
        catch (Exception e)
        {
            deletionFailedDialog.PopupCenteredShrink();

            GD.PrintErr("Failed to delete fossil file: ", e);
            return;
        }

        // If the species we just deleted was being displayed in the sidebar
        if (speciesPreviewPanel.PreviewSpecies != null &&
            speciesPreviewPanel.PreviewSpecies.FormattedName == cardToBeDeleted.SpeciesName)
        {
            // Revert to the welcome message
            welcomeLabel.Visible = true;
            speciesPreviewContainer.Visible = false;
        }

        cardToBeDeleted.QueueFree();
        cardToBeDeleted = null;
    }
}
