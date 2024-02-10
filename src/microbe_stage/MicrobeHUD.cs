using System;
using System.Collections.Generic;
using System.Globalization;
using Components;
using DefaultEcs;
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

    [Export]
    public NodePath StrainBarPanelPath = null!;

    [Export]
    public NodePath StrainBarPath = null!;

    [Export]
    public NodePath StrainBarFadeAnimationPlayerPath = null!;

#pragma warning disable CA2213
    [Export]
    public PackedScene WinBoxScene = null!;

    [Export]
    public Gradient StrainGradient = null!;

    // These are category keys for MouseHoverPanel
    private const string COMPOUNDS_CATEGORY = "compounds";
    private const string SPECIES_CATEGORY = "species";
    private const string FLOATING_CHUNKS_CATEGORY = "chunks";
    private const string AGENTS_CATEGORY = "agents";

    private readonly Dictionary<(string Category, LocalizedString Name), int> hoveredEntities = new();
    private readonly Dictionary<Compound, InspectedEntityLabel> hoveredCompoundControls = new();

    private ActionButton bindingModeHotkey = null!;
    private ActionButton unbindAllHotkey = null!;

    private Button multicellularButton = null!;
    private CustomWindow multicellularConfirmPopup = null!;
    private Button macroscopicButton = null!;

    private ProgressBar ingestedMatterBar = null!;

    private PanelContainer strainBarPanel = null!;
    private ProgressBar strainBar = null!;
    private AnimationPlayer strainBarFadeAnimationPlayer = null!;

    private CustomWindow? winBox;
#pragma warning restore CA2213

    /// <summary>
    ///   If not null the signaling agent radial menu is open for the given microbe, which should be the player
    /// </summary>
    private Entity? signalingAgentMenuOpenForMicrobe;

    private int? playerColonySize;

    private bool playerWasDigested;

    // These signals need to be copied to inheriting classes for Godot editor to pick them up
    [Signal]
    public new delegate void OnOpenMenu();

    [Signal]
    public new delegate void OnOpenMenuToHelp();

    [Signal]
    public delegate void OnSprintButtonPressed();

    [Signal]
    public delegate void OnToggleEngulfButtonPressed();

    [Signal]
    public delegate void OnFireToxinButtonPressed();

    [Signal]
    public delegate void OnSecreteSlimeButtonPressed();

    [Signal]
    public delegate void OnToggleBindingButtonPressed();

    [Signal]
    public delegate void OnUnbindAllButtonPressed();

    [Signal]
    public delegate void OnEjectEngulfedButtonPressed();

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

        strainBarPanel = GetNode<PanelContainer>(StrainBarPanelPath);
        strainBar = GetNode<ProgressBar>(StrainBarPath);
        strainBarFadeAnimationPlayer = GetNode<AnimationPlayer>(StrainBarFadeAnimationPlayerPath);

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
            UpdateMulticellularButton(stage.Player);
            UpdateMacroscopicButton(stage.Player);
            UpdateStrain(stage.Player);
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

    public void ShowSignalingCommandsMenu(Entity player)
    {
        if (!player.Has<CommandSignaler>())
        {
            GD.PrintErr("Can't show signaling commands for entity with no signaler component");
            return;
        }

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
    public void ApplySignalCommand(MicrobeSignalCommand? command, Entity microbe)
    {
        microbe.Get<CommandSignaler>().QueuedSignalingCommand = command;
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

    public override void UpdateFossilisationButtonStates()
    {
        var fossils = FossilisedSpecies.CreateListOfFossils(false);

        foreach (FossilisationButton button in fossilisationButtonLayer.GetChildren())
        {
            var species = button.AttachedEntity.Get<SpeciesMember>().Species;
            var alreadyFossilised =
                FossilisedSpecies.IsSpeciesAlreadyFossilised(species.FormattedName, fossils);

            SetupFossilisationButtonVisuals(button, alreadyFossilised);
        }
    }

    public override void ShowFossilisationButtons()
    {
        var fossils = FossilisedSpecies.CreateListOfFossils(false);

        foreach (var entity in stage!.WorldSimulation.EntitySystem)
        {
            // TODO: buttons to fossilize early multicellular species
            if (!entity.Has<MicrobeSpeciesMember>())
                continue;

            var species = entity.Get<SpeciesMember>().Species;

            var button = FossilisationButtonScene.Instance<FossilisationButton>();
            button.AttachedEntity = entity;
            button.Connect(nameof(FossilisationButton.OnFossilisationDialogOpened), this,
                nameof(ShowFossilisationDialog));

            var alreadyFossilised =
                FossilisedSpecies.IsSpeciesAlreadyFossilised(species.FormattedName, fossils);

            SetupFossilisationButtonVisuals(button, alreadyFossilised);

            fossilisationButtonLayer.AddChild(button);
        }
    }

    protected override void ReadPlayerHitpoints(out float hp, out float maxHealth)
    {
        ref var health = ref stage!.Player.Get<Health>();

        hp = health.CurrentHealth;
        maxHealth = health.MaxHealth;
    }

    protected override void UpdateHealth(float delta)
    {
        if (stage == null)
            throw new InvalidOperationException("UpdateHealth called before stage is set");

        // Normal health update if there is a player and the player was not engulfed
        if (stage.HasPlayer &&
            stage.Player.Get<Engulfable>().PhagocytosisStep is PhagocytosisPhase.None or PhagocytosisPhase.Ingestion)
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
        if (stage.HasPlayer)
        {
            var percentageValue = TranslationServer.Translate("PERCENTAGE_VALUE");

            // Show the digestion progress to the player
            hp = 1 - stage.Player.Get<Engulfable>().DigestedAmount;
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
        if (stage?.HasPlayer != true)
            return null;

        if (!stage.Player.Has<CompoundStorage>())
            return null;

        return stage.Player.Get<CompoundStorage>().Compounds;
    }

    protected override Func<Compound, bool> GetIsUsefulCheck()
    {
        if (!stage!.Player.Has<MicrobeColony>())
        {
            var compounds = stage.Player.Get<CompoundStorage>().Compounds;
            return compound => compounds.IsUseful(compound);
        }
        else
        {
            var compounds = stage.Player.Get<MicrobeColony>().GetCompounds();
            return compound => compounds.IsUsefulInAnyCompoundBag(compound);
        }
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
        if (!stage!.Player.Has<MicrobeColony>())
        {
            return GetPlayerUsefulCompounds()!.AreAnySpecificallySetUseful(allAgents);
        }

        return stage.Player.Get<MicrobeColony>().GetCompounds().AnyIsUsefulInAnyCompoundBag(allAgents);
    }

    protected override ICompoundStorage GetPlayerStorage()
    {
        if (!stage!.Player.Has<MicrobeColony>())
        {
            return stage.Player.Get<CompoundStorage>().Compounds;
        }

        return stage.Player.Get<MicrobeColony>().GetCompounds();
    }

    protected override void UpdateCompoundBars(float delta)
    {
        base.UpdateCompoundBars(delta);

        float maxSize;
        if (stage!.Player.Has<MicrobeColony>())
        {
            maxSize = stage.Player.Get<MicrobeColony>().CalculateTotalEngulfStorageSize();
        }
        else
        {
            maxSize = stage.Player.Get<Engulfer>().EngulfStorageSize;
        }

        ingestedMatterBar.MaxValue = maxSize;
        GUICommon.SmoothlyUpdateBar(ingestedMatterBar, GetPlayerUsedIngestionCapacity(), delta);
        ingestedMatterBar.GetNode<Label>("Value").Text = ingestedMatterBar.Value + " / " + ingestedMatterBar.MaxValue;
    }

    protected override ProcessStatistics? GetPlayerProcessStatistics()
    {
        return stage!.Player.Get<BioProcesses>().ProcessStatistics;
    }

    protected override void CalculatePlayerReproductionProgress(out Dictionary<Compound, float> gatheredCompounds,
        out IReadOnlyDictionary<Compound, float> totalNeededCompounds)
    {
        stage!.Player.Get<OrganelleContainer>().CalculateReproductionProgress(
            ref stage.Player.Get<ReproductionStatus>(), ref stage.Player.Get<SpeciesMember>(),
            stage.Player, stage.Player.Get<CompoundStorage>().Compounds, stage.GameWorld.WorldSettings,
            out gatheredCompounds, out totalNeededCompounds);
    }

    protected override void UpdateAbilitiesHotBar()
    {
        var player = stage!.Player;

        ref var organelles = ref player.Get<OrganelleContainer>();
        ref var cellProperties = ref player.Get<CellProperties>();
        ref var control = ref player.Get<MicrobeControl>();
        ref var species = ref player.Get<SpeciesMember>();

        bool showToxin;
        bool showSlime;

        bool engulfing;

        // Multicellularity is not checked here (only colony membership) as that is also not checked when firing toxins
        if (player.Has<MicrobeColony>())
        {
            ref var colony = ref player.Get<MicrobeColony>();

            // TODO: does this need a variant that just returns a bool and has an early exit?
            colony.CalculateColonySpecialOrganelles(out var vacuoles, out var slimeJets);

            showToxin = vacuoles > 0;
            showSlime = slimeJets > 0;

            engulfing = colony.ColonyState == MicrobeState.Engulf;
        }
        else
        {
            showToxin = organelles.AgentVacuoleCount > 0;
            showSlime = organelles.SlimeJets is { Count: > 0 };

            engulfing = control.State == MicrobeState.Engulf;
        }

        bool isDigesting = false;

        ref var engulfer = ref stage.Player.Get<Engulfer>();

        if (engulfer.EngulfedObjects is { Count: > 0 })
            isDigesting = true;

        // Read the engulf state from the colony as the player cell might be unable to engulf but some
        // member might be able to
        UpdateBaseAbilitiesBar(cellProperties.CanEngulfInColony(player), showToxin, showSlime,
            organelles.HasSignalingAgent, engulfing, control.Sprinting, isDigesting);

        bindingModeHotkey.Visible = organelles.CanBind(ref species);
        unbindAllHotkey.Visible = organelles.CanUnbind(ref species, player);

        bindingModeHotkey.Pressed = control.State == MicrobeState.Binding;
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

        foreach (var entity in stage.HoverInfo.Entities)
        {
            if (!entity.Has<ReadableName>())
                continue;

            var name = entity.Get<ReadableName>().Name;

            if (entity.Has<PlayerMarker>())
            {
                // Special handling for player
                var label = mouseHoverPanel.AddItem(SPECIES_CATEGORY, name.ToString());
                label.SetDescription(TranslationServer.Translate("PLAYER"));
                continue;
            }

            string category;

            if (entity.Has<SpeciesMember>())
            {
                category = SPECIES_CATEGORY;
            }
            else if (entity.Has<ToxinDamageSource>())
            {
                category = AGENTS_CATEGORY;
            }
            else
            {
                // Assume this is a chunk, chunks don't have really good identifying component on them
                category = FLOATING_CHUNKS_CATEGORY;
            }

            var key = (category, name);
            hoveredEntities.TryGetValue(key, out int count);
            hoveredEntities[key] = count + 1;
        }

        foreach (var hoveredEntity in hoveredEntities)
        {
            var item = mouseHoverPanel.AddItem(hoveredEntity.Key.Category, hoveredEntity.Key.Name.ToString());

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
                StrainBarPanelPath.Dispose();
                StrainBarPath.Dispose();
                StrainBarFadeAnimationPlayerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateStrain(Entity player)
    {
        var strainFraction = player.Get<StrainAffected>().CalculateStrainFraction();
        atpBar.TintProgress = StrainGradient.Interpolate(strainFraction);

        strainBar.Value = strainFraction;

        switch (Settings.Instance.StrainBarVisibilityMode.Value)
        {
            case Settings.StrainBarVisibility.Off:
                strainBarPanel.Hide();
                break;
            case Settings.StrainBarVisibility.VisibleWhenCloseToFull:
                AnimateStrainBarPanel(strainFraction, 0.65f);
                break;
            case Settings.StrainBarVisibility.VisibleWhenOverZero:
                AnimateStrainBarPanel(strainFraction, 0.05f);
                break;
            case Settings.StrainBarVisibility.AlwaysVisible:
                strainBarPanel.Show();
                break;
        }
    }

    private void AnimateStrainBarPanel(float strainFraction, float minimum)
    {
        var shouldBeVisible = strainFraction >= minimum;

        if (!strainBarPanel.Visible && shouldBeVisible)
        {
            strainBarFadeAnimationPlayer.Play("FadeIn");
        }
        else if (strainBarPanel.Visible && !shouldBeVisible)
        {
            strainBarFadeAnimationPlayer.Play("FadeOut");
        }
    }

    /// <summary>
    ///   Sets button's texture and hint based on its status of fossilisation
    /// </summary>
    private void SetupFossilisationButtonVisuals(FossilisationButton button, bool alreadyFossilised)
    {
        // Display a faded button with a different hint if the species has been fossilised.
        button.AlreadyFossilised = alreadyFossilised;
        button.HintTooltip = alreadyFossilised ?
            TranslationServer.Translate("FOSSILISATION_HINT_ALREADY_FOSSILISED") :
            TranslationServer.Translate("FOSSILISATION_HINT");
    }

    private void OnRadialItemSelected(int itemId)
    {
        if (signalingAgentMenuOpenForMicrobe != null)
        {
            ApplySignalCommand((MicrobeSignalCommand)itemId, signalingAgentMenuOpenForMicrobe.Value);
            return;
        }

        GD.PrintErr("Unexpected radial menu item selection signal");
    }

    private float GetPlayerUsedIngestionCapacity()
    {
        if (stage!.Player.Has<MicrobeColony>())
            return stage.Player.Get<MicrobeColony>().CalculateUsedIngestionCapacity();

        return stage.Player.Get<Engulfer>().UsedIngestionCapacity;
    }

    private void UpdateMulticellularButton(Entity player)
    {
        if (stage == null)
            throw new InvalidOperationException("Can't update multicellular button without stage set");

        if (!player.Has<MicrobeColony>())
        {
            multicellularButton.Visible = false;
            return;
        }

        if (stage.Player.Get<SpeciesMember>().Species is not MicrobeSpecies ||
            !stage.CurrentGame!.GameWorld.WorldSettings.IncludeMulticellular || stage.CurrentGame!.FreeBuild)
        {
            multicellularButton.Visible = false;
            return;
        }

        multicellularButton.Visible = true;

        ref var colony = ref player.Get<MicrobeColony>();

        var newColonySize = colony.ColonyMembers.Length;

        if (stage.MovingToEditor)
        {
            multicellularButton.Disabled = true;
        }
        else
        {
            bool canBecomeMulticellular = newColonySize >= Constants.COLONY_SIZE_REQUIRED_FOR_MULTICELLULAR;
            multicellularButton.Disabled = !canBecomeMulticellular;

            if (stage.CurrentGame.TutorialState.Enabled && canBecomeMulticellular)
            {
                stage.CurrentGame.TutorialState.SendEvent(TutorialEventType.MicrobeBecomeMulticellularAvailable,
                    EventArgs.Empty, this);
            }
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

    private void UpdateMacroscopicButton(Entity player)
    {
        if (stage == null)
            throw new InvalidOperationException("Can't update macroscopic button without stage set");

        if (!player.Has<MicrobeColony>())
        {
            macroscopicButton.Visible = false;
            return;
        }

        if (stage.Player.Get<SpeciesMember>().Species is not EarlyMulticellularSpecies || stage.CurrentGame!.FreeBuild)
        {
            macroscopicButton.Visible = false;
            return;
        }

        macroscopicButton.Visible = true;

        ref var colony = ref player.Get<MicrobeColony>();

        var newColonySize = colony.ColonyMembers.Length;

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

        if (stage == null)
        {
            GD.PrintErr("Stage has disappeared");
            return;
        }

        if (!stage.HasPlayer || playerColonySize is null or < Constants.COLONY_SIZE_REQUIRED_FOR_MULTICELLULAR)
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

        if (stage == null)
        {
            GD.PrintErr("Stage has disappeared");
            return;
        }

        if (!stage.HasPlayer || stage.Player.Get<SpeciesMember>().Species is not EarlyMulticellularSpecies ||
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

    private void OnSprintPressed()
    {
        EmitSignal(nameof(OnSprintButtonPressed));
    }

    private void OnEngulfmentPressed()
    {
        EmitSignal(nameof(OnToggleEngulfButtonPressed));
    }

    private void OnFireToxinPressed()
    {
        EmitSignal(nameof(OnFireToxinButtonPressed));
    }

    private void OnBindingModePressed()
    {
        EmitSignal(nameof(OnToggleBindingButtonPressed));
    }

    private void OnUnbindAllPressed()
    {
        EmitSignal(nameof(OnUnbindAllButtonPressed));
    }

    private void OnSecreteSlimePressed()
    {
        EmitSignal(nameof(OnSecreteSlimeButtonPressed));
    }

    private void OnEjectEngulfedPressed()
    {
        EmitSignal(nameof(OnEjectEngulfedButtonPressed));
    }
}
