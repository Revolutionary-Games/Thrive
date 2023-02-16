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
public abstract class PatchMapEditorComponent<TEditor> : EditorComponentBase<TEditor>
    where TEditor : IEditorWithPatches
{
    [Export]
    public NodePath? MapDrawerPath;

    [Export]
    public NodePath PatchDetailsPanelPath = null!;

    [Export]
    public NodePath SeedLabelPath = null!;

    /// <summary>
    ///   Where the player wants to move after editing
    /// </summary>
    [JsonProperty]
    protected Patch? targetPatch;

    [JsonProperty]
    protected Patch playerPatchOnEntry = null!;

#pragma warning disable CA2213
    protected PatchMapDrawer mapDrawer = null!;
    protected PatchDetailsPanel detailsPanel = null!;
    private Label seedLabel = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Returns the current patch the player is in
    /// </summary>
    [JsonIgnore]
    public Patch CurrentPatch => targetPatch ?? playerPatchOnEntry;

    [JsonIgnore]
    public Patch? SelectedPatch => targetPatch;

    /// <summary>
    ///   Called when the selected patch changes
    /// </summary>
    [JsonIgnore]
    public Action<Patch>? OnSelectedPatchChanged { get; set; }

    public override void _Ready()
    {
        base._Ready();

        mapDrawer = GetNode<PatchMapDrawer>(MapDrawerPath);
        detailsPanel = GetNode<PatchDetailsPanel>(PatchDetailsPanelPath);
        seedLabel = GetNode<Label>(SeedLabelPath);

        mapDrawer.OnSelectedPatchChanged = _ =>
        {
            UpdateShownPatchDetails();

            if (mapDrawer.SelectedPatch != null)
                OnSelectedPatchChanged?.Invoke(mapDrawer.SelectedPatch);
        };

        detailsPanel.OnMoveToPatchClicked = SetPlayerPatch;
    }

    public override void Init(TEditor owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);

        if (!fresh)
        {
            UpdatePlayerPatch(targetPatch);
        }
        else
        {
            targetPatch = null;

            playerPatchOnEntry = mapDrawer.Map?.CurrentPatch ??
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

    public override void OnValidAction()
    {
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (MapDrawerPath != null)
            {
                MapDrawerPath.Dispose();
                PatchDetailsPanelPath.Dispose();
                SeedLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
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
        mapDrawer.PlayerPatch = patch ?? playerPatchOnEntry;
        detailsPanel.CurrentPatch = mapDrawer.PlayerPatch;

        // Just in case this didn't get called already. Note that this may result in duplicate calls here
        UpdateShownPatchDetails();
    }

    private void UpdateSeedLabel()
    {
        seedLabel.Text = TranslationServer.Translate("SEED_LABEL")
            .FormatSafe(Editor.CurrentGame.GameWorld.WorldSettings.Seed);
    }

    private void OnFindCurrentPatchPressed()
    {
        mapDrawer.CenterScroll();
        mapDrawer.SelectedPatch = mapDrawer.PlayerPatch;
    }

    private void MoveToPatchClicked()
    {
        SetPlayerPatch(mapDrawer.SelectedPatch);
    }
}
