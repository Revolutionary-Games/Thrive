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
public class MicrobeHUD : CreatureStageHUDBase<MicrobeStage>
{
    [Export]
    public NodePath? MulticellularButtonPath;

    [Export]
    public NodePath MulticellularConfirmPopupPath = null!;

    [Export]
    public NodePath MacroscopicButtonPath = null!;

    [Export]
    public NodePath IngestedMatterBarPath = null!;

    [Export]
    public NodePath BindingModeHotkeyPath = null!;

    [Export]
    public NodePath UnbindAllHotkeyPath = null!;

#pragma warning disable CA2213
    [Export]
    public PackedScene WinBoxScene = null!;

    // These are category keys for MouseHoverPanel
    private const string COMPOUNDS_CATEGORY = "compounds";
    private const string SPECIES_CATEGORY = "species";
    private const string FLOATING_CHUNKS_CATEGORY = "chunks";
    private const string AGENTS_CATEGORY = "agents";

    private readonly Dictionary<(string Category, string Name), int> hoveredEntities = new();
    private readonly Dictionary<Compound, InspectedEntityLabel> hoveredCompoundControls = new();

    private ActionButton bindingModeHotkey = null!;
    private ActionButton unbindAllHotkey = null!;

    private Button multicellularButton = null!;
    private CustomWindow multicellularConfirmPopup = null!;
    private Button macroscopicButton = null!;

    private ProgressBar ingestedMatterBar = null!;

    private CustomWindow? winBox;
#pragma warning restore CA2213

    /// <summary>
    ///   If not null the signaling agent radial menu is open for the given microbe, which should be the player
    /// </summary>
    private Microbe? signalingAgentMenuOpenForMicrobe;

    private int? playerColonySize;

    private bool playerWasDigested;

    // These signals need to be copied to inheriting classes for Godot editor to pick them up
    [Signal]
    public new delegate void OnOpenMenu();

    [Signal]
    public new delegate void OnOpenMenuToHelp();

    protected override string? UnPauseHelpText => TranslationServer.Translate("PAUSE_PROMPT");

    public override void _Ready()
    {
        base._Ready();
        ingestedMatterBar = GetNode<ProgressBar>(IngestedMatterBarPath);

        multicellularButton = GetNode<Button>(MulticellularButtonPath);
        multicellularConfirmPopup = GetNode<CustomWindow>(MulticellularConfirmPopupPath);
        macroscopicButton = GetNode<Button>(MacroscopicButtonPath);

        bindingModeHotkey = GetNode<ActionButton>(BindingModeHotkeyPath);
        unbindAllHotkey = GetNode<ActionButton>(UnbindAllHotkeyPath);

        mouseHoverPanel.AddCategory(COMPOUNDS_CATEGORY, new LocalizedString("COMPOUNDS_COLON"));
        mouseHoverPanel.AddCategory(SPECIES_CATEGORY, new LocalizedString("SPECIES_COLON"));
        mouseHoverPanel.AddCategory(FLOATING_CHUNKS_CATEGORY, new LocalizedString("FLOATING_CHUNKS_COLON"));
        mouseHoverPanel.AddCategory(AGENTS_CATEGORY, new LocalizedString("AGENTS_COLON"));

        foreach (var compound in SimulationParameters.Instance.GetCloudCompounds())
        {
            var hoveredCompoundControl = mouseHoverPanel.AddItem(
                COMPOUNDS_CATEGORY, compound.Name, compound.LoadedIcon);
            hoveredCompoundControls.Add(compound, hoveredCompoundControl);
        }

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

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationTranslationChanged)
        {
            UpdateColonySizeForMulticellular();
            UpdateColonySizeForMacroscopic();
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

        winBox = WinBoxScene.Instance<CustomWindow>();
        winExtinctBoxHolder.AddChild(winBox);
        winBox.Show();

        winBox.GetNode<Timer>("Timer").Connect("timeout", this, nameof(ToggleWinBox));
    }

    public override void ShowFossilisationButtons()
    {
        var microbes = GetTree().GetNodesInGroup(Constants.AI_TAG_MICROBE).Cast<Microbe>();
        var fossils = FossilisedSpecies.CreateListOfFossils(false);
        foreach (var microbe in microbes)
        {
            if (microbe.Species is not MicrobeSpecies)
                continue;

            var button = FossilisationButtonScene.Instance<FossilisationButton>();
            button.AttachedEntity = microbe;
            button.Connect(nameof(FossilisationButton.OnFossilisationDialogOpened), this,
                nameof(ShowFossilisationDialog));

            // Display a faded button with a different hint if the species has been fossilised.
            var alreadyFossilised =
                FossilisedSpecies.IsSpeciesAlreadyFossilised(microbe.Species.FormattedName, fossils);
            button.AlreadyFossilised = alreadyFossilised;
            button.HintTooltip = alreadyFossilised ?
                TranslationServer.Translate("FOSSILISATION_HINT_ALREADY_FOSSILISED") :
                TranslationServer.Translate("FOSSILISATION_HINT");

            fossilisationButtonLayer.AddChild(button);
        }
    }

    protected override void ReadPlayerHitpoints(out float hp, out float maxHP)
    {
        hp = stage!.Player!.Hitpoints;
        maxHP = stage.Player.MaxHitpoints;
    }

    protected override void UpdateHealth(float delta)
    {
        if (stage?.Player != null && stage.Player.PhagocytosisStep != PhagocytosisPhase.Ingested)
        {
            playerWasDigested = false;
            healthBar.TintProgress = defaultHealthBarColour;
            base.UpdateHealth(delta);
            return;
        }

        float hp = 0;

        string hpText = playerWasDigested ?
            TranslationServer.Translate("DEVOURED") :
            hp.ToString(CultureInfo.CurrentCulture);

        // Update to the player's current digested progress, unless the player does not exist
        if (stage!.HasPlayer)
        {
            var percentageValue = TranslationServer.Translate("PERCENTAGE_VALUE");

            // Show the digestion progress to the player
            hp = 1 - (stage.Player!.DigestedAmount / Constants.PARTIALLY_DIGESTED_THRESHOLD);
            maxHP = Constants.FULLY_DIGESTED_LIMIT;
            hpText = percentageValue.FormatSafe(Mathf.Round((1 - hp) * 100));
            playerWasDigested = true;
            FlashHealthBar(new Color(0.96f, 0.5f, 0.27f), delta);
        }

        healthBar.MaxValue = maxHP;
        GUICommon.SmoothlyUpdateBar(healthBar, hp, delta);
        hpLabel.Text = hpText;
        hpLabel.HintTooltip = hpText;
    }

    protected override CompoundBag? GetPlayerUsefulCompounds()
    {
        return stage!.Player?.Compounds;
    }

    protected override Func<Compound, bool> GetIsUsefulCheck()
    {
        var colony = stage!.Player!.Colony;
        if (colony == null)
        {
            var compounds = stage.Player.Compounds;
            return compound => compounds.IsUseful(compound);
        }

        return compound => colony.ColonyMembers.Any(c => c.Compounds.IsUseful(compound));
    }

    protected override bool SpecialHandleBar(ProgressBar bar)
    {
        if (bar == ingestedMatterBar)
        {
            bar.Visible = GetPlayerUsedIngestionCapacity() > 0;
            return true;
        }

        return false;
    }

    protected override bool ShouldShowAgentsPanel()
    {
        var colony = stage!.Player!.Colony;
        if (colony == null)
        {
            return GetPlayerUsefulCompounds()!.AreAnySpecificallySetUseful(allAgents);
        }

        return colony.ColonyMembers.Any(
            c => c.Compounds.AreAnySpecificallySetUseful(allAgents));
    }

    protected override ICompoundStorage GetPlayerStorage()
    {
        return stage!.Player!.Colony?.ColonyCompounds ?? (ICompoundStorage)stage.Player.Compounds;
    }

    protected override void UpdateCompoundBars(float delta)
    {
        base.UpdateCompoundBars(delta);

        ingestedMatterBar.MaxValue = stage!.Player!.Colony?.HexCount ?? stage.Player.HexCount;
        GUICommon.SmoothlyUpdateBar(ingestedMatterBar, GetPlayerUsedIngestionCapacity(), delta);
        ingestedMatterBar.GetNode<Label>("Value").Text = ingestedMatterBar.Value + " / " + ingestedMatterBar.MaxValue;
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

    protected override void UpdateAbilitiesHotBar()
    {
        var player = stage!.Player!;

        bool showToxin;
        bool showSlime;

        // Multicellularity is not checked here (only colony membership) as that is also not checked when firing toxins
        if (player.Colony != null)
        {
            showToxin = player.Colony.ColonyMembers.Any(c => c.AgentVacuoleCount > 0);
            showSlime = player.Colony.ColonyMembers.Any(c => c.SlimeJets.Count > 0);
        }
        else
        {
            showToxin = player.AgentVacuoleCount > 0;
            showSlime = player.SlimeJets.Count > 0;
        }

        UpdateBaseAbilitiesBar(player.CanEngulfInColony(), showToxin, showSlime,
            player.HasSignalingAgent, player.State == MicrobeState.Engulf);

        bindingModeHotkey.Visible = player.CanBind;
        unbindAllHotkey.Visible = player.CanUnbind;

        bindingModeHotkey.Pressed = player.State == MicrobeState.Binding;
        unbindAllHotkey.Pressed = Input.IsActionPressed(unbindAllHotkey.ActionName);
    }

    protected override void UpdateHoverInfo(float delta)
    {
        stage!.HoverInfo.Process(delta);

        // Show hovered compound information in GUI
        foreach (var compound in hoveredCompoundControls)
        {
            var compoundControl = compound.Value;
            stage.HoverInfo.HoveredCompounds.TryGetValue(compound.Key, out float amount);

            // It is not useful to show trace amounts of a compound, so those are skipped
            if (amount < Constants.COMPOUND_DENSITY_CATEGORY_VERY_LITTLE)
            {
                compoundControl.Visible = false;
                continue;
            }

            compoundControl.SetText(compound.Key.Name);
            compoundControl.SetDescription(GetCompoundDensityCategory(amount) ?? TranslationServer.Translate("N_A"));
            compoundControl.SetDescriptionColor(GetCompoundDensityCategoryColor(amount));
            compoundControl.Visible = true;
        }

        // Refresh list
        mouseHoverPanel.ClearEntries(SPECIES_CATEGORY);
        mouseHoverPanel.ClearEntries(FLOATING_CHUNKS_CATEGORY);
        mouseHoverPanel.ClearEntries(AGENTS_CATEGORY);

        // Show the entity's name and count of hovered entities
        hoveredEntities.Clear();

        foreach (var entity in stage.HoverInfo.InspectableEntities)
        {
            var category = string.Empty;

            if (entity is Microbe microbe)
            {
                if (microbe.IsPlayerMicrobe)
                {
                    // Special handling for player
                    var label = mouseHoverPanel.AddItem(SPECIES_CATEGORY, entity.ReadableName);
                    label.SetDescription(TranslationServer.Translate("PLAYER"));
                    continue;
                }

                category = SPECIES_CATEGORY;
            }
            else if (entity is FloatingChunk)
            {
                category = FLOATING_CHUNKS_CATEGORY;
            }
            else if (entity is AgentProjectile)
            {
                category = AGENTS_CATEGORY;
            }

            var key = (category, entity.ReadableName);
            hoveredEntities.TryGetValue(key, out int count);
            hoveredEntities[key] = count + 1;
        }

        foreach (var hoveredEntity in hoveredEntities)
        {
            var item = mouseHoverPanel.AddItem(hoveredEntity.Key.Category, hoveredEntity.Key.Name);

            if (hoveredEntity.Value > 1)
                item.SetDescription(TranslationServer.Translate("N_TIMES").FormatSafe(hoveredEntity.Value));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (MulticellularButtonPath != null)
            {
                MulticellularButtonPath.Dispose();
                MulticellularConfirmPopupPath.Dispose();
                MacroscopicButtonPath.Dispose();
                IngestedMatterBarPath.Dispose();
                BindingModeHotkeyPath.Dispose();
                UnbindAllHotkeyPath.Dispose();
            }
        }

        base.Dispose(disposing);
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

    private float GetPlayerUsedIngestionCapacity()
    {
        return stage!.Player!.Colony?.UsedIngestionCapacity ?? stage.Player.UsedIngestionCapacity;
    }

    private void UpdateMulticellularButton(Microbe player)
    {
        if (stage == null)
            throw new InvalidOperationException("Can't update multicellular button without stage set");

        if (player.Colony == null || player.IsMulticellular ||
            !stage.CurrentGame!.GameWorld.WorldSettings.IncludeMulticellular || stage.CurrentGame!.FreeBuild)
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

        multicellularButton.Text = TranslationServer.Translate("BECOME_MULTICELLULAR")
            .FormatSafe(playerColonySize, Constants.COLONY_SIZE_REQUIRED_FOR_MULTICELLULAR);
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

        macroscopicButton.Text = TranslationServer.Translate("BECOME_MACROSCOPIC")
            .FormatSafe(playerColonySize, Constants.COLONY_SIZE_REQUIRED_FOR_MACROSCOPIC);
    }

    private void OnBecomeMulticellularPressed()
    {
        if (!Paused)
        {
            PauseButtonPressed(true);
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
            PauseButtonPressed(false);
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
