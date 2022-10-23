using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

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

    public override string PageName => "Museum";
    public override string TranslatedPageName => TranslationServer.Translate("MUSEUM_PAGE");

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
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationVisibilityChanged && Visible)
        {
            // For now, rebuild the card list entirely each time we open the page. Could well be optimised.
            foreach (Node card in cardContainer.GetChildren())
                card.DetachAndQueueFree();

            foreach (var speciesName in FossilisedSpecies.CreateListOfSaves())
            {
                var card = (MuseumCard)GD.Load<PackedScene>($"res://src/gui_common/fossilisation/MuseumCard.tscn").Instance();

                var savedSpecies = FossilisedSpecies.LoadSpeciesFromFile(speciesName);

                if (savedSpecies is not MicrobeSpecies)
                {
                    throw new NotImplementedException("Loading non-microbe species is not yet implemented");
                }

                card.SavedSpecies = (MicrobeSpecies)FossilisedSpecies.LoadSpeciesFromFile(speciesName);
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
        cardContainer.Columns = collapsed ? 3 : 4;
    }

    private void UpdateSpeciesPreview(MuseumCard card)
    {
        if (!speciesPreviewContainer.Visible)
        {
            welcomeLabel.Visible = false;
            speciesPreviewContainer.Visible = true;
        }

        var species = card.SavedSpecies;
        speciesPreview.PreviewSpecies = species;

        // Deselect all other cards to prevent highlights hanging around.
        foreach (MuseumCard otherCard in cardContainer.GetChildren())
        {
            if (otherCard != card)
                otherCard.Pressed = false;
        }

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
        speciesDetailsLabel.ExtendedBbcode = TranslationServer.Translate("SPECIES_DETAIL_TEXT").FormatSafe(
            species.FormattedNameBbCode, species.ID, species.Generation, species.Population, species.Colour.ToHtml(),
            string.Join("\n  ", species.Behaviour.Select(b =>
                BehaviourDictionary.GetBehaviourLocalizedString(b.Key) + ": " + b.Value)));

        switch (species)
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

    private void OnOpenInFreebuildPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (speciesPreview.PreviewSpecies == null)
            return;

        if (CurrentGame != null)
        {
            leaveGameConfirmationDialog.DialogText = TranslationServer.Translate("OPEN_IN_FREEBUILD_WARNING");
            leaveGameConfirmationDialog.PopupCenteredShrink();
            return;
        }

        TransitionToFreebuild();
    }

    private void TransitionToFreebuild()
    {
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f, () =>
        {
            // Instantiate a new editor scene
            var editor = (MicrobeEditor)SceneManager.Instance.LoadScene(MainGameState.MicrobeEditor).Instance();

            // Start freebuild game with the selected species
            editor.CurrentGame = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings(), true, (Species)speciesPreview.PreviewSpecies!.Clone());

            // Switch to the editor scene
            SceneManager.Instance.SwitchToScene(editor);
        }, false);
    }
}