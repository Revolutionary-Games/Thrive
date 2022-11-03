using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///    Thriveopedia page displaying an evolutionary tree of all species in the current game.
/// </summary>
/// <remarks>
///   <para>
///     Note a lot of this functionality is duplicated from AutoEvoExploringTool.
///   </para>
/// </remarks>
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

    private VBoxContainer disabledWarning = null!;
    private EvolutionaryTree evolutionaryTree = null!;
    private CustomRichTextLabel speciesDetailsLabel = null!;
    private SpeciesPreview speciesPreview = null!;
    private CellHexesPreview hexesPreview = null!;

    public override string PageName => "EvolutionaryTree";

    public override string TranslatedPageName =>
        TranslationServer.Translate("THRIVEOPEDIA_EVOLUTIONARY_TREE_PAGE_TITLE");

    public override void _Ready()
    {
        base._Ready();

        disabledWarning = GetNode<VBoxContainer>(DisabledInFreebuildPath);
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

    /// <summary>
    ///   Clear and then rebuild the evolutionary tree each time we open the page. This way, we ensure the tree is
    ///   always up to date.
    /// </summary>
    public void RebuildTree()
    {
        if (!Visible)
            return;

        if (CurrentGame == null)
            throw new InvalidOperationException("Current game is null");

        // TODO: fix the tree for freebuild
        if (CurrentGame.FreeBuild)
        {
            evolutionaryTree.Visible = false;
            disabledWarning.Visible = true;
            return;
        }

        // Building the tree relies on the existence of a full history of generations stored in the current game. Since
        // we only started adding these in 0.6.0, it's impossible to build a tree in older saves.
        // TODO: avoid an ugly try/catch block by actually checking the original save version?
        try
        {
            evolutionaryTree.Clear();
            speciesHistoryList.Clear();

            evolutionaryTree.Init(CurrentGame.GameWorld.PlayerSpecies);
            InitFirstGeneration();

            // A possible next step would be to rebuild only when the Thriveopedia as a whole is opened.
            foreach (var generation in CurrentGame.GameWorld.GenerationHistory)
            {
                var record = generation.Value;
                evolutionaryTree.UpdateEvolutionaryTreeWithRunResults(
                    record.AutoEvoResults, record.Generation, record.TimeElapsed);
                speciesHistoryList.Add(record.AllSpecies);
            }
        }
        catch (KeyNotFoundException e)
        {
            GD.PrintErr($"Evolutionary tree failed to build with error {e}");
            evolutionaryTree.Visible = false;
            disabledWarning.Visible = true;
        }
    }

    private void InitFirstGeneration()
    {
        // TODO: fix this so it shows player species progression rather than current state
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
        speciesDetailsLabel.ExtendedBbcode = species.GetDetailString();
    }
}
