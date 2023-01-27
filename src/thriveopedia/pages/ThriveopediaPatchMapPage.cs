﻿using System;
using Godot;

/// <summary>
///   Thriveopedia page displaying the patch map for the current game.
/// </summary>
/// <remarks>
///   <para>
///     Note a lot of this functionality is duplicated from PatchMapEditorComponent.
///   </para>
/// </remarks>
public class ThriveopediaPatchMapPage : ThriveopediaPage
{
    [Export]
    public NodePath? MapDrawerPath;

    [Export]
    public NodePath PatchDetailsPanelPath = null!;

    [Export]
    public NodePath SeedLabelPath = null!;

#pragma warning disable CA2213
    private PatchMapDrawer mapDrawer = null!;
    private PatchDetailsPanel detailsPanel = null!;
    private Label seedLabel = null!;
#pragma warning restore CA2213

    private Patch playerPatchOnEntry = null!;

    public override string PageName => "PatchMap";
    public override string TranslatedPageName => TranslationServer.Translate("THRIVEOPEDIA_PATCH_MAP_PAGE_TITLE");

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
    }

    public override void OnThriveopediaOpened()
    {
        if (CurrentGame == null)
            return;

        // TODO: update the player patch if they move patch in the editor
        SetGameForMap();
    }

    public override void UpdateCurrentWorldDetails()
    {
        if (CurrentGame == null)
            return;

        SetGameForMap();
        UpdateSeedLabel();
    }

    public override void OnNavigationPanelSizeChanged(bool collapsed)
    {
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

    protected virtual void UpdateShownPatchDetails()
    {
        detailsPanel.SelectedPatch = mapDrawer.SelectedPatch;
        detailsPanel.UpdateShownPatchDetails();
    }

    protected void OnTranslationsChanged()
    {
        UpdateShownPatchDetails();
        UpdateSeedLabel();
    }

    private void UpdatePlayerPatch(Patch? patch)
    {
        mapDrawer.PlayerPatch = patch ?? playerPatchOnEntry;
        detailsPanel.CurrentPatch = mapDrawer.PlayerPatch;

        // Just in case this didn't get called already. Note that this may result in duplicate calls here
        UpdateShownPatchDetails();
    }

    private void SetGameForMap()
    {
        mapDrawer.Map = CurrentGame!.GameWorld.Map;
        playerPatchOnEntry = mapDrawer.Map?.CurrentPatch ??
            throw new InvalidOperationException("Map current patch needs to be set / SetMap needs to be called");
        UpdatePlayerPatch(playerPatchOnEntry);
    }

    private void OnFindCurrentPatchPressed()
    {
        mapDrawer.CenterScroll();
        mapDrawer.SelectedPatch = mapDrawer.PlayerPatch;
    }

    private void UpdateSeedLabel()
    {
        seedLabel.Text = TranslationServer.Translate("SEED_LABEL")
            .FormatSafe(CurrentGame!.GameWorld.WorldSettings.Seed);
    }
}
