using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Components;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base HUD class for stages where the player moves a creature around
/// </summary>
/// <typeparam name="TStage">The type of the stage this HUD is for</typeparam>
[JsonObject(MemberSerialization.OptIn)]
[GodotAbstract]
public partial class CreatureStageHUDBase<TStage> : HUDWithPausing, ICreatureStageHUD
    where TStage : GodotObject, ICreatureStage
{
    // TODO: rename to SignalingAgentsHotkeyPath
#pragma warning disable CA2213
    [Export]
    public PackedScene FossilisationButtonScene = null!;

    [Export]
    public AudioStream MicrobePickupOrganelleSound = null!;

    [Export]
    public PackedScene ExtinctionBoxScene = null!;

    [Export]
    public PackedScene PatchExtinctionBoxScene = null!;
#pragma warning restore CA2213

    protected readonly Color defaultHealthBarColour = new(0.96f, 0.27f, 0.48f);

    protected readonly Color ingestedMatterBarColour = new(0.88f, 0.49f, 0.49f);

    protected readonly List<Compound> allAgents = new();

#pragma warning disable CA2213
    [Export]
    protected MouseHoverPanel mouseHoverPanel = null!;

    [Export]
    protected EnvironmentPanel environmentPanel = null!;

    [Export]
    protected CompoundPanels compoundsPanel = null!;

    [Export]
    protected EditorEntryButton editorButton = null!;

    [Export]
    protected ActionButton mucocystHotkey = null!;

    [Export]
    protected ActionButton engulfHotkey = null!;
    [Export]
    protected ActionButton secreteSlimeHotkey = null!;
    [Export]
    protected ActionButton ejectEngulfedHotkey = null!;
    [Export]
    protected ActionButton signalingAgentsHotkey = null!;

    protected CompoundProgressBar oxygenBar = null!;
    protected CompoundProgressBar co2Bar = null!;
    protected CompoundProgressBar nitrogenBar = null!;
    protected CompoundProgressBar temperatureBar = null!;
    protected CompoundProgressBar sunlightBar = null!;
    protected CompoundProgressBar pressureBar = null!;

    protected CompoundProgressBar radiationBar = null!;

    // TODO: switch to dynamically creating the following bars to allow better extensibility in terms of compound types
    protected CompoundProgressBar glucoseBar = null!;
    protected CompoundProgressBar ammoniaBar = null!;
    protected CompoundProgressBar phosphateBar = null!;
    protected CompoundProgressBar hydrogenSulfideBar = null!;
    protected CompoundProgressBar ironBar = null!;
    protected CompoundProgressBar oxytoxyBar = null!;
    protected CompoundProgressBar mucilageBar = null!;

    [Export]
    protected TextureProgressBar atpBar = null!;
    [Export]
    protected TextureProgressBar healthBar = null!;
    [Export]
    protected Label atpLabel = null!;
    [Export]
    protected Label hpLabel = null!;
    [Export]
    protected Label populationLabel = null!;
    [Export]
    protected PatchNameOverlay patchName = null!;
    [Export]
    protected Label hintText = null!;
    [Export]
    protected RadialPopup packControlRadial = null!;

    [Export]
    protected HUDBottomBar bottomLeftBar = null!;

    protected Control winExtinctBoxHolder = null!;

    [Export]
    protected Control fossilisationButtonLayer = null!;
    [Export]
    protected FossilisationDialog fossilisationDialog = null!;
#pragma warning restore CA2213

    // Store these statefully for after player death
    protected float maxHP = 1.0f;
    protected float maxATP = 1.0f;

    /// <summary>
    ///   Access to the stage to retrieve information for display as well as call some player initiated actions.
    /// </summary>
    protected TStage? stage;

    private readonly List<(Compound Compound, CompoundProgressBar Bar)> compoundBars = new();

    private readonly Dictionary<Compound, float> gatheredCompounds = new();
    private readonly Dictionary<Compound, float> totalNeededCompounds = new();

    private readonly StringName barFillName = new("fill");

    // This block of controls is split from the reset as some controls are protected and these are private
#pragma warning disable CA2213
    [Export]
    private ActionButton sprintHotkey = null!;

    [Export]
    private ProgressBar strainBar = null!;

    [Export]
    private Control damageScreenEffect = null!;

    [Export]
    private HBoxContainer hotBar = null!;
    [Export]
    private ActionButton fireToxinHotkey = null!;

    private CustomWindow? extinctionBox;
    private PatchExtinctionBox? patchExtinctionBox;
    [Export]
    private ProcessPanel processPanel = null!;

    private ShaderMaterial damageShaderMaterial = null!;
#pragma warning restore CA2213

    private StringName fadeParameterName = new("fade");

    [Export]
    private StyleBoxFlat? strainBarRedFill;

    // Used for a save-load to apply these properties
    private bool temporaryEnvironmentCompressed;
    private bool temporaryCompoundCompressed;

    /// <summary>
    ///   Used by UpdateHoverInfo to run HOVER_PANEL_UPDATE_INTERVAL
    /// </summary>
    private double hoverInfoTimeElapsed;

    [JsonProperty]
    private float healthBarFlashDuration;

    [JsonProperty]
    private Color healthBarFlashColour = new(0, 0, 0, 0);

    private float lastHealth;
    private float damageEffectCurrentValue;

    private bool strainIsRed;

    protected CreatureStageHUDBase()
    {
    }

    [Signal]
    public delegate void OnOpenMenuEventHandler();

    [Signal]
    public delegate void OnOpenMenuToHelpEventHandler();

    /// <summary>
    ///   Gets and sets the text that appears at the upper HUD.
    /// </summary>
    public string HintText
    {
        get => hintText.Text;
        set => hintText.Text = value;
    }

    [JsonProperty]
    public bool EnvironmentPanelCompressed
    {
        get
        {
            // Save load compatibility
            if (environmentPanel == null!)
                return temporaryEnvironmentCompressed;

            return environmentPanel.PanelCompressed;
        }
        set
        {
            // Save load compatibility
            if (environmentPanel == null!)
            {
                temporaryEnvironmentCompressed = value;
                return;
            }

            environmentPanel.PanelCompressed = value;
        }
    }

    [JsonProperty]
    public bool CompoundsPanelCompressed
    {
        get
        {
            // Save load compatibility
            if (compoundsPanel == null!)
                return temporaryCompoundCompressed;

            return compoundsPanel.PanelCompressed;
        }
        set
        {
            if (compoundsPanel == null!)
            {
                temporaryCompoundCompressed = value;
                return;
            }

            compoundsPanel.PanelCompressed = value;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        winExtinctBoxHolder = GetNode<Control>("../WinExtinctBoxHolder");

        OnAbilitiesHotBarDisplayChanged(Settings.Instance.DisplayAbilitiesHotBar);
        Settings.Instance.DisplayAbilitiesHotBar.OnChanged += OnAbilitiesHotBarDisplayChanged;

        SetEditorButtonFlashEffect(Settings.Instance.GUILightEffectsEnabled);
        Settings.Instance.GUILightEffectsEnabled.OnChanged += SetEditorButtonFlashEffect;

        damageShaderMaterial = (ShaderMaterial)damageScreenEffect.Material;

        // Setup bars. In the future it'd be nice to set up bars as needed for the player for allowing easily adding
        // new compound types
        var barScene = GD.Load<PackedScene>("res://src/microbe_stage/gui/CompoundProgressBar.tscn");

        var simulationParameters = SimulationParameters.Instance;

        // Environment bars
        oxygenBar = CompoundProgressBar.CreatePercentageDisplay(barScene,
            simulationParameters.GetCompoundDefinition(Compound.Oxygen), 0, true);
        co2Bar = CompoundProgressBar.CreatePercentageDisplay(barScene,
            simulationParameters.GetCompoundDefinition(Compound.Carbondioxide), 0, true);
        nitrogenBar = CompoundProgressBar.CreatePercentageDisplay(barScene,
            simulationParameters.GetCompoundDefinition(Compound.Nitrogen), 0, true);

        // Is it a good idea to show the chemical formulas like this (with subscripts)?

        // Need to use separate strings here so that the localization system doesn't see these
        var oxygenNotTranslated = "O\u2082";
        var co2NotTranslated = "CO\u2082";
        var nitrogenNotTranslated = "N\u2082";

        oxygenBar.DisplayedName = new LocalizedString(oxygenNotTranslated);
        co2Bar.DisplayedName = new LocalizedString(co2NotTranslated);
        nitrogenBar.DisplayedName = new LocalizedString(nitrogenNotTranslated);

        temperatureBar = CompoundProgressBar.CreateSimpleWithUnit(barScene,
            simulationParameters.GetCompoundDefinition(Compound.Temperature), 0,
            SimulationParameters.GetCompound(Compound.Temperature).Unit ??
            throw new Exception("Temperature unit not set"));
        temperatureBar.DisplayedName = new LocalizedString("TEMPERATURE_SHORT");

        sunlightBar = CompoundProgressBar.CreatePercentageDisplay(barScene,
            simulationParameters.GetCompoundDefinition(Compound.Sunlight), 0, true);
        sunlightBar.DisplayedName = new LocalizedString("LIGHT");

        pressureBar = CompoundProgressBar.CreateSimpleWithUnit(barScene,
            GD.Load<Texture2D>("res://assets/textures/gui/bevel/Pressure.svg"), new LocalizedString("PRESSURE_SHORT"),
            100000, "Pa");

        radiationBar = CompoundProgressBar.CreateSimpleCompound(barScene,
            simulationParameters.GetCompoundDefinition(Compound.Radiation), 0, "mGy");

        environmentPanel.AddPrimaryBar(oxygenBar);
        environmentPanel.AddPrimaryBar(co2Bar);
        environmentPanel.AddPrimaryBar(nitrogenBar);
        environmentPanel.AddPrimaryBar(temperatureBar);
        environmentPanel.AddPrimaryBar(sunlightBar);

        // TODO: should this be hidden? At least in the microbe stage this doesn't change during gameplay so this just
        // takes up space unnecessarily
        environmentPanel.AddPrimaryBar(pressureBar);

        environmentPanel.AddPrimaryBar(radiationBar);

        // Hidden until it would show something
        radiationBar.Hide();

        // Compound bars
        glucoseBar = CompoundProgressBar.Create(barScene, simulationParameters.GetCompoundDefinition(Compound.Glucose),
            0, 1);
        ammoniaBar = CompoundProgressBar.Create(barScene, simulationParameters.GetCompoundDefinition(Compound.Ammonia),
            0, 1);
        phosphateBar = CompoundProgressBar.Create(barScene,
            simulationParameters.GetCompoundDefinition(Compound.Phosphates), 0, 1);
        hydrogenSulfideBar = CompoundProgressBar.Create(barScene,
            simulationParameters.GetCompoundDefinition(Compound.Hydrogensulfide), 0, 1);
        ironBar = CompoundProgressBar.Create(barScene, simulationParameters.GetCompoundDefinition(Compound.Iron), 0, 1);

        compoundsPanel.AddPrimaryBar(glucoseBar);
        compoundBars.Add((Compound.Glucose, glucoseBar));

        compoundsPanel.AddPrimaryBar(ammoniaBar);
        compoundBars.Add((Compound.Ammonia, ammoniaBar));

        compoundsPanel.AddPrimaryBar(phosphateBar);
        compoundBars.Add((Compound.Phosphates, phosphateBar));

        compoundsPanel.AddPrimaryBar(hydrogenSulfideBar);
        compoundBars.Add((Compound.Hydrogensulfide, hydrogenSulfideBar));

        compoundsPanel.AddPrimaryBar(ironBar);
        compoundBars.Add((Compound.Iron, ironBar));

        // Agent bars
        oxytoxyBar = CompoundProgressBar.Create(barScene, simulationParameters.GetCompoundDefinition(Compound.Oxytoxy),
            0, 1);
        mucilageBar = CompoundProgressBar.Create(barScene,
            simulationParameters.GetCompoundDefinition(Compound.Mucilage), 0, 1);

        compoundsPanel.AddAgentBar(oxytoxyBar);
        compoundBars.Add((Compound.Oxytoxy, oxytoxyBar));

        compoundsPanel.AddAgentBar(mucilageBar);
        compoundBars.Add((Compound.Mucilage, mucilageBar));

        // Fossilization setup
        // Make sure fossilization layer update won't run if it isn't open
        fossilisationButtonLayer.Visible = false;

        // TODO: move these to be gotten as a method in SimulationParameters (similarly to `GetCloudCompounds()`)
        allAgents.Add(Compound.Oxytoxy);
        allAgents.Add(Compound.Mucilage);
    }

    public void Init(TStage containedInStage)
    {
        stage = containedInStage;
    }

    public override void _Process(double delta)
    {
        if (stage == null)
            return;

        var convertedDelta = (float)delta;

        if (stage.HasAlivePlayer)
        {
            UpdateNeededBars();
            UpdateCompoundBars(convertedDelta);
            UpdateReproductionProgress();
            UpdateAbilitiesHotBar();
            UpdateStrain();
        }

        UpdateATP(convertedDelta);
        UpdateHealth(convertedDelta);

        hoverInfoTimeElapsed += delta;
        if (hoverInfoTimeElapsed > Constants.HOVER_PANEL_UPDATE_INTERVAL)
        {
            UpdateHoverInfo((float)hoverInfoTimeElapsed);
            hoverInfoTimeElapsed = 0;
        }

        UpdatePopulation();
        UpdateProcessPanel();

        UpdateFossilisationButtons();

        // This would be kind of hard to make a non-polling approach for updating the button status
        UpdateSpeedModeDisplay();
    }

    public void SendObjectsToTutorials(TutorialState tutorialState)
    {
        tutorialState.MicrobePressEditorButton.PressEditorButtonControl = editorButton;
        tutorialState.OpenProcessPanelTutorial.ProcessPanelButtonControl = bottomLeftBar.ProcessPanelButtonControl;

        tutorialState.GlucoseCollecting.CompoundPanels = compoundsPanel;
        tutorialState.GlucoseCollecting.HUDBottomBar = bottomLeftBar;

        tutorialState.DayNightTutorial.EnvironmentPanel = environmentPanel;
        tutorialState.DayNightTutorial.HUDBottomBar = bottomLeftBar;
    }

    /// <summary>
    ///   Enables the editor button.
    /// </summary>
    public void ShowReproductionDialog()
    {
        if (!editorButton.Disabled || stage?.HasPlayer != true)
            return;

        if (stage.MovingToEditor)
            return;

        GUICommon.Instance.PlayCustomSound(MicrobePickupOrganelleSound);

        editorButton.ShowReproductionDialog();

        HUDMessages.ShowMessage(Localization.Translate("NOTICE_READY_TO_EDIT"), DisplayDuration.Long);
    }

    /// <summary>
    ///   Disables the editor button.
    /// </summary>
    public void HideReproductionDialog()
    {
        editorButton.HideReproductionDialog();
    }

    public void CloseProcessPanel()
    {
        if (processPanel.Visible)
        {
            bottomLeftBar.ProcessesPressed = false;
            processPanel.Hide();
        }
    }

    public override void OnEnterStageLoadingScreen(bool longerDuration, bool returningFromEditor)
    {
        if (stage == null)
            throw new InvalidOperationException("Stage not setup for HUD");

        ShowLoadingScreen(stage);
    }

    public void OnSuicide()
    {
        stage?.OnSuicide();
    }

    public void EditorButtonPressed()
    {
        GD.Print("Move to editor pressed");

        // TODO: find out when this can happen (this happened when a really laggy save was loaded and the editor button
        // was pressed before the stage fade in fully completed)
        if (stage?.HasPlayer != true)
        {
            GD.PrintErr("Trying to press editor button while having no player object");
            return;
        }

        // To prevent being clicked twice
        editorButton.Disabled = true;

        EnsureGameIsUnpausedForEditor();

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.3f, stage.MoveToEditor, false);

        stage.MovingToEditor = true;
    }

    /// <summary>
    ///   Used to safely cancel editor entry if preconditions are no longer met
    /// </summary>
    public void OnCancelEditorEntry()
    {
        GD.Print("Canceled editor entry, fading stage back in");
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeIn, 0.3f);

        // Prevent being stuck in a state where editor can no longer be entered
        // https://github.com/Revolutionary-Games/Thrive/issues/4204
        stage!.MovingToEditor = false;

        // TODO: should the editor button be always unlocked like this
        editorButton.Disabled = false;
    }

    public void ShowPatchName(string localizedPatchName)
    {
        patchName.ShowName(localizedPatchName);
    }

    public void ShowExtinctionBox()
    {
        if (extinctionBox != null)
            return;

        winExtinctBoxHolder.Show();

        var box = ExtinctionBoxScene.Instantiate<ExtinctionBox>();

        Species? continueAs = null;

        if (stage!.GameWorld.WorldSettings.SwitchSpeciesOnExtinction)
        {
            continueAs = GetPotentialSpeciesToContinueAs();

            if (continueAs == null)
                GD.Print("No species to continue as found");
        }

        box.ShowContinueAs = continueAs;
        box.Connect(ExtinctionBox.SignalName.ContinueSelected, new Callable(this, nameof(ContinueAsSpecies)));

        extinctionBox = box;
        winExtinctBoxHolder.AddChild(extinctionBox);
        extinctionBox.Show();
    }

    public void ShowPatchExtinctionBox()
    {
        if (patchExtinctionBox == null)
        {
            patchExtinctionBox = PatchExtinctionBoxScene.Instantiate<PatchExtinctionBox>();
            winExtinctBoxHolder.AddChild(patchExtinctionBox);

            patchExtinctionBox.OnMovedToNewPatch = MoveToNewPatchAfterExtinctInCurrent;
        }

        if (!winExtinctBoxHolder.Visible)
        {
            winExtinctBoxHolder.Show();

            patchExtinctionBox.PlayerSpecies = stage!.GameWorld.PlayerSpecies;
            patchExtinctionBox.Map = stage.GameWorld.Map;

            patchExtinctionBox.Show();
        }
    }

    public void HidePatchExtinctionBox()
    {
        winExtinctBoxHolder.Hide();
        patchExtinctionBox?.Hide();

        // Allow extinction box to show again / stop showing it so that it doesn't mess with patch extinction box if
        // the player continued the game after extinction.
        extinctionBox?.QueueFree();
        extinctionBox = null;
    }

    public void UpdateEnvironmentalBars(BiomeConditions biome)
    {
        oxygenBar.SetValueAsPercentageFromFraction(biome.CurrentCompoundAmounts[Compound.Oxygen].Ambient);
        co2Bar.SetValueAsPercentageFromFraction(biome.CurrentCompoundAmounts[Compound.Carbondioxide].Ambient);
        nitrogenBar.SetValueAsPercentageFromFraction(biome.CurrentCompoundAmounts[Compound.Nitrogen].Ambient);
        pressureBar.CurrentValue = biome.Pressure;

        sunlightBar.SetValueAsPercentageFromFraction(biome.CurrentCompoundAmounts[Compound.Sunlight].Ambient);

        temperatureBar.CurrentValue = ReadTemperature(biome);

        // TODO: pressure?
        // pressureBar.CurrentValue = ?
    }

    public void UpdateRadiationBar(float radiation, float maxRadiation, float warningThreshold)
    {
        radiationBar.UpdateValue(radiation, maxRadiation);
        radiationBar.Visible = radiationBar.CurrentValue > 0;

        if (radiation / maxRadiation >= warningThreshold)
        {
            radiationBar.Flash();
        }
    }

    public override void PauseButtonPressed(bool buttonState)
    {
        base.PauseButtonPressed(buttonState);

        bottomLeftBar.Paused = Paused;

        if (menu.Visible)
            return;

        if (Paused)
        {
            fossilisationButtonLayer.Visible = true;
            ShowFossilisationButtons();
        }
        else
        {
            HideFossilisationButtons();

            stage?.CurrentGame?.TutorialState.SendEvent(TutorialEventType.GameResumedByPlayer, EventArgs.Empty, this);
        }
    }

    /// <summary>
    ///   Opens the dialog to fossilise the species selected with a given fossilisation button.
    /// </summary>
    /// <param name="button">The button attached to the organism to fossilise</param>
    public void ShowFossilisationDialog(FossilisationButton button)
    {
        if (!button.AttachedEntity.IsAlive)
        {
            GD.PrintErr("Tried to show fossilization dialog for a dead entity");
            return;
        }

        if (button.AttachedEntity.Has<MicrobeSpeciesMember>())
        {
            fossilisationDialog.SelectedSpecies = button.AttachedEntity.Get<MicrobeSpeciesMember>().Species;
            fossilisationDialog.PopupCenteredShrink();
        }
        else
        {
            throw new NotImplementedException("Saving non-microbe species is not yet implemented");
        }
    }

    public virtual void ApplySpeedMode(bool fastModeEnabled)
    {
        GD.PrintErr("Fast mode is not implemented for this stage");
    }

    public virtual bool GetCurrentSpeedMode()
    {
        return false;
    }

    /// <summary>
    ///   Hides both the compounds panel and the environment panel for tutorial purposes
    /// </summary>
    public void HideEnvironmentAndCompoundPanels(bool playAnimation)
    {
        if (playAnimation)
        {
            compoundsPanel.ShowPanel = false;
            environmentPanel.ShowPanel = false;
        }
        else
        {
            compoundsPanel.HideWithoutAnimation();
            environmentPanel.HideWithoutAnimation();
        }

        bottomLeftBar.CompoundsPressed = false;
        bottomLeftBar.EnvironmentPressed = false;
    }

    /// <summary>
    ///   Restores the compound panel after it was closed for the tutorial
    /// </summary>
    public void ShowCompoundPanel()
    {
        compoundsPanel.ShowPanel = true;
        bottomLeftBar.CompoundsPressed = true;
    }

    /// <summary>
    ///   Restores the environment panel after it was closed for the tutorial
    /// </summary>
    public void ShowEnvironmentPanel()
    {
        environmentPanel.ShowPanel = true;
        bottomLeftBar.EnvironmentPressed = true;
    }

    /// <summary>
    ///   Creates and displays a fossilisation button above each on-screen organism.
    /// </summary>
    protected virtual void ShowFossilisationButtons()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    /// <summary>
    ///   Updates all fossilisation buttons' status of fossilisation
    /// </summary>
    protected virtual void UpdateFossilisationButtonStates()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    /// <summary>
    ///   Destroys all fossilisation buttons on screen. And the layer won't be usable again until re-enabled (and
    ///   becomes visible again)
    /// </summary>
    protected void HideFossilisationButtons()
    {
        fossilisationButtonLayer.QueueFreeChildren();

        // Stop processing the layer
        fossilisationButtonLayer.Visible = false;
    }

    protected virtual float ReadTemperature(BiomeConditions biome)
    {
        return biome.CurrentCompoundAmounts[Compound.Temperature].Ambient;
    }

    protected virtual void UpdateHealth(float delta)
    {
        var hp = 0.0f;

        // Update to the player's current HP, unless the player does not exist
        if (stage!.HasPlayer)
            ReadPlayerHitpoints(out hp, out maxHP);

        // TODO: skip updating the label if value has not changed to save on memory allocations
        healthBar.MaxValue = maxHP;
        GUICommon.SmoothlyUpdateBar(healthBar, hp, delta);

        // TODO: might need to consider some effects of the rounding if max health is for example 58.8 there could be
        // situations where there's slight confusion possibility as the displayed value is higher than it really is
        // Max HP is rounded now as well so that it matches what the HP can end up displaying as
        var hpText = StringUtils.FormatNumber(MathF.Round(hp)) + " / " + StringUtils.FormatNumber(MathF.Round(maxHP));
        hpLabel.Text = hpText;
        hpLabel.TooltipText = hpText;

        if (Settings.Instance.ScreenDamageEffect.Value)
        {
            // Process damage flash effect
            if (damageEffectCurrentValue > 0)
            {
                damageEffectCurrentValue -= delta * Constants.SCREEN_DAMAGE_FLASH_DECAY_SPEED;

                damageScreenEffect.Visible = true;
                damageShaderMaterial.SetShaderParameter(fadeParameterName, damageEffectCurrentValue);
            }
            else
            {
                damageScreenEffect.Visible = false;
            }

            // Start damage flash if player has taken enough damage
            if (hp < lastHealth && lastHealth - hp > Constants.SCREEN_DAMAGE_FLASH_THRESHOLD)
                damageEffectCurrentValue = 1;
        }
        else if (damageEffectCurrentValue > 0)
        {
            // Disable effect if user turned off the setting while flashing
            damageEffectCurrentValue = 0;
            damageScreenEffect.Visible = false;
        }

        lastHealth = hp;
    }

    protected virtual void ReadPlayerHitpoints(out float hp, out float maxHP)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected void UpdateStrain()
    {
        var readStrainFraction = ReadPlayerStrainFraction();

        // Skip the rest of the method if the player does not have strain
        if (readStrainFraction == null)
            return;

        var strainFraction = readStrainFraction.Value;

        strainBar.Value = strainFraction;

        var strainState = !CanSprint();

        if (strainState != strainIsRed)
        {
            strainIsRed = strainState;

            if (strainIsRed)
            {
                strainBar.AddThemeStyleboxOverride(barFillName, strainBarRedFill);
            }
            else
            {
                strainBar.RemoveThemeStyleboxOverride(barFillName);
            }
        }

        switch (Settings.Instance.StrainBarVisibilityMode.Value)
        {
            case Settings.StrainBarVisibility.Off:
                strainBar.Hide();
                break;
            case Settings.StrainBarVisibility.VisibleWhenCloseToFull:
                if (strainFraction >= 0.8f)
                {
                    strainBar.Show();
                }
                else
                {
                    strainBar.Hide();
                }

                break;
            case Settings.StrainBarVisibility.VisibleWhenOverZero:
                if (strainFraction > 0.0f)
                {
                    strainBar.Show();
                }
                else
                {
                    strainBar.Hide();
                }

                break;
            case Settings.StrainBarVisibility.AlwaysVisible:
                strainBar.Show();
                break;
        }
    }

    /// <summary>
    ///   Gets the current amount of strain affecting the player
    /// </summary>
    /// <returns>
    ///   Null if the player is missing <see cref="StrainAffected"/>,
    ///   else the player's strain fraction
    /// </returns>
    protected virtual float? ReadPlayerStrainFraction()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual bool CanSprint()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected void SetEditorButtonFlashEffect(bool enabled)
    {
        editorButton.SetEditorButtonFlashEffect(enabled);
    }

    protected void UpdatePopulation()
    {
        var playerSpecies = stage!.GameWorld.PlayerSpecies;
        double population = stage.GameWorld.Map.CurrentPatch!.GetSpeciesGameplayPopulation(playerSpecies);

        if (population <= 0 && stage.HasPlayer)
            population = 1;

        // To not confuse the player that might see a 1 as the population but still seeing plenty of their species
        // scale up the displayed numbers
        if (playerSpecies is MicrobeSpecies or MulticellularSpecies)
        {
            // Scale is trillions
            population *= Constants.MICROBE_POPULATION_MULTIPLIER;
        }

        // TODO: skip updating the label if value has not changed to save on memory allocations
        populationLabel.Text = population.FormatNumber();
        populationLabel.TooltipText = population.ToString("N0", CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///   Updates the GUI bars to show only the necessary compounds
    /// </summary>
    protected void UpdateNeededBars()
    {
        if (!stage!.HasPlayer)
            return;

        if (GetPlayerUsefulCompounds()?.HasAnyBeenSetUseful() != true)
            return;

        // TODO: would it be better to calculate useful compounds one at a time rather than allocating a method here?
        // This causes quite a bit of memory allocations
        UpdateBarVisibility(GetIsUsefulCheck());
    }

    protected virtual CompoundBag? GetPlayerUsefulCompounds()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual Func<Compound, bool> GetIsUsefulCheck()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual bool ShouldShowAgentsPanel()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected Color GetCompoundDensityCategoryColor(float amount)
    {
        return amount switch
        {
            >= Constants.COMPOUND_DENSITY_CATEGORY_AN_ABUNDANCE => new Color(0.282f, 0.788f, 0.011f),
            >= Constants.COMPOUND_DENSITY_CATEGORY_QUITE_A_BIT => new Color(0.011f, 0.768f, 0.466f),
            >= Constants.COMPOUND_DENSITY_CATEGORY_FAIR_AMOUNT => new Color(0.011f, 0.768f, 0.717f),
            >= Constants.COMPOUND_DENSITY_CATEGORY_SOME => new Color(0.011f, 0.705f, 0.768f),
            >= Constants.COMPOUND_DENSITY_CATEGORY_LITTLE => new Color(0.011f, 0.552f, 0.768f),
            >= Constants.COMPOUND_DENSITY_CATEGORY_VERY_LITTLE => new Color(0.011f, 0.290f, 0.768f),
            _ => new Color(1.0f, 1.0f, 1.0f),
        };
    }

    protected string? GetCompoundDensityCategory(float amount)
    {
        return amount switch
        {
            >= Constants.COMPOUND_DENSITY_CATEGORY_AN_ABUNDANCE =>
                Localization.Translate("CATEGORY_AN_ABUNDANCE"),
            >= Constants.COMPOUND_DENSITY_CATEGORY_QUITE_A_BIT =>
                Localization.Translate("CATEGORY_QUITE_A_BIT"),
            >= Constants.COMPOUND_DENSITY_CATEGORY_FAIR_AMOUNT =>
                Localization.Translate("CATEGORY_A_FAIR_AMOUNT"),
            >= Constants.COMPOUND_DENSITY_CATEGORY_SOME =>
                Localization.Translate("CATEGORY_SOME"),
            >= Constants.COMPOUND_DENSITY_CATEGORY_LITTLE =>
                Localization.Translate("CATEGORY_LITTLE"),
            >= Constants.COMPOUND_DENSITY_CATEGORY_VERY_LITTLE =>
                Localization.Translate("CATEGORY_VERY_LITTLE"),
            _ => null,
        };
    }

    /// <summary>
    ///   Updates the compound bars with the correct values.
    /// </summary>
    protected virtual void UpdateCompoundBars(float delta)
    {
        var compounds = GetPlayerStorage();

        foreach (var (compound, bar) in compoundBars)
        {
            // Probably can save on performance here by not updating hidden bars and hoping that when bars become
            // visible they will be updated immediately for the player to not notice
            if (!bar.Visible)
                continue;

            bar.UpdateValue(compounds.GetCompoundAmount(compound), compounds.GetCapacityForCompound(compound));
        }
    }

    protected void UpdateReproductionProgress()
    {
        CalculatePlayerReproductionProgress(gatheredCompounds, totalNeededCompounds);

        float fractionOfAmmonia = 0;
        float fractionOfPhosphates = 0;

        try
        {
            fractionOfAmmonia = gatheredCompounds[Compound.Ammonia] / totalNeededCompounds[Compound.Ammonia];
        }
        catch (Exception e)
        {
            GD.PrintErr("can't get reproduction ammonia progress: ", e);
        }

        try
        {
            fractionOfPhosphates = gatheredCompounds[Compound.Phosphates] / totalNeededCompounds[Compound.Phosphates];
        }
        catch (Exception e)
        {
            GD.PrintErr("can't get reproduction phosphates progress: ", e);
        }

        editorButton.UpdateReproductionProgressBars(fractionOfAmmonia, fractionOfPhosphates);
    }

    protected virtual void CalculatePlayerReproductionProgress(Dictionary<Compound, float> gatheredCompounds,
        Dictionary<Compound, float> totalNeededCompounds)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected void UpdateATP(float delta)
    {
        var atpAmount = 0.0f;

        // Update to the player's current ATP, unless the player does not exist
        if (stage!.HasPlayer)
        {
            var compounds = GetPlayerStorage();

            atpAmount = compounds.GetCompoundAmount(Compound.ATP);
            maxATP = compounds.GetCapacityForCompound(Compound.ATP);
        }

        atpBar.MaxValue = maxATP * 10.0f;

        // If the current ATP is close to full, just pretend that it is to keep the bar from flickering
        if (maxATP - atpAmount < Math.Max(maxATP / 20.0f, 0.1f))
        {
            atpAmount = maxATP;
        }

        GUICommon.SmoothlyUpdateBar(atpBar, atpAmount * 10.0f, delta);

        // TODO: skip updating the label if value has not changed to save on memory allocations
        var atpText = StringUtils.SlashSeparatedNumbersFormat(atpAmount, maxATP);
        atpLabel.Text = atpText;
        atpLabel.TooltipText = atpText;
    }

    protected virtual ICompoundStorage GetPlayerStorage()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected void UpdateProcessPanel()
    {
        if (!processPanel.Visible)
            return;

        processPanel.ShownData = stage is { HasAlivePlayer: true } ? GetPlayerProcessStatistics() : null;
    }

    protected virtual ProcessStatistics? GetPlayerProcessStatistics()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    /// <summary>
    ///   Updates the mouse hover indicator / player look at box (inspector panel) with stuff.
    /// </summary>
    protected virtual void UpdateHoverInfo(float delta)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void UpdateAbilitiesHotBar()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected void UpdateBaseAbilitiesBar(bool showEngulf, bool showToxin, bool showSlime,
        bool showingSignaling, bool showMucocyst, bool showSprint, bool engulfOn, bool showEject, bool mucocystOn,
        bool isSprinting)
    {
        engulfHotkey.Visible = showEngulf;
        fireToxinHotkey.Visible = showToxin;
        secreteSlimeHotkey.Visible = showSlime;
        signalingAgentsHotkey.Visible = showingSignaling;
        ejectEngulfedHotkey.Visible = showEject;
        mucocystHotkey.Visible = showMucocyst;
        sprintHotkey.Visible = showSprint;

        sprintHotkey.ButtonPressed = isSprinting;
        engulfHotkey.ButtonPressed = engulfOn;
        mucocystHotkey.ButtonPressed = mucocystOn;

        if (fireToxinHotkey.ActionNameAsStringName != null)
            fireToxinHotkey.ButtonPressed = Input.IsActionPressed(fireToxinHotkey.ActionNameAsStringName);

        if (secreteSlimeHotkey.ActionNameAsStringName != null)
            secreteSlimeHotkey.ButtonPressed = Input.IsActionPressed(secreteSlimeHotkey.ActionNameAsStringName);

        if (signalingAgentsHotkey.ActionNameAsStringName != null)
            signalingAgentsHotkey.ButtonPressed = Input.IsActionPressed(signalingAgentsHotkey.ActionNameAsStringName);

        if (ejectEngulfedHotkey.ActionNameAsStringName != null)
            ejectEngulfedHotkey.ButtonPressed = Input.IsActionPressed(ejectEngulfedHotkey.ActionNameAsStringName);
    }

    /// <summary>
    ///   Updates the different bars and panels that should be displayed to the screen
    /// </summary>
    protected virtual void UpdateBarVisibility(Func<Compound, bool> isUseful)
    {
        if (ShouldShowAgentsPanel())
        {
            compoundsPanel.ShowAgents = true;
        }
        else
        {
            compoundsPanel.ShowAgents = false;
        }

        if (compoundBars == null)
            throw new InvalidOperationException("This HUD is not initialized");

        foreach (var (compound, bar) in compoundBars)
        {
            if (isUseful.Invoke(compound))
            {
                bar.Show();
            }
            else
            {
                bar.Hide();
            }
        }
    }

    protected virtual Species? GetPotentialSpeciesToContinueAs()
    {
        if (stage == null)
        {
            GD.PrintErr("No stage to get available species through");
            return null;
        }

        var currentPlayer = stage.GameWorld.PlayerSpecies;

        // TODO: if we want to allow going back stages, this will need to be adjusted
        var mustBeSameStage = (Species species) => currentPlayer.GetType() == species.GetType();

        return stage.GameWorld.GetClosestRelatedSpecies(currentPlayer, true, mustBeSameStage);
    }

    protected void OpenMenu()
    {
        EmitSignal(SignalName.OnOpenMenu);
    }

    protected void OpenHelp()
    {
        EmitSignal(SignalName.OnOpenMenuToHelp);
    }

    protected void FlashHealthBar(Color colour, float delta)
    {
        healthBarFlashDuration -= delta;

        if (healthBarFlashDuration % 0.6f < 0.3f)
        {
            healthBar.TintProgress = colour;
        }
        else
        {
            // Restore colour
            healthBar.TintProgress = defaultHealthBarColour;
        }

        // Loop flash
        if (healthBarFlashDuration <= 0)
            healthBarFlashDuration = 2.5f;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            {
            }

            fadeParameterName.Dispose();

            strainBarRedFill?.Dispose();

            barFillName.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Called when the player died out in a patch and selected a new one
    /// </summary>
    private void MoveToNewPatchAfterExtinctInCurrent(Patch patch)
    {
        winExtinctBoxHolder.Hide();
        stage!.MoveToPatch(patch);
    }

    private void ProcessPanelButtonPressed()
    {
        if (processPanel.Visible)
        {
            processPanel.Visible = false;
            bottomLeftBar.ProcessesPressed = false;
        }
        else
        {
            processPanel.Show();
            bottomLeftBar.ProcessesPressed = true;

            // Send a tutorial event about this opening
            stage?.CurrentGame?.TutorialState.SendEvent(TutorialEventType.ProcessPanelOpened, EventArgs.Empty, this);
        }
    }

    private void OnProcessPanelClosed()
    {
        bottomLeftBar.ProcessesPressed = false;
    }

    private void OnAbilitiesHotBarDisplayChanged(bool displayed)
    {
        hotBar.Visible = displayed;
    }

    private void CompoundButtonPressed(bool pressed)
    {
        compoundsPanel.ShowPanel = pressed;
    }

    private void EnvironmentButtonPressed(bool pressed)
    {
        environmentPanel.ShowPanel = pressed;
    }

    private void HelpButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        menu.OpenToHelp();
    }

    private void StatisticsButtonPressed()
    {
        ThriveopediaManager.OpenPage("CurrentWorld");
    }

    private void SpeedModeButtonPressed(bool pressed)
    {
        ApplySpeedMode(pressed);
    }

    private void UpdateSpeedModeDisplay()
    {
        bottomLeftBar.SpeedModePressed = GetCurrentSpeedMode();
    }

    private void HeatViewButtonPressed(bool pressed)
    {
        if (stage == null)
        {
            GD.PrintErr("No stage to set view mode");
            return;
        }

        if (pressed)
        {
            stage.SetSpecialViewMode(ViewMode.Heat);
        }
        else
        {
            stage.SetSpecialViewMode(ViewMode.Normal);
        }
    }

    private void UpdateFossilisationButtons()
    {
        if (!fossilisationButtonLayer.Visible)
            return;

        foreach (var button in fossilisationButtonLayer.GetChildren().OfType<FossilisationButton>())
        {
            button.UpdatePosition();
        }
    }

    private void ContinueAsSpecies(uint id)
    {
        var species = stage?.GameWorld.GetSpecies(id);

        if (species == null)
        {
            GD.PrintErr("Couldn't find species to continue as");
            return;
        }

        stage!.ContinueGameAsSpecies(species);
    }
}
