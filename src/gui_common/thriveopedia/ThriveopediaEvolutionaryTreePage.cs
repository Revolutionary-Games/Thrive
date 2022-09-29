using System.Collections.Generic;
using System.Linq;
using Godot;

public class ThriveopediaEvolutionaryTreePage : ThriveopediaPage
{
    [Export]
    public NodePath EvolutionaryTreePath = null!;

    [Export]
    public NodePath SpeciesDetailsLabelPath = null!;

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    [Export]
    public NodePath HexPreviewPath = null!;

    private EvolutionaryTree evolutionaryTree = null!;
    private CustomRichTextLabel speciesDetailsLabel = null!;
    private SpeciesPreview speciesPreview = null!;
    private CellHexesPreview hexesPreview = null!;

    private bool initialised;
    private readonly List<Dictionary<uint, Species>> speciesHistoryList = new();

    public override string PageName => "EvolutionaryTree";
    public override string TranslatedPageName => TranslationServer.Translate("EVOLUTIONARY_TREE_PAGE");

    public override void _Ready()
    {
        base._Ready();

        evolutionaryTree = GetNode<EvolutionaryTree>(EvolutionaryTreePath);
        speciesDetailsLabel = GetNode<CustomRichTextLabel>(SpeciesDetailsLabelPath);
        speciesPreview = GetNode<SpeciesPreview>(SpeciesPreviewPath);
        hexesPreview = GetNode<CellHexesPreview>(HexPreviewPath);

        UpdateCurrentWorldDetails();
    }

    public override void UpdateCurrentWorldDetails()
    {
        if (CurrentGame == null)
            return;
            
        if (!initialised)
        {
            evolutionaryTree.Init(CurrentGame.GameWorld.PlayerSpecies);
            InitFirstGeneration();
            initialised = true;
        }
    }

    private void InitFirstGeneration()
    {
        speciesHistoryList.Add(new Dictionary<uint, Species>
        {
            { CurrentGame!.GameWorld.PlayerSpecies.ID, CurrentGame.GameWorld.PlayerSpecies },
        });
    }

    private void UpdateSpeciesPreview(Species species)
    {
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

    private void EvolutionaryTreeNodeSelected(int generation, uint id)
    {
        UpdateSpeciesPreview(speciesHistoryList[generation][id]);
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