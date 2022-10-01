using System.Linq;
using Godot;

public class ThriveopediaMuseumPage : ThriveopediaPage
{
    [Export]
    public NodePath CardContainerPath = null!;

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    [Export]
    public NodePath HexesPreviewPath = null!;

    [Export]
    public NodePath SpeciesDetailsLabelPath = null!;

    private GridContainer cardContainer = null!;
    private SpeciesPreview speciesPreview = null!;
    private CellHexesPreview hexesPreview = null!;
    private CustomRichTextLabel speciesDetailsLabel = null!;

    private bool hasBecomeVisibleAtLeastOnce;

    public override string PageName => "Museum";
    public override string TranslatedPageName => TranslationServer.Translate("MUSEUM_PAGE");

    public override void _Ready()
    {
        base._Ready();

        cardContainer = GetNode<GridContainer>(CardContainerPath);
        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexesPreview = GetNode<CellHexesPreview>(HexesPreviewPath);
        speciesDetailsLabel = GetNode<CustomRichTextLabel>(SpeciesDetailsLabelPath);
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationVisibilityChanged && Visible && !hasBecomeVisibleAtLeastOnce)
        {
            for (int i = 0; i < 10; i++)
            {
                var card = (MuseumCard)GD.Load<PackedScene>($"res://src/gui_common/fossilisation/MuseumCard.tscn").Instance();
                card.Connect(nameof(MuseumCard.OnSpeciesSelected), this, nameof(UpdateSpeciesPreview));
                cardContainer.AddChild(card);
            }
            hasBecomeVisibleAtLeastOnce = true;
        }
    }

    public override void UpdateCurrentWorldDetails()
    {
    }

    private void UpdateSpeciesPreview(MuseumCard card)
    {
        var species = card.SavedSpecies;
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
}