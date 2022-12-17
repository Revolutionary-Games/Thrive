using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
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
    public NodePath SpeciesDetailsPanelPath = null!;

    private readonly List<Dictionary<uint, Species>> speciesHistoryList = new();

    private VBoxContainer disabledWarning = null!;
    private EvolutionaryTree evolutionaryTree = null!;
    private SpeciesDetailsPanel speciesDetailsPanel = null!;

    public override string PageName => "EvolutionaryTree";

    public override string TranslatedPageName =>
        TranslationServer.Translate("THRIVEOPEDIA_EVOLUTIONARY_TREE_PAGE_TITLE");

    public override void _Ready()
    {
        base._Ready();

        disabledWarning = GetNode<VBoxContainer>(DisabledInFreebuildPath);
        evolutionaryTree = GetNode<EvolutionaryTree>(EvolutionaryTreePath);
        speciesDetailsPanel = GetNode<SpeciesDetailsPanel>(SpeciesDetailsPanelPath);

        UpdateCurrentWorldDetails();
    }

    public override void OnThriveopediaOpened()
    {
        if (CurrentGame == null)
            return;

        RebuildTree();
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
        if (CurrentGame == null)
            throw new InvalidOperationException("Current game is null");

        // TODO: fix the tree for freebuild
        if (CurrentGame.FreeBuild)
        {
            OnTreeFailedToBuild("Tree opened in freebuild mode");
            return;
        }

        // Building the tree relies on the existence of a full history of generations stored in the current game. Since
        // we only started adding these in 0.6.0, it's impossible to build a tree in older saves.
        // TODO: avoid an ugly try/catch block by actually checking the original save version?
        var generationHistory = CurrentGame.GameWorld.GenerationHistory;
        if (generationHistory.Count < 1)
        {
            OnTreeFailedToBuild("Generation history is empty");
            return;
        }

        try
        {
            evolutionaryTree.Clear();
            speciesHistoryList.Clear();

            foreach (var generation in generationHistory)
            {
                var record = generation.Value;

                if (generation.Key == 0)
                {
                    var playerSpeciesID = CurrentGame!.GameWorld.PlayerSpecies.ID;
                    var playerSpeciesData = record.AllSpeciesData[playerSpeciesID];

                    // Player species data should never be null for any generation
                    evolutionaryTree.Init(
                        playerSpeciesData.Species!,
                        CurrentGame.GameWorld.PlayerSpecies.FormattedName);
                    speciesHistoryList.Add(new Dictionary<uint, Species>
                    {
                        { playerSpeciesID, playerSpeciesData.Species! },
                    });
                    continue;
                }

                // Recover all omitted species data for this generation so we can fill the tree
                var updatedSpeciesData = record.AllSpeciesData.ToDictionary(
                    s => s.Key,
                    s => GenerationRecord.GetFullSpeciesRecord(s.Key, generation.Key, generationHistory));

                evolutionaryTree.UpdateEvolutionaryTreeWithRunResults(updatedSpeciesData, generation.Key,
                    record.TimeElapsed, CurrentGame.GameWorld.PlayerSpecies.ID);
                speciesHistoryList.Add(updatedSpeciesData.ToDictionary(s => s.Key, s => s.Value.Species));
            }
        }
        catch (KeyNotFoundException e)
        {
            OnTreeFailedToBuild(e.ToString());
        }
    }

    private void OnTreeFailedToBuild(string error)
    {
        GD.PrintErr($"Evolutionary tree failed to build with error: {error}");
        evolutionaryTree.Visible = false;
        disabledWarning.Visible = true;
    }

    private void EvolutionaryTreeNodeSelected(int generation, uint id)
    {
        speciesDetailsPanel.PreviewSpecies = speciesHistoryList[generation][id];
    }
}
