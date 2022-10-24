using System.Collections.Generic;
using System.Linq;
using Godot;

public class ThriveopediaEvolutionaryTreePage : ThriveopediaPage
{
    [Export]
    public NodePath DisabledInFreebuildPath = null!;

    [Export]
    public NodePath EvolutionaryTreePath = null!;

    [Export]
    public NodePath SpeciesDetailsLabelPath = null!;

    [Export]
    public NodePath SpeciesPreviewPath = null!;

    [Export]
    public NodePath HexPreviewPath = null!;

    private readonly List<Dictionary<uint, Species>> speciesHistoryList = new();
    private VBoxContainer disabledInFreebuild = null!;
    private EvolutionaryTree evolutionaryTree = null!;
    private CustomRichTextLabel speciesDetailsLabel = null!;
    private SpeciesPreview speciesPreview = null!;
    private CellHexesPreview hexesPreview = null!;

    public override string PageName => "EvolutionaryTree";
    public override string TranslatedPageName => TranslationServer.Translate("EVOLUTIONARY_TREE_PAGE");

    public override void _Ready()
    {
        base._Ready();

        disabledInFreebuild = GetNode<VBoxContainer>(DisabledInFreebuildPath);
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

        RebuildTree();
    }

    public override void OnNavigationPanelSizeChanged(bool collapsed)
    {
    }

    public void RebuildTree()
    {
        if (!Visible)
            return;

        // TODO fix the tree for freebuild?
        if (CurrentGame!.FreeBuild)
        {
            evolutionaryTree.Visible = false;
            disabledInFreebuild.Visible = true;
            return;
        }

        // Building the tree relies on the existence of a full history of generations stored in the current game. Since
        // we only started adding these in 0.6.0, it's impossible to build a tree in older saves.
        // TODO avoid an ugly try/catch block by actually checking the original save version?
        try
        {
            evolutionaryTree.Clear();
            speciesHistoryList.Clear();

            evolutionaryTree.Init(CurrentGame.GameWorld.PlayerSpecies);
            InitFirstGeneration();

            foreach (var generation in CurrentGame.GameWorld.GenerationHistory)
            {
                var record = generation.Value;
                evolutionaryTree.UpdateEvolutionaryTreeWithRunResults(
                    record.AutoEvoResult, record.Generation, record.TimeElapsed);
                speciesHistoryList.Add(record.AllSpecies);
            }
        }
        catch (KeyNotFoundException)
        {
            evolutionaryTree.Visible = false;
            disabledInFreebuild.Visible = true;
        }
    }

    private void InitFirstGeneration()
    {
        // TODO fix this so it shows player species progression rather than current state
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
