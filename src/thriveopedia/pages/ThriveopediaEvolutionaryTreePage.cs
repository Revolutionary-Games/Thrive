using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

/// <summary>
///   Thriveopedia page displaying an evolutionary tree of all species in the current game.
/// </summary>
/// <remarks>
///   <para>
///     Note a lot of this functionality is duplicated from AutoEvoExploringTool.
///   </para>
/// </remarks>
public partial class ThriveopediaEvolutionaryTreePage : ThriveopediaPage, IThriveopediaPage
{
    /// <summary>
    ///   Full species data keyed by generation number. This is not a list indexed by generation as the generation
    ///   history can have gaps in it (see <see cref="GenerationRecord.GetFullSpeciesRecord"/>).
    /// </summary>
    private readonly Dictionary<int, Dictionary<uint, Species>> speciesHistory = new();

#pragma warning disable CA2213
    [Export]
    private VBoxContainer errorContainer = null!;

    [Export]
    private EvolutionaryTree evolutionaryTree = null!;

    [Export]
    private SpeciesDetailsPanelWithFossilisation speciesDetailsPanelWithFossilisation = null!;
#pragma warning restore CA2213

    public string PageName => "EvolutionaryTree";

    public string TranslatedPageName => Localization.Translate("THRIVEOPEDIA_EVOLUTIONARY_TREE_PAGE_TITLE");
    public string? TranslatedPageBody => null;
    public string? TranslatedAdditionalSearchContent => null;

    public string ParentPageName => "CurrentWorld";

    public override void _Ready()
    {
        base._Ready();

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

        // Do not rebuild the tree if we are not showing it as this causes extra lag upon loading a save
        if (!IsVisibleInTree())
        {
            GD.Print("Skipping evolutionary tree rebuild as not visible in tree");
            return;
        }

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

            speciesHistory.Clear();

            foreach (var generation in CurrentGame.GameWorld.GenerationHistory)
            {
                var updatedSpeciesData = generation.Value.AllSpeciesData.ToDictionary(s => s.Key,
                    s => GenerationRecord
                        .GetFullSpeciesRecord(s.Key, generation.Key, CurrentGame.GameWorld.GenerationHistory).Species);
                speciesHistory[generation.Key] = updatedSpeciesData;
            }
        }
        catch (Exception e)
        {
            OnTreeFailedToBuild(e.ToString());
        }
    }

    private void OnTreeFailedToBuild(string error)
    {
        // TODO: if the failures happen relatively often it'd be good to show the actual error to the player as
        // otherwise we specifically need to get users to give us their logs
        GD.PrintErr($"Evolutionary tree failed to build with error: {error}");
        evolutionaryTree.Visible = false;
        errorContainer.Visible = true;
    }

    private void EvolutionaryTreeNodeSelected(int generation, uint id)
    {
        if (!speciesHistory.TryGetValue(generation, out var generationSpecies) ||
            !generationSpecies.TryGetValue(id, out var species))
        {
            GD.PrintErr($"No data for species {id} in generation {generation} to show for the selected tree node");
            speciesDetailsPanelWithFossilisation.PreviewSpecies = null;
            return;
        }

        speciesDetailsPanelWithFossilisation.PreviewSpecies = species;
    }
}
