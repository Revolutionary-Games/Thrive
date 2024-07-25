using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Components;
using DefaultEcs;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Manages the microbe HUD
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public partial class MicrobeHUD : CreatureStageHUDBase<MicrobeStage>
{
    [Export(PropertyHint.ColorNoAlpha)]
    public Color IngestedMatterBarFillColour = new(0.88f, 0.49f, 0.49f);

#pragma warning disable CA2213
    [Export]
    public PackedScene WinBoxScene = null!;

    // These are category keys for MouseHoverPanel
    private const string COMPOUNDS_CATEGORY = "compounds";
    private const string SPECIES_CATEGORY = "species";
    private const string FLOATING_CHUNKS_CATEGORY = "chunks";
    private const string AGENTS_CATEGORY = "agents";

    private readonly Dictionary<(string Category, LocalizedString Name), int> hoveredEntities = new();
    private readonly Dictionary<Compound, InspectedEntityLabel> hoveredCompoundControls = new();

    [Export]
    private ActionButton bindingModeHotkey = null!;

    [Export]
    private ActionButton unbindAllHotkey = null!;

    [Export]
    private Button multicellularButton = null!;

    [Export]
    private CustomWindow multicellularConfirmPopup = null!;

    [Export]
    private Button macroscopicButton = null!;

    private CompoundProgressBar ingestedMatterBar = null!;

    private CustomWindow? winBox;
#pragma warning restore CA2213

    /// <summary>
    ///   If not null the signaling agent radial menu is open for the given microbe, which should be the player
    /// </summary>
    private Entity? signalingAgentMenuOpenForMicrobe;

    private int? playerColonySize;

    private bool playerWasDigested;

    /// <summary>
    ///   Wether or not the player has the <see cref="StrainAffected"/> component, if not an error will be printed
    ///   and updating the bar will be ignored
    /// </summary>
    private bool playerMissingStrainAffected;

    [Signal]
    public delegate void OnToggleEngulfButtonPressedEventHandler();

    [Signal]
    public delegate void OnFireToxinButtonPressedEventHandler();

    [Signal]
    public delegate void OnSecreteSlimeButtonPressedEventHandler();

    [Signal]
    public delegate void OnToggleBindingButtonPressedEventHandler();

    [Signal]
    public delegate void OnUnbindAllButtonPressedEventHandler();

    [Signal]
    public delegate void OnEjectEngulfedButtonPressedEventHandler();

    [Signal]
    public delegate void OnSprintButtonPressedEventHandler();

    protected override string UnPauseHelpText => Localization.Translate("PAUSE_PROMPT");

    public override void _Ready()
    {
        base._Ready();

        var barScene = GD.Load<PackedScene>("res://src/microbe_stage/gui/CompoundProgressBar.tscn");

        ingestedMatterBar = CompoundProgressBar.Create(barScene,
            GD.Load<Texture2D>("res://assets/textures/gui/bevel/ingestedmatter.png"),
            new LocalizedString("INGESTED_MATTER"), 0, 1);
        ingestedMatterBar.FillColour = IngestedMatterBarFillColour;

        compoundsPanel.AddPrimaryBar(ingestedMatterBar);
        ingestedMatterBar.Visible = false;

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

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (stage == null)
            return;

        if (stage.HasPlayer)
        {
            UpdateMulticellularButton(stage.Player);
            UpdateMacroscopicButton(stage.Player);
        }
        else
        {
            multicellularButton.Visible = false;
            macroscopicButton.Visible = false;
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
            (Localization.Translate("SIGNAL_COMMAND_NONE"), (int)MicrobeSignalCommand.None),
            (Localization.Translate("SIGNAL_COMMAND_FOLLOW"), (int)MicrobeSignalCommand.FollowMe),
            (Localization.Translate("SIGNAL_COMMAND_TO_ME"), (int)MicrobeSignalCommand.MoveToMe),
            (Localization.Translate("SIGNAL_COMMAND_FLEE"), (int)MicrobeSignalCommand.FleeFromMe),
            (Localization.Translate("SIGNAL_COMMAND_AGGRESSION"), (int)MicrobeSignalCommand.BecomeAggressive),
        };

        packControlRadial.Radial.CenterText = Localization.Translate("SIGNAL_TO_EMIT");

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

        winBox = WinBoxScene.Instantiate<CustomWindow>();
        winExtinctBoxHolder.AddChild(winBox);
        winBox.Show();

        winBox.GetNode<Timer>("Timer").Connect(Timer.SignalName.Timeout, new Callable(this, nameof(ToggleWinBox)));
    }

    protected override void UpdateFossilisationButtonStates()
    {
        var fossils = FossilisedSpecies.CreateListOfFossils(false);

        foreach (var button in fossilisationButtonLayer.GetChildren().OfType<FossilisationButton>())
        {
            var species = button.AttachedEntity.Get<SpeciesMember>().Species;
            var alreadyFossilised =
                FossilisedSpecies.IsSpeciesAlreadyFossilised(species.FormattedName, fossils);

            SetupFossilisationButtonVisuals(button, alreadyFossilised);
        }
    }

    protected override void ShowFossilisationButtons()
    {
        var fossils = FossilisedSpecies.CreateListOfFossils(false);

        foreach (var entity in stage!.WorldSimulation.EntitySystem)
        {
            // TODO: buttons to fossilize early multicellular species
            if (!entity.Has<MicrobeSpeciesMember>())
                continue;

            var species = entity.Get<SpeciesMember>().Species;

            var button = FossilisationButtonScene.Instantiate<FossilisationButton>();
            button.AttachedEntity = entity;
            button.Connect(FossilisationButton.SignalName.OnFossilisationDialogOpened, new Callable(this,
                nameof(ShowFossilisationDialog)));

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
            Localization.Translate("DEVOURED") :
            hp.ToString(CultureInfo.CurrentCulture);

        // Update to the player's current digested progress, unless the player does not exist
        if (stage.HasPlayer)
        {
            var percentageValue = Localization.Translate("PERCENTAGE_VALUE");

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
        hpLabel.TooltipText = hpText;
    }

    protected override CompoundBag? GetPlayerUsefulCompounds()
    {
        if (stage?.HasPlayer != true)
            return null;

        if (!stage.Player.Has<CompoundStorage>())
            return null;

        return stage.Player.Get<CompoundStorage>().Compounds;
    }

    protected override float? ReadPlayerStrainFraction()
    {
        if (!stage!.Player.Has<StrainAffected>())
        {
            if (!playerMissingStrainAffected)
            {
                GD.PrintErr("Player is missing StrainAffected component");
                playerMissingStrainAffected = true;
            }

            return null;
        }

        return stage.Player.Get<StrainAffected>().CalculateStrainFraction();
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

    protected override void UpdateBarVisibility(Func<Compound, bool> isUseful)
    {
        base.UpdateBarVisibility(isUseful);

        ingestedMatterBar.Visible = GetPlayerUsedIngestionCapacity() > 0;
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

        ingestedMatterBar.UpdateValue(GetPlayerUsedIngestionCapacity(), maxSize);
    }

    protected override ProcessStatistics? GetPlayerProcessStatistics()
    {
        return stage!.Player.Get<BioProcesses>().ProcessStatistics;
    }

    protected override void CalculatePlayerReproductionProgress(Dictionary<Compound, float> gatheredCompounds,
        Dictionary<Compound, float> totalNeededCompounds)
    {
        stage!.Player.Get<OrganelleContainer>().CalculateReproductionProgress(
            ref stage.Player.Get<ReproductionStatus>(), ref stage.Player.Get<SpeciesMember>(),
            stage.Player, stage.Player.Get<CompoundStorage>().Compounds, stage.GameWorld.WorldSettings,
            gatheredCompounds, totalNeededCompounds);
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
            organelles.HasSignalingAgent, engulfing, isDigesting, control.Sprinting,
            stage.GameWorld.WorldSettings.ExperimentalFeatures);

        bindingModeHotkey.Visible = organelles.CanBind(ref species);
        unbindAllHotkey.Visible = organelles.CanUnbind(ref species, player);

        bindingModeHotkey.ButtonPressed = control.State == MicrobeState.Binding;

        if (unbindAllHotkey.ActionNameAsStringName != null)
            unbindAllHotkey.ButtonPressed = Input.IsActionPressed(unbindAllHotkey.ActionNameAsStringName);
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
            compoundControl.SetDescription(GetCompoundDensityCategory(amount) ?? Localization.Translate("N_A"));
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
                label.SetDescription(Localization.Translate("PLAYER"));
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
                item.SetDescription(Localization.Translate("N_TIMES").FormatSafe(hoveredEntity.Value));
        }
    }

    /// <summary>
    ///   Sets button's texture and hint based on its status of fossilisation
    /// </summary>
    private void SetupFossilisationButtonVisuals(FossilisationButton button, bool alreadyFossilised)
    {
        // Display a faded button with a different hint if the species has been fossilised.
        button.AlreadyFossilised = alreadyFossilised;
        button.TooltipText = alreadyFossilised ?
            Localization.Translate("FOSSILISATION_HINT_ALREADY_FOSSILISED") :
            Localization.Translate("FOSSILISATION_HINT");
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
            return stage.Player.Get<MicrobeColony>().CalculateUsedEngulfingCapacity();

        return stage.Player.Get<Engulfer>().UsedEngulfingCapacity;
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

        multicellularButton.Text = Localization.Translate("BECOME_MULTICELLULAR")
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

        macroscopicButton.Text = Localization.Translate("BECOME_MACROSCOPIC")
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

    private void OnBecomeMulticellularCanceled()
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

    private void OnEngulfmentPressed()
    {
        EmitSignal(SignalName.OnToggleEngulfButtonPressed);
    }

    private void OnFireToxinPressed()
    {
        EmitSignal(SignalName.OnFireToxinButtonPressed);
    }

    private void OnBindingModePressed()
    {
        EmitSignal(SignalName.OnToggleBindingButtonPressed);
    }

    private void OnUnbindAllPressed()
    {
        EmitSignal(SignalName.OnUnbindAllButtonPressed);
    }

    private void OnSecreteSlimePressed()
    {
        EmitSignal(SignalName.OnSecreteSlimeButtonPressed);
    }

    private void OnEjectEngulfedPressed()
    {
        EmitSignal(SignalName.OnEjectEngulfedButtonPressed);
    }

    private void OnSprintPressed()
    {
        EmitSignal(SignalName.OnSprintButtonPressed);
    }

    private void OnTranslationsChanged()
    {
        UpdateColonySizeForMulticellular();
        UpdateColonySizeForMacroscopic();
    }
}
