﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Editor patch map component
/// </summary>
/// <remarks>
///   <para>
///     TODO: this is a bit too microbe specific currently so this probably needs a bit more generalization in the
///     future with more logic being put in <see cref="MicrobeEditorPatchMap"/>
///   </para>
/// </remarks>
/// <typeparam name="TEditor">Type of editor this component is for</typeparam>
[GodotAbstract]
public partial class PatchMapEditorComponent<TEditor> : EditorComponentBase<TEditor>
    where TEditor : IEditorWithPatches
{
    /// <summary>
    ///   Where the player wants to move after editing
    /// </summary>
    [JsonProperty]
    protected Patch? targetPatch;

    [JsonProperty]
    protected Patch playerPatchOnEntry = null!;

#pragma warning disable CA2213
    [Export]
    protected PatchMapDrawer mapDrawer = null!;

    [Export]
    [AssignOnlyChildItemsOnDeserialize]
    [JsonProperty]
    protected PatchDetailsPanel detailsPanel = null!;

    [Export]
    private Label seedLabel = null!;
#pragma warning restore CA2213

    private Compound sunlight = null!;

    [JsonProperty]
    private FogOfWarMode fogOfWar;

    private bool enabledMigrationPatchFilter;

    protected PatchMapEditorComponent()
    {
    }

    /// <summary>
    ///   Returns the current patch the player is in
    /// </summary>
    [JsonIgnore]
    public Patch CurrentPatch => targetPatch ?? playerPatchOnEntry;

    /// <summary>
    ///   Returns the patch where the player wants to move after editing
    /// </summary>
    [JsonIgnore]
    public Patch? TargetPatch => targetPatch;

    [JsonIgnore]
    public Patch? SelectedPatch => mapDrawer.SelectedPatch;

    /// <summary>
    ///   Called when the selected patch changes
    /// </summary>
    [JsonIgnore]
    public Action<Patch>? OnSelectedPatchChanged { get; set; }

    public override void _Ready()
    {
        base._Ready();

        mapDrawer.OnSelectedPatchChanged = _ =>
        {
            UpdateShownPatchDetails();

            if (mapDrawer.SelectedPatch != null)
                OnSelectedPatchChanged?.Invoke(mapDrawer.SelectedPatch);
        };

        detailsPanel.OnMoveToPatchClicked = SetPlayerPatch;
        detailsPanel.OnMigrationAdded = ValidateMigration;
        detailsPanel.OnMigrationWizardStepChanged = OnMigrationProgress;

        sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    }

    public override void Init(TEditor owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);

        detailsPanel.SpeciesToUseForMigrations = Editor.CurrentGame.GameWorld.PlayerSpecies;

        fogOfWar = Editor.CurrentGame.FreeBuild ?
            FogOfWarMode.Ignored :
            Editor.CurrentGame.GameWorld.WorldSettings.FogOfWarMode;

        var map = Editor.CurrentGame.GameWorld.Map;

        if (map != mapDrawer.Map)
            throw new InvalidOperationException("Map is not set correctly on this component");

        if (fogOfWar == FogOfWarMode.Ignored)
        {
            map.RevealAllPatches();
        }

        // Make sure the map setting of fog of war always matches the world
        map.FogOfWar = fogOfWar;

        if (!fresh)
        {
            UpdatePlayerPatch(targetPatch);
        }
        else
        {
            targetPatch = null;

            playerPatchOnEntry = map.CurrentPatch ??
                throw new InvalidOperationException("Map current patch needs to be set / SetMap needs to be called");

            UpdatePlayerPatch(playerPatchOnEntry);
        }

        UpdateSeedLabel();
    }

    public void SetMap(PatchMap map)
    {
        mapDrawer.Map = map;
    }

    public override void OnFinishEditing()
    {
        // Move patches
        if (targetPatch != null)
        {
            GD.Print(GetType().Name, ": applying player move to patch: ", targetPatch.Name);
            Editor.CurrentGame.GameWorld.Map.CurrentPatch = targetPatch;

            // Add the edited species to that patch to allow the species to gain population there
            // TODO: Log player species' migration
            targetPatch.AddSpecies(Editor.EditedBaseSpecies, 0);
        }

        // Migrations
        foreach (var migration in detailsPanel.Migrations)
        {
            if (migration.Amount <= 0 || migration.DestinationPatch == null || migration.SourcePatch == null)
            {
                GD.PrintErr("Not applying an invalid migration");
                continue;
            }

            var playerSpecies = Editor.CurrentGame.GameWorld.PlayerSpecies;

            var sourcePreviousPopulation = migration.SourcePatch.GetSpeciesSimulationPopulation(playerSpecies);

            // Max is used here to ensure no negative population ends up being set
            migration.SourcePatch.UpdateSpeciesSimulationPopulation(playerSpecies,
                Math.Max(sourcePreviousPopulation - migration.Amount, 0));

            if (migration.DestinationPatch.FindSpeciesByID(playerSpecies.ID) != null)
            {
                migration.DestinationPatch.UpdateSpeciesSimulationPopulation(playerSpecies,
                    migration.DestinationPatch.GetSpeciesSimulationPopulation(playerSpecies) + migration.Amount);
            }
            else
            {
                migration.DestinationPatch.AddSpecies(playerSpecies, migration.Amount);
            }
        }
    }

    public override void OnMutationPointsChanged(int mutationPoints)
    {
    }

    public override void UpdateUndoRedoButtons(bool canUndo, bool canRedo)
    {
    }

    public override void OnInsufficientMP(bool playSound = true)
    {
    }

    public override void OnActionBlockedWhileAnotherIsInProgress()
    {
    }

    public override void OnValidAction(IEnumerable<CombinableActionData> actions)
    {
    }

    public override void OnLightLevelChanged(float dayLightFraction)
    {
        base.OnLightLevelChanged(dayLightFraction);

        var maxLightLevel = Editor.CurrentPatch.Biome.GetCompound(sunlight, CompoundAmountType.Biome).Ambient;
        var templateMaxLightLevel =
            Editor.CurrentPatch.GetCompoundAmountForDisplay(sunlight, CompoundAmountType.Template);

        // We don't want the light level in other patches be changed to zero if this callback is called while
        // we're on a patch that isn't affected by day/night effects
        if (maxLightLevel > 0.0f && templateMaxLightLevel > 0.0f)
        {
            foreach (var patch in Editor.CurrentGame.GameWorld.Map.Patches.Values)
            {
                var targetMaxLightLevel = patch.Biome.GetCompound(sunlight, CompoundAmountType.Biome);

                var lightLevelAmount = new BiomeCompoundProperties
                {
                    Ambient = targetMaxLightLevel.Ambient * dayLightFraction,
                };

                patch.Biome.CurrentCompoundAmounts[sunlight] = lightLevelAmount;
            }
        }

        // TODO: isn't this entirely logically wrong? See the comment in PatchManager about needing to set average
        // light levels on editor entry. This seems wrong because the average light amount is *not* the current light
        // level, meaning that auto-evo prediction would be incorrect (if these numbers were used there, but aren't
        // currently, see the documentation on previewBiomeConditions)
        // // Need to set average to be the same as ambient so Auto-Evo updates correctly
        // previewBiomeConditions.AverageCompounds[sunlight] = lightLevelAmount;

        UpdateShownPatchDetails();
    }

    protected virtual void UpdateShownPatchDetails()
    {
        detailsPanel.SelectedPatch = mapDrawer.SelectedPatch;
        detailsPanel.IsPatchMoveValid = IsPatchMoveValid(mapDrawer.SelectedPatch);
        detailsPanel.UpdateShownPatchDetails();
    }

    protected override void OnTranslationsChanged()
    {
        UpdateShownPatchDetails();
        UpdateSeedLabel();
    }

    /// <summary>
    ///   Returns true when the player is allowed to move to the specified patch
    /// </summary>
    /// <returns>True if the patch move requested is valid. False otherwise</returns>
    private bool IsPatchMoveValid(Patch? patch)
    {
        if (patch == null)
            return false;

        // Can't go to the patch you are in
        if (CurrentPatch == patch)
            return false;

        // If we are freebuilding, check if the target patch is connected by any means, then it is allowed
        if (Editor.FreeBuilding && CurrentPatch.GetAllConnectedPatches().Contains(patch))
            return true;

        if (CheatManager.MoveToAnyPatch)
            return true;

        // Can move to any patch that player species inhabits or is adjacent to such a patch
        return GetMovablePatches().Contains(patch);
    }

    private HashSet<Patch> GetMovablePatches()
    {
        var movablePatches = Editor.CurrentGame.GameWorld.Map.Patches.Values.Where(p =>
            p.SpeciesInPatch.ContainsKey(Editor.CurrentGame.GameWorld.PlayerSpecies)).ToHashSet();

        foreach (var patch in movablePatches.ToList())
        {
            foreach (var adjacent in patch.Adjacent)
            {
                movablePatches.Add(adjacent);
            }
        }

        return movablePatches;
    }

    private void SetPlayerPatch(Patch? patch)
    {
        if (!IsPatchMoveValid(patch))
            return;

        if (patch == playerPatchOnEntry)
        {
            targetPatch = null;
        }
        else
        {
            targetPatch = patch;
        }

        Editor.OnCurrentPatchUpdated(targetPatch ?? CurrentPatch);
        UpdatePlayerPatch(targetPatch);
    }

    private void UpdatePlayerPatch(Patch? patch)
    {
        if (mapDrawer.Map == null)
            throw new InvalidOperationException("Map needs to be set on the drawer first");

        mapDrawer.PlayerPatch = patch ?? playerPatchOnEntry;

        if (mapDrawer.Map.UpdatePatchVisibility(mapDrawer.PlayerPatch))
            mapDrawer.MarkDirty();

        detailsPanel.CurrentPatch = mapDrawer.PlayerPatch;

        // Just in case this didn't get called already. Note that this may result in duplicate calls here
        UpdateShownPatchDetails();
    }

    private void UpdateSeedLabel()
    {
        seedLabel.Text = Localization.Translate("SEED_LABEL")
            .FormatSafe(Editor.CurrentGame.GameWorld.WorldSettings.Seed);
    }

    private void OnFindCurrentPatchPressed()
    {
        mapDrawer.CenterToCurrentPatch();
        mapDrawer.SelectedPatch = mapDrawer.PlayerPatch;
    }

    private void MoveToPatchClicked()
    {
        SetPlayerPatch(mapDrawer.SelectedPatch);
    }

    private void ValidateMigration(PatchDetailsPanel.Migration migration)
    {
        if (migration.SourcePatch == null || migration.Amount <= 0)
        {
            GD.PrintErr("Trying to check validity of migration without source patch or migration population amount");
            return;
        }

        // Cannot move more population than there exists in the patch
        if (migration.SourcePatch.GetSpeciesSimulationPopulation(Editor.CurrentGame.GameWorld.PlayerSpecies) <
            migration.Amount || migration.Amount < 0)
        {
            detailsPanel.Migrations.Remove(migration);
        }
    }

    private void OnMigrationProgress(PatchDetailsPanel.MigrationWizardStep step)
    {
        if (mapDrawer.Map == null)
        {
            GD.PrintErr("Map not set when setting up a migration");
            return;
        }

        switch (step)
        {
            case PatchDetailsPanel.MigrationWizardStep.SelectSourcePatch:
            {
                // Deselect current patch to allow picking it again (if the player wants it to be the start position)
                mapDrawer.SelectedPatch = null;

                // Enable filter to show only valid patches for move

                mapDrawer.ApplyPatchNodeEnabledStatus(p =>
                    p.GetSpeciesSimulationPopulation(Editor.EditedBaseSpecies) > 0);
                enabledMigrationPatchFilter = true;

                break;
            }

            case PatchDetailsPanel.MigrationWizardStep.SelectDestinationPatch:
            {
                // Enable filter to show just patches next to the source one
                var nextTo = detailsPanel.CurrentMigrationSourcePatch;
                if (nextTo == null)
                {
                    GD.PrintErr("No current migration source patch set");
                }
                else
                {
                    // Reset selected patch to make the map behave better for this case
                    mapDrawer.SelectedPatch = null;

                    // Apply the filter
                    // TODO: should this allow selecting undiscovered patches? The player species being present in a
                    // patch doesn't reveal the patch so it is a bit weird to need to use a bunch of free moves to
                    // reveal patches (this kind of feels like an exploit and someone might report it as a bug).
                    mapDrawer.ApplyPatchNodeEnabledStatus(p =>
                        p != nextTo && p.Adjacent.Contains(nextTo) && p.Visibility == MapElementVisibility.Shown);
                    enabledMigrationPatchFilter = true;
                }

                break;
            }

            case PatchDetailsPanel.MigrationWizardStep.SelectPopulationAmount:
            {
                // At this step selecting any patches is not necessary or useful, so all patches are disabled for
                // selection here for clarity

                mapDrawer.SelectedPatch = null;
                mapDrawer.ApplyPatchNodeEnabledStatus(false);
                enabledMigrationPatchFilter = true;

                break;
            }

            default:
            {
                // Otherwise reset the filter
                if (enabledMigrationPatchFilter)
                {
                    mapDrawer.ApplyPatchNodeEnabledStatus(true);
                    enabledMigrationPatchFilter = false;
                }

                break;
            }
        }

        // When selecting a source patch, only show valid targets
    }
}
