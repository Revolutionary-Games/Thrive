using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Manages the multicellular HUD scene
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class MulticellularHUD : StageHUDBase<MulticellularStage>
{
    [Export]
    public NodePath? MoveToLandPopupPath;

    [Export]
    public NodePath ToLandButtonPath = null!;

    [Export]
    public NodePath AwakenButtonPath = null!;

    [Export]
    public NodePath AwakenConfirmPopupPath = null!;

#pragma warning disable CA2213
    private CustomDialog moveToLandPopup = null!;
    private Button toLandButton = null!;
    private Button awakenButton = null!;
    private CustomDialog awakenConfirmPopup = null!;
#pragma warning restore CA2213

    private float? lastBrainPower;

    // These signals need to be copied to inheriting classes for Godot editor to pick them up
    [Signal]
    public new delegate void OnOpenMenu();

    [Signal]
    public new delegate void OnOpenMenuToHelp();

    protected override string? UnPauseHelpText => null;

    public override void _Ready()
    {
        base._Ready();

        moveToLandPopup = GetNode<CustomDialog>(MoveToLandPopupPath);
        toLandButton = GetNode<Button>(ToLandButtonPath);
        awakenButton = GetNode<Button>(AwakenButtonPath);
        awakenConfirmPopup = GetNode<CustomDialog>(AwakenConfirmPopupPath);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (stage == null)
            return;

        if (stage.HasPlayer)
        {
            UpdateAwakenButton(stage.Player!);

            // Hide the land button when already on the land in the prototype
            toLandButton.Visible = stage.Player!.MovementMode == MovementMode.Swimming;
        }
        else
        {
            awakenButton.Visible = false;
            toLandButton.Visible = false;
        }
    }

    public override void ShowFossilisationButtons()
    {
    }

    protected override void ReadPlayerHitpoints(out float hp, out float maxHP)
    {
        // TODO: player hitpoints
        hp = 100;
        maxHP = 100;
    }

    protected override CompoundBag GetPlayerUsefulCompounds()
    {
        return stage!.Player!.ProcessCompoundStorage;
    }

    protected override Func<Compound, bool> GetIsUsefulCheck()
    {
        var bag = stage!.Player!.ProcessCompoundStorage;
        return c => bag.IsUseful(c);
    }

    protected override bool SpecialHandleBar(ProgressBar bar)
    {
        return false;
    }

    protected override bool ShouldShowAgentsPanel()
    {
        throw new NotImplementedException();
    }

    protected override void CalculatePlayerReproductionProgress(out Dictionary<Compound, float> gatheredCompounds,
        out Dictionary<Compound, float> totalNeededCompounds)
    {
        // TODO: reproduction process for multicellular
        gatheredCompounds = new Dictionary<Compound, float>
        {
            { ammonia, 1 },
            { phosphates, 1 },
        };
        totalNeededCompounds = new Dictionary<Compound, float>
        {
            { ammonia, 1 },
            { phosphates, 1 },
        };
    }

    protected override ICompoundStorage GetPlayerStorage()
    {
        return stage!.Player!.ProcessCompoundStorage;
    }

    protected override ProcessStatistics? GetPlayerProcessStatistics()
    {
        return stage!.Player!.ProcessStatistics;
    }

    protected override IEnumerable<(bool Player, Species Species)> GetHoveredSpecies()
    {
        // TODO: implement nearby species
        return Array.Empty<(bool Player, Species Species)>();
    }

    protected override IReadOnlyDictionary<Compound, float> GetHoveredCompounds()
    {
        // TODO: implement nearby compounds
        return new Dictionary<Compound, float>();
    }

    protected override void UpdateAbilitiesHotBar()
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (MoveToLandPopupPath != null)
            {
                MoveToLandPopupPath.Dispose();
                ToLandButtonPath.Dispose();
                AwakenButtonPath.Dispose();
                AwakenConfirmPopupPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnMoveToLandPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        moveToLandPopup.PopupCenteredShrink();

        // TODO: make the cursor visible while this popup is open
    }

    private void OnMoveToLandConfirmed()
    {
        if (stage?.Player == null)
        {
            GD.Print("Player is missing to move to land");
            return;
        }

        GD.Print("Moving player to land");

        toLandButton.Disabled = true;

        EnsureGameIsUnpausedForEditor();

        // TODO: this is entirely placeholder feature
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.3f, stage.TeleportToLand, false);
    }

    private void OnAwakenPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        awakenConfirmPopup.PopupCenteredShrink();
    }

    private void UpdateAwakenButton(MulticellularCreature player)
    {
        if (player.Species.MulticellularType == MulticellularSpeciesType.Awakened)
        {
            awakenButton.Visible = false;
            return;
        }

        float brainPower = player.Species.BrainPower;

        // TODO: require being ready to reproduce? Or do we want the player first to play as an awakened creature
        // before getting to the editor where they can still make some changes?

        // Doesn't matter as this is just for updating the GUI
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (lastBrainPower == brainPower)
            return;

        lastBrainPower = brainPower;

        var limit = Constants.BRAIN_POWER_REQUIRED_FOR_AWAKENING;

        awakenButton.Disabled = brainPower < limit;
        awakenButton.Text = TranslationServer.Translate("ACTION_AWAKEN").FormatSafe(brainPower, limit);
    }

    private void OnAwakenConfirmed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // TODO: awakening stage not done yet
        ToolTipManager.Instance.ShowPopup(TranslationServer.Translate("TO_BE_IMPLEMENTED"), 2.5f);
    }
}
