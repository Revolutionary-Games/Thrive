using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Manages the microbe HUD
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class MicrobeHUD : StageHUDBase<MicrobeStage>
{
    [Export]
    public NodePath MulticellularButtonPath = null!;

    [Export]
    public NodePath MulticellularConfirmPopupPath = null!;

    [Export]
    public NodePath MacroscopicButtonPath = null!;

    [Export]
    public PackedScene WinBoxScene = null!;

    [Export]
    public NodePath BindingModeHotkeyPath = null!;

    [Export]
    public NodePath UnbindAllHotkeyPath = null!;

    private ActionButton bindingModeHotkey = null!;
    private ActionButton unbindAllHotkey = null!;

    private Button multicellularButton = null!;
    private CustomDialog multicellularConfirmPopup = null!;
    private Button macroscopicButton = null!;

    private CustomDialog? winBox;

    private int? playerColonySize;

    /// <summary>
    ///   If not null the signaling agent radial menu is open for the given microbe, which should be the player
    /// </summary>
    private Microbe? signalingAgentMenuOpenForMicrobe;

    // These signals need to be copied to inheriting classes for Godot editor to pick them up
    [Signal]
    public new delegate void OnOpenMenu();

    [Signal]
    public new delegate void OnOpenMenuToHelp();

    protected override string? UnPauseHelpText => TranslationServer.Translate("PAUSE_PROMPT");

    public override void _Ready()
    {
        base._Ready();

        multicellularButton = GetNode<Button>(MulticellularButtonPath);
        multicellularConfirmPopup = GetNode<CustomDialog>(MulticellularConfirmPopupPath);
        macroscopicButton = GetNode<Button>(MacroscopicButtonPath);

        bindingModeHotkey = GetNode<ActionButton>(BindingModeHotkeyPath);
        unbindAllHotkey = GetNode<ActionButton>(UnbindAllHotkeyPath);

        multicellularButton.Visible = false;
        macroscopicButton.Visible = false;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (stage == null)
            return;

        if (stage.HasPlayer)
        {
            UpdateMulticellularButton(stage.Player!);
            UpdateMacroscopicButton(stage.Player!);
        }
        else
        {
            multicellularButton.Visible = false;
            macroscopicButton.Visible = false;
        }
    }

    public void ShowSignalingCommandsMenu(Microbe player)
    {
        if (packControlRadial.Visible)
        {
            GD.PrintErr("Radial menu is already open for signaling commands");
            return;
        }

        var choices = new List<(string Text, int Id)>
        {
            (TranslationServer.Translate("SIGNAL_COMMAND_NONE"), (int)MicrobeSignalCommand.None),
            (TranslationServer.Translate("SIGNAL_COMMAND_FOLLOW"), (int)MicrobeSignalCommand.FollowMe),
            (TranslationServer.Translate("SIGNAL_COMMAND_TO_ME"), (int)MicrobeSignalCommand.MoveToMe),
            (TranslationServer.Translate("SIGNAL_COMMAND_FLEE"), (int)MicrobeSignalCommand.FleeFromMe),
            (TranslationServer.Translate("SIGNAL_COMMAND_AGGRESSION"), (int)MicrobeSignalCommand.BecomeAggressive),
        };

        packControlRadial.Radial.CenterText = TranslationServer.Translate("SIGNAL_TO_EMIT");

        signalingAgentMenuOpenForMicrobe = player;
        packControlRadial.ShowWithItems(choices);
    }

    public MicrobeSignalCommand? SelectSignalCommandIfOpen()
    {
        // Return nothing if not open
        if (!packControlRadial.Visible)
            return null;

        var item = packControlRadial.Radial.HoveredItem;

        packControlRadial.Hide();

        if (item == null)
            return null;

        return (MicrobeSignalCommand)item.Value;
    }

    /// <summary>
    ///   Applies a signaling command to microbe. This is here as the user can actively select a radial menu item
    /// </summary>
    /// <param name="command">The command to apply</param>
    /// <param name="microbe">The target microbe</param>
    public void ApplySignalCommand(MicrobeSignalCommand? command, Microbe microbe)
    {
        microbe.QueuedSignalingCommand = command;
        signalingAgentMenuOpenForMicrobe = null;
    }

    public void ToggleWinBox()
    {
        if (winBox != null)
        {
            winExtinctBoxHolder.Hide();
            winBox.DetachAndQueueFree();
            return;
        }

        winExtinctBoxHolder.Show();

        winBox = WinBoxScene.Instance<CustomDialog>();
        winExtinctBoxHolder.AddChild(winBox);
        winBox.Show();

        winBox.GetNode<Timer>("Timer").Connect("timeout", this, nameof(ToggleWinBox));
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationTranslationChanged)
        {
            UpdateColonySizeForMulticellular();
            UpdateColonySizeForMacroscopic();
        }
    }

    protected override void ReadPlayerHitpoints(out float hp, out float maxHP)
    {
        hp = stage!.Player!.Hitpoints;
        maxHP = stage.Player.MaxHitpoints;
    }

    protected override CompoundBag? GetPlayerUsefulCompounds()
    {
        return stage!.Player?.Compounds;
    }

    protected override ICompoundStorage GetPlayerColonyOrPlayerStorage()
    {
        return stage!.Player!.Colony?.ColonyCompounds ?? (ICompoundStorage)stage.Player.Compounds;
    }

    protected override ProcessStatistics? GetPlayerProcessStatistics()
    {
        return stage!.Player!.ProcessStatistics;
    }

    protected override void CalculatePlayerReproductionProgress(out Dictionary<Compound, float> gatheredCompounds,
        out Dictionary<Compound, float> totalNeededCompounds)
    {
        stage!.Player!.CalculateReproductionProgress(out gatheredCompounds, out totalNeededCompounds);
    }

    protected override IEnumerable<(bool Player, Species Species)> GetHoveredSpecies()
    {
        return stage!.HoverInfo.HoveredMicrobes.Select(m => (m.IsPlayerMicrobe, m.Species));
    }

    protected override IReadOnlyDictionary<Compound, float> GetHoveredCompounds()
    {
        return stage!.HoverInfo.HoveredCompounds;
    }

    protected override string GetMouseHoverCoordinateText()
    {
        return string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("STUFF_AT"),
            stage!.Camera.CursorWorldPos.x, stage.Camera.CursorWorldPos.z);
    }

    protected override void UpdateAbilitiesHotBar()
    {
        var player = stage!.Player!;
        UpdateBaseAbilitiesBar(!player.CellTypeProperties.MembraneType.CellWall, player.AgentVacuoleCount > 0,
            player.HasSignalingAgent, player.State == Microbe.MicrobeState.Engulf);

        bindingModeHotkey.Visible = player.CanBind;
        unbindAllHotkey.Visible = player.CanUnbind;

        bindingModeHotkey.Pressed = player.State == Microbe.MicrobeState.Binding;
        unbindAllHotkey.Pressed = Input.IsActionPressed(unbindAllHotkey.ActionName);
    }

    private void OnRadialItemSelected(int itemId)
    {
        if (signalingAgentMenuOpenForMicrobe != null)
        {
            ApplySignalCommand((MicrobeSignalCommand)itemId, signalingAgentMenuOpenForMicrobe);
            return;
        }

        GD.PrintErr("Unexpected radial menu item selection signal");
    }

    private void UpdateMulticellularButton(Microbe player)
    {
        if (stage == null)
            throw new InvalidOperationException("Can't update multicellular button without stage set");

        if (player.Colony == null || player.IsMulticellular || stage.CurrentGame!.FreeBuild)
        {
            multicellularButton.Visible = false;
            return;
        }

        multicellularButton.Visible = true;

        var newColonySize = player.Colony.ColonyMembers.Count;

        if (stage.MovingToEditor)
        {
            multicellularButton.Disabled = true;
        }
        else
        {
            multicellularButton.Disabled = newColonySize < Constants.COLONY_SIZE_REQUIRED_FOR_MULTICELLULAR;
        }

        UpdateColonySize(newColonySize);
    }

    private void UpdateColonySize(int newColonySize)
    {
        if (newColonySize != playerColonySize)
        {
            playerColonySize = newColonySize;
            UpdateColonySizeForMulticellular();
            UpdateColonySizeForMacroscopic();
        }
    }

    private void UpdateColonySizeForMulticellular()
    {
        if (playerColonySize == null)
            return;

        multicellularButton.Text = string.Format(TranslationServer.Translate("BECOME_MULTICELLULAR"), playerColonySize,
            Constants.COLONY_SIZE_REQUIRED_FOR_MULTICELLULAR);
    }

    private void UpdateMacroscopicButton(Microbe player)
    {
        if (stage == null)
            throw new InvalidOperationException("Can't update macroscopic button without stage set");

        if (player.Colony == null || !player.IsMulticellular || stage.CurrentGame!.FreeBuild)
        {
            macroscopicButton.Visible = false;
            return;
        }

        macroscopicButton.Visible = true;

        var newColonySize = player.Colony.ColonyMembers.Count;

        if (stage.MovingToEditor)
        {
            macroscopicButton.Disabled = true;
        }
        else
        {
            macroscopicButton.Disabled = newColonySize < Constants.COLONY_SIZE_REQUIRED_FOR_MACROSCOPIC;
        }

        UpdateColonySize(newColonySize);
    }

    private void UpdateColonySizeForMacroscopic()
    {
        if (playerColonySize == null)
            return;

        macroscopicButton.Text = string.Format(TranslationServer.Translate("BECOME_MACROSCOPIC"), playerColonySize,
            Constants.COLONY_SIZE_REQUIRED_FOR_MACROSCOPIC);
    }

    private void OnBecomeMulticellularPressed()
    {
        if (!Paused)
        {
            bottomLeftBar.Paused = true;
        }
        else
        {
            GUICommon.Instance.PlayButtonPressSound();
        }

        multicellularConfirmPopup.PopupCenteredShrink();
    }

    private void OnBecomeMulticellularCancelled()
    {
        // The game should have been paused already but just in case
        if (Paused)
        {
            bottomLeftBar.Paused = false;
        }
    }

    private void OnBecomeMulticellularConfirmed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (stage?.Player == null || playerColonySize is null or < Constants.COLONY_SIZE_REQUIRED_FOR_MULTICELLULAR)
        {
            GD.Print("Player is no longer eligible to move to multicellular stage");
            return;
        }

        GD.Print("Becoming multicellular. NOTE: game is moving to prototype parts of the game, " +
            "expect non-finished and buggy things!");

        // To prevent being clicked twice
        multicellularButton.Disabled = true;

        EnsureGameIsUnpausedForEditor();

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.3f, stage.MoveToMulticellular, false);

        stage.MovingToEditor = true;
    }

    private void OnBecomeMacroscopicPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (stage?.Player?.IsMulticellular != true ||
            playerColonySize is null or < Constants.COLONY_SIZE_REQUIRED_FOR_MACROSCOPIC)
        {
            GD.Print("Player is no longer eligible to move to late multicellular stage");
            return;
        }

        GD.Print("Becoming macroscopic");

        // To prevent being clicked twice
        macroscopicButton.Disabled = true;

        EnsureGameIsUnpausedForEditor();

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.3f, stage.MoveToMacroscopic, false);

        stage.MovingToEditor = true;
    }
}
