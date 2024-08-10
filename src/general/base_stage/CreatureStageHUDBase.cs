using System;
using System.Collections.Generic;
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
    [Export]
    public NodePath? MouseHoverPanelPath;

    [Export]
    public NodePath AtpLabelPath = null!;

    [Export]
    public NodePath HpLabelPath = null!;

    [Export]
    public NodePath PopulationLabelPath = null!;

    [Export]
    public NodePath PatchOverlayPath = null!;

    [Export]
    public NodePath AtpBarPath = null!;

    [Export]
    public NodePath HealthBarPath = null!;

    [Export]
    public NodePath ProcessPanelPath = null!;

    [Export]
    public NodePath HintTextPath = null!;

    [Export]
    public NodePath HotBarPath = null!;

    [Export]
    public NodePath EngulfHotkeyPath = null!;

    [Export]
    public NodePath SecreteSlimeHotkeyPath = null!;

    // TODO: rename to SignalingAgentsHotkeyPath
    [Export]
    public NodePath SignallingAgentsHotkeyPath = null!;

    [Export]
    public NodePath MicrobeControlRadialPath = null!;

    [Export]
    public NodePath FireToxinHotkeyPath = null!;

    [Export]
    public NodePath EjectEngulfedHotkeyPath = null!;

    [Export]
    public NodePath BottomLeftBarPath = null!;

    [Export]
    public NodePath FossilisationButtonLayerPath = null!;

    [Export]
    public NodePath FossilisationDialogPath = null!;

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

    protected Compound ammonia = null!;
    protected Compound atp = null!;
    protected Compound carbondioxide = null!;
    protected Compound glucose = null!;
    protected Compound hydrogensulfide = null!;
    protected Compound iron = null!;
    protected Compound nitrogen = null!;
    protected Compound oxygen = null!;
    protected Compound oxytoxy = null!;
    protected Compound mucilage = null!;
    protected Compound phosphates = null!;
    protected Compound sunlight = null!;
    protected Compound temperature = null!;

#pragma warning disable CA2213
    protected MouseHoverPanel mouseHoverPanel = null!;

    [Export]
    protected EnvironmentPanel environmentPanel = null!;

    [Export]
    protected CompoundPanels compoundsPanel = null!;

    [Export]
    protected EditorEntryButton editorButton = null!;

    [Export]
    protected ActionButton mucocystHotkey = null!;

    protected ActionButton engulfHotkey = null!;
    protected ActionButton secreteSlimeHotkey = null!;
    protected ActionButton ejectEngulfedHotkey = null!;
    protected ActionButton signalingAgentsHotkey = null!;

    protected CompoundProgressBar oxygenBar = null!;
    protected CompoundProgressBar co2Bar = null!;
    protected CompoundProgressBar nitrogenBar = null!;
    protected CompoundProgressBar temperatureBar = null!;
    protected CompoundProgressBar sunlightBar = null!;

    // TODO: implement changing pressure conditions
    protected CompoundProgressBar pressureBar = null!;

    // TODO: switch to dynamically creating the following bars to allow better extensibility in terms of compound types
    protected CompoundProgressBar glucoseBar = null!;
    protected CompoundProgressBar ammoniaBar = null!;
    protected CompoundProgressBar phosphateBar = null!;
    protected CompoundProgressBar hydrogenSulfideBar = null!;
    protected CompoundProgressBar ironBar = null!;
    protected CompoundProgressBar oxytoxyBar = null!;
    protected CompoundProgressBar mucilageBar = null!;

    protected TextureProgressBar atpBar = null!;
    protected TextureProgressBar healthBar = null!;
    protected Label atpLabel = null!;
    protected Label hpLabel = null!;
    protected Label populationLabel = null!;
    protected PatchNameOverlay patchNameOverlay = null!;
    protected Label hintText = null!;
    protected RadialPopup packControlRadial = null!;

    protected HUDBottomBar bottomLeftBar = null!;

    protected Control winExtinctBoxHolder = null!;

    protected Control fossilisationButtonLayer = null!;
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

    // This block of controls is split from the reset as some controls are protected and these are private
#pragma warning disable CA2213
    [Export]
    private ActionButton sprintHotkey = null!;

    [Export]
    private ProgressBar strainBar = null!;

    [Export]
    private Control damageScreenEffect = null!;

    private HBoxContainer hotBar = null!;
    private ActionButton fireToxinHotkey = null!;

    private CustomWindow? extinctionBox;
    private PatchExtinctionBox? patchExtinctionBox;
    private ProcessPanel processPanel = null!;

    private ShaderMaterial damageShaderMaterial = null!;
#pragma warning restore CA2213

    private StringName fadeParameterName = new("fade");

    [Export]
    private StyleBoxFlat? strainBarRedFill;

    // Used for save load to apply these properties
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

        mouseHoverPanel = GetNode<MouseHoverPanel>(MouseHoverPanelPath);

        atpBar = GetNode<TextureProgressBar>(AtpBarPath);
        healthBar = GetNode<TextureProgressBar>(HealthBarPath);

        atpLabel = GetNode<Label>(AtpLabelPath);
        hpLabel = GetNode<Label>(HpLabelPath);
        populationLabel = GetNode<Label>(PopulationLabelPath);
        patchNameOverlay = GetNode<PatchNameOverlay>(PatchOverlayPath);
        hintText = GetNode<Label>(HintTextPath);
        hotBar = GetNode<HBoxContainer>(HotBarPath);

        packControlRadial = GetNode<RadialPopup>(MicrobeControlRadialPath);

        bottomLeftBar = GetNode<HUDBottomBar>(BottomLeftBarPath);

        engulfHotkey = GetNode<ActionButton>(EngulfHotkeyPath);
        secreteSlimeHotkey = GetNode<ActionButton>(SecreteSlimeHotkeyPath);
        fireToxinHotkey = GetNode<ActionButton>(FireToxinHotkeyPath);
        ejectEngulfedHotkey = GetNode<ActionButton>(EjectEngulfedHotkeyPath);
        signalingAgentsHotkey = GetNode<ActionButton>(SignallingAgentsHotkeyPath);

        processPanel = GetNode<ProcessPanel>(ProcessPanelPath);

        OnAbilitiesHotBarDisplayChanged(Settings.Instance.DisplayAbilitiesHotBar);
        Settings.Instance.DisplayAbilitiesHotBar.OnChanged += OnAbilitiesHotBarDisplayChanged;

        SetEditorButtonFlashEffect(Settings.Instance.GUILightEffectsEnabled);
        Settings.Instance.GUILightEffectsEnabled.OnChanged += SetEditorButtonFlashEffect;

        damageShaderMaterial = (ShaderMaterial)damageScreenEffect.Material;

        ammonia = SimulationParameters.Instance.GetCompound("ammonia");
        atp = SimulationParameters.Instance.GetCompound("atp");
        carbondioxide = SimulationParameters.Instance.GetCompound("carbondioxide");
        glucose = SimulationParameters.Instance.GetCompound("glucose");
        hydrogensulfide = SimulationParameters.Instance.GetCompound("hydrogensulfide");
        iron = SimulationParameters.Instance.GetCompound("iron");
        nitrogen = SimulationParameters.Instance.GetCompound("nitrogen");
        oxygen = SimulationParameters.Instance.GetCompound("oxygen");
        oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        mucilage = SimulationParameters.Instance.GetCompound("mucilage");
        phosphates = SimulationParameters.Instance.GetCompound("phosphates");
        sunlight = SimulationParameters.Instance.GetCompound("sunlight");
        temperature = SimulationParameters.Instance.GetCompound("temperature");

        // Setup bars. In the future it'd be nice to set up bars as needed for the player for allowing easily adding
        // new compound types
        var barScene = GD.Load<PackedScene>("res://src/microbe_stage/gui/CompoundProgressBar.tscn");

        // Environment bars
        oxygenBar = CompoundProgressBar.CreatePercentageDisplay(barScene, oxygen, 0, true);
        co2Bar = CompoundProgressBar.CreatePercentageDisplay(barScene, carbondioxide, 0, true);
        nitrogenBar = CompoundProgressBar.CreatePercentageDisplay(barScene, nitrogen, 0, true);

        // Is it a good idea to show the chemical formulas like this (with subscripts)?

        // Need to use separate strings here so that the localization system doesn't see these
        var oxygenNotTranslated = "O\u2082";
        var co2NotTranslated = "CO\u2082";
        var nitrogenNotTranslated = "N\u2082";

        oxygenBar.DisplayedName = new LocalizedString(oxygenNotTranslated);
        co2Bar.DisplayedName = new LocalizedString(co2NotTranslated);
        nitrogenBar.DisplayedName = new LocalizedString(nitrogenNotTranslated);

        temperatureBar = CompoundProgressBar.CreateSimpleWithUnit(barScene, temperature, 0,
            temperature.Unit ?? throw new Exception("Temperature unit not set"));
        temperatureBar.DisplayedName = new LocalizedString("TEMPERATURE_SHORT");

        sunlightBar = CompoundProgressBar.CreatePercentageDisplay(barScene, sunlight, 0, true);
        sunlightBar.DisplayedName = new LocalizedString("LIGHT");

        // TODO: set value from patch
        pressureBar = CompoundProgressBar.CreateSimpleWithUnit(barScene,
            GD.Load<Texture2D>("res://assets/textures/gui/bevel/Pressure.png"), new LocalizedString("PRESSURE_SHORT"),
            200, "kPa");

        environmentPanel.AddPrimaryBar(oxygenBar);
        environmentPanel.AddPrimaryBar(co2Bar);
        environmentPanel.AddPrimaryBar(nitrogenBar);
        environmentPanel.AddPrimaryBar(temperatureBar);
        environmentPanel.AddPrimaryBar(sunlightBar);
        environmentPanel.AddPrimaryBar(pressureBar);

        // Compound bars
        glucoseBar = CompoundProgressBar.Create(barScene, glucose, 0, 1);
        ammoniaBar = CompoundProgressBar.Create(barScene, ammonia, 0, 1);
        phosphateBar = CompoundProgressBar.Create(barScene, phosphates, 0, 1);
        hydrogenSulfideBar = CompoundProgressBar.Create(barScene, hydrogensulfide, 0, 1);
        ironBar = CompoundProgressBar.Create(barScene, iron, 0, 1);

        compoundsPanel.AddPrimaryBar(glucoseBar);
        compoundBars.Add((glucose, glucoseBar));

        compoundsPanel.AddPrimaryBar(ammoniaBar);
        compoundBars.Add((ammonia, ammoniaBar));

        compoundsPanel.AddPrimaryBar(phosphateBar);
        compoundBars.Add((phosphates, phosphateBar));

        compoundsPanel.AddPrimaryBar(hydrogenSulfideBar);
        compoundBars.Add((hydrogensulfide, hydrogenSulfideBar));

        compoundsPanel.AddPrimaryBar(ironBar);
        compoundBars.Add((iron, ironBar));

        // Agent bars
        oxytoxyBar = CompoundProgressBar.Create(barScene, oxytoxy, 0, 1);
        mucilageBar = CompoundProgressBar.Create(barScene, mucilage, 0, 1);

        compoundsPanel.AddAgentBar(oxytoxyBar);
        compoundBars.Add((oxytoxy, oxytoxyBar));

        compoundsPanel.AddAgentBar(mucilageBar);
        compoundBars.Add((mucilage, mucilageBar));

        // Fossilization setup
        fossilisationButtonLayer = GetNode<Control>(FossilisationButtonLayerPath);
        fossilisationDialog = GetNode<FossilisationDialog>(FossilisationDialogPath);

        // Make sure fossilization layer update won't run if it isn't open
        fossilisationButtonLayer.Visible = false;

        // TODO: move these to be gotten as a method in SimulationParameters (similarly to `GetCloudCompounds()`)
        allAgents.Add(oxytoxy);
        allAgents.Add(mucilage);
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
    }

    public void SendEditorButtonToTutorial(TutorialState tutorialState)
    {
        tutorialState.MicrobePressEditorButton.PressEditorButtonControl = editorButton;
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

    public override void OnEnterStageTransition(bool longerDuration, bool returningFromEditor)
    {
        if (stage == null)
            throw new InvalidOperationException("Stage not setup for HUD");

        if (stage.IsLoadedFromSave && !returningFromEditor)
        {
            // TODO: make it so that the below sequence can be added anyway to not have to have this special logic here
            stage.OnFinishTransitioning();
            return;
        }

        AddFadeIn(stage, longerDuration);
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
        patchNameOverlay.ShowName(localizedPatchName);
    }

    public void ShowExtinctionBox()
    {
        if (extinctionBox != null)
            return;

        winExtinctBoxHolder.Show();

        var box = ExtinctionBoxScene.Instantiate<ExtinctionBox>();

        // update our own population before looking for nearby species
        stage!.GameWorld.PlayerSpecies.Population = 0;
        Species? continueAs = null;

        if (stage!.GameWorld.WorldSettings.SwitchSpeciesOnExtinction)
        {
            box.SpeciesSwitchType = stage.GameWorld.PlayerSpecies.GetType();
            continueAs = GetPotentialSpeciesToContinueAs();

            if (continueAs == null)
                GD.Print("No species to continue as found");
        }
        else
        {
            box.SpeciesSwitchType = null;
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
        oxygenBar.SetValueAsPercentageFromFraction(biome.CurrentCompoundAmounts[oxygen].Ambient);
        co2Bar.SetValueAsPercentageFromFraction(biome.CurrentCompoundAmounts[carbondioxide].Ambient);
        nitrogenBar.SetValueAsPercentageFromFraction(biome.CurrentCompoundAmounts[nitrogen].Ambient);

        sunlightBar.SetValueAsPercentageFromFraction(biome.CurrentCompoundAmounts[sunlight].Ambient);

        temperatureBar.CurrentValue = biome.CurrentCompoundAmounts[temperature].Ambient;

        // TODO: pressure?
        // pressureBar.CurrentValue = ?
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

    protected virtual void UpdateHealth(float delta)
    {
        var hp = 0.0f;

        // Update to the player's current HP, unless the player does not exist
        if (stage!.HasPlayer)
            ReadPlayerHitpoints(out hp, out maxHP);

        // TODO: skip updating the label if value has not changed to save on memory allocations
        healthBar.MaxValue = maxHP;
        GUICommon.SmoothlyUpdateBar(healthBar, hp, delta);
        var hpText = StringUtils.FormatNumber(Mathf.Round(hp)) + " / " + StringUtils.FormatNumber(maxHP);
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
        if (!stage!.GameWorld.WorldSettings.ExperimentalFeatures)
        {
            strainBar.Visible = false;
            return;
        }

        var readStrainFraction = ReadPlayerStrainFraction();

        // Skip the rest of the method if player does not have strain
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
                strainBar.AddThemeStyleboxOverride("fill", strainBarRedFill);
            }
            else
            {
                strainBar.RemoveThemeStyleboxOverride("fill");
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
        var population = stage.GameWorld.Map.CurrentPatch!.GetSpeciesGameplayPopulation(playerSpecies);

        if (population <= 0 && stage.HasPlayer)
            population = 1;

        // TODO: skip updating the label if value has not changed to save on memory allocations
        populationLabel.Text = population.FormatNumber();
    }

    /// <summary>
    ///   Updates the GUI bars to show only needed compounds
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
            fractionOfAmmonia = gatheredCompounds[ammonia] / totalNeededCompounds[ammonia];
        }
        catch (Exception e)
        {
            GD.PrintErr("can't get reproduction ammonia progress: ", e);
        }

        try
        {
            fractionOfPhosphates = gatheredCompounds[phosphates] / totalNeededCompounds[phosphates];
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

            atpAmount = compounds.GetCompoundAmount(atp);
            maxATP = compounds.GetCapacityForCompound(atp);
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
        sprintHotkey.Visible = showSprint && stage!.GameWorld.WorldSettings.ExperimentalFeatures;

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
            if (MouseHoverPanelPath != null)
            {
                MouseHoverPanelPath.Dispose();
                AtpLabelPath.Dispose();
                HpLabelPath.Dispose();
                PopulationLabelPath.Dispose();
                PatchOverlayPath.Dispose();
                AtpBarPath.Dispose();
                HealthBarPath.Dispose();
                ProcessPanelPath.Dispose();
                HintTextPath.Dispose();
                HotBarPath.Dispose();
                EngulfHotkeyPath.Dispose();
                EjectEngulfedHotkeyPath.Dispose();
                SecreteSlimeHotkeyPath.Dispose();
                SignallingAgentsHotkeyPath.Dispose();
                MicrobeControlRadialPath.Dispose();
                FireToxinHotkeyPath.Dispose();
                BottomLeftBarPath.Dispose();
                FossilisationButtonLayerPath.Dispose();
                FossilisationDialogPath.Dispose();
            }

            fadeParameterName.Dispose();

            strainBarRedFill?.Dispose();
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
