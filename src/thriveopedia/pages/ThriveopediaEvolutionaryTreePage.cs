﻿using System;
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
    private void RebuildTree()
    {
        if (CurrentGame == null)
            throw new InvalidOperationException("Current game is null");

        try
        {
            CurrentGame.GameWorld.BuildEvolutionaryTree(evolutionaryTree);

            speciesHistoryList.Clear();

            foreach (var generation in CurrentGame.GameWorld.GenerationHistory)
            {
                var updatedSpeciesData = generation.Value.AllSpeciesData.ToDictionary(s => s.Key,
                    s => GenerationRecord
                        .GetFullSpeciesRecord(s.Key, generation.Key, CurrentGame.GameWorld.GenerationHistory).Species);
                speciesHistoryList.Add(updatedSpeciesData);
            }
        }
        catch (Exception e)
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
