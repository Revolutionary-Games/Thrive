﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Newtonsoft.Json;
using Array = Godot.Collections.Array;
using Object = Godot.Object;

/// <summary>
///   Base HUD class for stages where the player moves a creature around
/// </summary>
/// <typeparam name="TStage">The type of the stage this HUD is for</typeparam>
[JsonObject(MemberSerialization.OptIn)]
public abstract class StageHUDBase<TStage> : Control, IStageHUD
    where TStage : Object, IStage
{
    [Export]
    public NodePath? CompoundsGroupAnimationPlayerPath;

    [Export]
    public NodePath EnvironmentGroupAnimationPlayerPath = null!;

    [Export]
    public NodePath PanelsTweenPath = null!;

    [Export]
    public NodePath LeftPanelsPath = null!;

    [Export]
    public NodePath MouseHoverPanelPath = null!;

    [Export]
    public NodePath HoveredCellsContainerPath = null!;

    [Export]
    public NodePath MenuPath = null!;

    [Export]
    public NodePath AtpLabelPath = null!;

    [Export]
    public NodePath HpLabelPath = null!;

    [Export]
    public NodePath PopulationLabelPath = null!;

    [Export]
    public NodePath PatchOverlayPath = null!;

    [Export]
    public NodePath EditorButtonPath = null!;

    [Export]
    public NodePath EnvironmentPanelPath = null!;

    [Export]
    public NodePath OxygenBarPath = null!;

    [Export]
    public NodePath Co2BarPath = null!;

    [Export]
    public NodePath NitrogenBarPath = null!;

    [Export]
    public NodePath TemperaturePath = null!;

    [Export]
    public NodePath SunlightPath = null!;

    [Export]
    public NodePath PressurePath = null!;

    [Export]
    public NodePath EnvironmentPanelBarContainerPath = null!;

    [Export]
    public NodePath CompoundsPanelPath = null!;

    [Export]
    public NodePath GlucoseBarPath = null!;

    [Export]
    public NodePath AmmoniaBarPath = null!;

    [Export]
    public NodePath PhosphateBarPath = null!;

    [Export]
    public NodePath HydrogenSulfideBarPath = null!;

    [Export]
    public NodePath IronBarPath = null!;

    [Export]
    public NodePath EnvironmentPanelExpandButtonPath = null!;

    [Export]
    public NodePath EnvironmentPanelCompressButtonPath = null!;

    [Export]
    public NodePath CompoundsPanelExpandButtonPath = null!;

    [Export]
    public NodePath CompoundsPanelCompressButtonPath = null!;

    [Export]
    public NodePath CompoundsPanelBarContainerPath = null!;

    [Export]
    public NodePath AtpBarPath = null!;

    [Export]
    public NodePath HealthBarPath = null!;

    [Export]
    public NodePath AmmoniaReproductionBarPath = null!;

    [Export]
    public NodePath PhosphateReproductionBarPath = null!;

    [Export]
    public NodePath EditorButtonFlashPath = null!;

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

    [Export]
    public NodePath SignallingAgentsHotkeyPath = null!;

    [Export]
    public NodePath MicrobeControlRadialPath = null!;

    [Export]
    public NodePath PausePromptPath = null!;

    [Export]
    public NodePath PauseInfoPath = null!;

    [Export]
    public NodePath HoveredCompoundsContainerPath = null!;

    [Export]
    public NodePath HoverPanelSeparatorPath = null!;

    [Export]
    public NodePath AgentsPanelPath = null!;

    [Export]
    public NodePath OxytoxyBarPath = null!;

    [Export]
    public NodePath MucilageBarPath = null!;

    [Export]
    public NodePath AgentsPanelBarContainerPath = null!;

    [Export]
    public NodePath FireToxinHotkeyPath = null!;

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
    public Texture AmmoniaBW = null!;

    [Export]
    public Texture PhosphatesBW = null!;

    [Export]
    public Texture AmmoniaInv = null!;

    [Export]
    public Texture PhosphatesInv = null!;

    [Export]
    public PackedScene ExtinctionBoxScene = null!;

    [Export]
    public PackedScene PatchExtinctionBoxScene = null!;
#pragma warning restore CA2213

    // Inspections and cleanup disagree here
    // ReSharper disable RedundantNameQualifier
    protected readonly System.Collections.Generic.Dictionary<Species, int> hoveredSpeciesCounts = new();

    protected readonly System.Collections.Generic.Dictionary<Compound, HoveredCompoundControl> hoveredCompoundControls =
        new();

    // ReSharper restore RedundantNameQualifier

    protected readonly Color defaultHealthBarColour = new(0.96f, 0.27f, 0.48f);

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
    protected AnimationPlayer compoundsGroupAnimationPlayer = null!;
    protected AnimationPlayer environmentGroupAnimationPlayer = null!;
    protected MarginContainer mouseHoverPanel = null!;
    protected Panel environmentPanel = null!;
    protected GridContainer? environmentPanelBarContainer;
    protected ActionButton engulfHotkey = null!;
    protected ActionButton secreteSlimeHotkey = null!;
    protected ActionButton signallingAgentsHotkey = null!;

    protected ProgressBar oxygenBar = null!;
    protected ProgressBar co2Bar = null!;
    protected ProgressBar nitrogenBar = null!;
    protected ProgressBar temperatureBar = null!;
    protected ProgressBar sunlightLabel = null!;

    // TODO: implement changing pressure conditions
    // ReSharper disable once NotAccessedField.Local
    protected ProgressBar pressure = null!;

    protected GridContainer? compoundsPanelBarContainer;
    protected ProgressBar glucoseBar = null!;
    protected ProgressBar ammoniaBar = null!;
    protected ProgressBar phosphateBar = null!;
    protected ProgressBar hydrogenSulfideBar = null!;
    protected ProgressBar ironBar = null!;
    protected Button environmentPanelExpandButton = null!;
    protected Button environmentPanelCompressButton = null!;
    protected Button compoundsPanelExpandButton = null!;
    protected Button compoundsPanelCompressButton = null!;
    protected TextureProgress atpBar = null!;
    protected TextureProgress healthBar = null!;
    protected TextureProgress ammoniaReproductionBar = null!;
    protected TextureProgress phosphateReproductionBar = null!;
    protected Light2D editorButtonFlash = null!;
    protected PauseMenu menu = null!;
    protected Label atpLabel = null!;
    protected Label hpLabel = null!;
    protected Label populationLabel = null!;
    protected PatchNameOverlay patchNameOverlay = null!;
    protected TextureButton editorButton = null!;
    protected Tween panelsTween = null!;
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

    /// <summary>
    ///   Show mouse coordinates data in the mouse
    ///   hover box, useful during develop.
    /// </summary>
#pragma warning disable 649 // ignored until we get some GUI or something to change this
    protected bool showMouseCoordinates;
#pragma warning restore 649

    // This block of controls is split from the reset as some controls are protected and these are private
#pragma warning disable CA2213
    private Control pausePrompt = null!;
    private CustomRichTextLabel pauseInfo = null!;

    private VBoxContainer hoveredCompoundsContainer = null!;
    private HSeparator hoveredCellsSeparator = null!;
    private VBoxContainer hoveredCellsContainer = null!;
    private Panel compoundsPanel = null!;
    private HBoxContainer hotBar = null!;
    private ActionButton fireToxinHotkey = null!;
    private Control agentsPanel = null!;
    private ProgressBar oxytoxyBar = null!;
    private ProgressBar mucilageBar = null!;
    private CustomDialog? extinctionBox;
    private PatchExtinctionBox? patchExtinctionBox;
    private ProcessPanel processPanel = null!;
#pragma warning restore CA2213

    private Array? compoundBars;

    /// <summary>
    ///   For toggling paused with the pause button.
    /// </summary>
    private bool paused;

    private bool environmentCompressed;
    private bool compoundCompressed;

    // The values of the two following variables are the opposite of the expected values.
    // I.e. their values are true when their respective panels are collapsed.
    private bool compoundsPanelActive;

    private bool environmentPanelActive;

    /// <summary>
    ///   Used by UpdateHoverInfo to run HOVER_PANEL_UPDATE_INTERVAL
    /// </summary>
    private float hoverInfoTimeElapsed;

    [JsonProperty]
    private float healthBarFlashDuration;

    [JsonProperty]
    private Color healthBarFlashColour = new(0, 0, 0, 0);

    // These signals need to be copied to inheriting classes for Godot editor to pick them up
    [Signal]
    public delegate void OnOpenMenu();

    [Signal]
    public delegate void OnOpenMenuToHelp();

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
        get => environmentCompressed;
        set
        {
            if (environmentCompressed == value)
                return;

            environmentCompressed = value;
            UpdateEnvironmentPanelState();
        }
    }

    [JsonProperty]
    public bool CompoundsPanelCompressed
    {
        get => compoundCompressed;
        set
        {
            if (compoundCompressed == value)
                return;

            compoundCompressed = value;
            UpdateCompoundsPanelState();
        }
    }

    [JsonIgnore]
    public bool Paused => paused;

    /// <summary>
    ///   If this returns non-null value the help text / prompt for unpausing is shown when paused
    /// </summary>
    protected abstract string? UnPauseHelpText { get; }

    public override void _Ready()
    {
        base._Ready();

        compoundBars = GetTree().GetNodesInGroup("CompoundBar");

        winExtinctBoxHolder = GetNode<Control>("../WinExtinctBoxHolder");

        panelsTween = GetNode<Tween>(PanelsTweenPath);
        mouseHoverPanel = GetNode<MarginContainer>(MouseHoverPanelPath);
        agentsPanel = GetNode<Control>(AgentsPanelPath);

        environmentPanel = GetNode<Panel>(EnvironmentPanelPath);
        environmentPanelBarContainer = GetNode<GridContainer>(EnvironmentPanelBarContainerPath);
        oxygenBar = GetNode<ProgressBar>(OxygenBarPath);
        co2Bar = GetNode<ProgressBar>(Co2BarPath);
        nitrogenBar = GetNode<ProgressBar>(NitrogenBarPath);
        temperatureBar = GetNode<ProgressBar>(TemperaturePath);
        sunlightLabel = GetNode<ProgressBar>(SunlightPath);
        pressure = GetNode<ProgressBar>(PressurePath);

        compoundsPanel = GetNode<Panel>(CompoundsPanelPath);
        compoundsPanelBarContainer = GetNode<GridContainer>(CompoundsPanelBarContainerPath);
        glucoseBar = GetNode<ProgressBar>(GlucoseBarPath);
        ammoniaBar = GetNode<ProgressBar>(AmmoniaBarPath);
        phosphateBar = GetNode<ProgressBar>(PhosphateBarPath);
        hydrogenSulfideBar = GetNode<ProgressBar>(HydrogenSulfideBarPath);
        ironBar = GetNode<ProgressBar>(IronBarPath);

        environmentPanelExpandButton = GetNode<Button>(EnvironmentPanelExpandButtonPath);
        environmentPanelCompressButton = GetNode<Button>(EnvironmentPanelCompressButtonPath);
        compoundsPanelExpandButton = GetNode<Button>(CompoundsPanelExpandButtonPath);
        compoundsPanelCompressButton = GetNode<Button>(CompoundsPanelCompressButtonPath);

        oxytoxyBar = GetNode<ProgressBar>(OxytoxyBarPath);
        mucilageBar = GetNode<ProgressBar>(MucilageBarPath);
        atpBar = GetNode<TextureProgress>(AtpBarPath);
        healthBar = GetNode<TextureProgress>(HealthBarPath);
        ammoniaReproductionBar = GetNode<TextureProgress>(AmmoniaReproductionBarPath);
        phosphateReproductionBar = GetNode<TextureProgress>(PhosphateReproductionBarPath);
        editorButtonFlash = GetNode<Light2D>(EditorButtonFlashPath);

        atpLabel = GetNode<Label>(AtpLabelPath);
        hpLabel = GetNode<Label>(HpLabelPath);
        menu = GetNode<PauseMenu>(MenuPath);
        compoundsGroupAnimationPlayer = GetNode<AnimationPlayer>(CompoundsGroupAnimationPlayerPath);
        environmentGroupAnimationPlayer = GetNode<AnimationPlayer>(EnvironmentGroupAnimationPlayerPath);
        hoveredCompoundsContainer = GetNode<VBoxContainer>(HoveredCompoundsContainerPath);
        hoveredCellsSeparator = GetNode<HSeparator>(HoverPanelSeparatorPath);
        hoveredCellsContainer = GetNode<VBoxContainer>(HoveredCellsContainerPath);
        populationLabel = GetNode<Label>(PopulationLabelPath);
        patchNameOverlay = GetNode<PatchNameOverlay>(PatchOverlayPath);
        editorButton = GetNode<TextureButton>(EditorButtonPath);
        hintText = GetNode<Label>(HintTextPath);
        hotBar = GetNode<HBoxContainer>(HotBarPath);

        pausePrompt = GetNode<Control>(PausePromptPath);
        pauseInfo = GetNode<CustomRichTextLabel>(PauseInfoPath);

        packControlRadial = GetNode<RadialPopup>(MicrobeControlRadialPath);

        bottomLeftBar = GetNode<HUDBottomBar>(BottomLeftBarPath);

        engulfHotkey = GetNode<ActionButton>(EngulfHotkeyPath);
        secreteSlimeHotkey = GetNode<ActionButton>(SecreteSlimeHotkeyPath);
        fireToxinHotkey = GetNode<ActionButton>(FireToxinHotkeyPath);
        signallingAgentsHotkey = GetNode<ActionButton>(SignallingAgentsHotkeyPath);

        processPanel = GetNode<ProcessPanel>(ProcessPanelPath);

        OnAbilitiesHotBarDisplayChanged(Settings.Instance.DisplayAbilitiesHotBar);
        Settings.Instance.DisplayAbilitiesHotBar.OnChanged += OnAbilitiesHotBarDisplayChanged;

        SetEditorButtonFlashEffect(Settings.Instance.GUILightEffectsEnabled);
        Settings.Instance.GUILightEffectsEnabled.OnChanged += SetEditorButtonFlashEffect;

        foreach (var compound in SimulationParameters.Instance.GetCloudCompounds())
        {
            var hoveredCompoundControl = new HoveredCompoundControl(compound);
            hoveredCompoundControls.Add(compound, hoveredCompoundControl);
            hoveredCompoundsContainer.AddChild(hoveredCompoundControl);
        }

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

        fossilisationButtonLayer = GetNode<Control>(FossilisationButtonLayerPath);
        fossilisationDialog = GetNode<FossilisationDialog>(FossilisationDialogPath);

        allAgents.Add(oxytoxy);
        allAgents.Add(mucilage);

        UpdateEnvironmentPanelState();
        UpdateCompoundsPanelState();
        UpdatePausePrompt();
    }

    public void Init(TStage containedInStage)
    {
        stage = containedInStage;
    }

    public void SendEditorButtonToTutorial(TutorialState tutorialState)
    {
        tutorialState.MicrobePressEditorButton.PressEditorButtonControl = editorButton;
    }

    public override void _Process(float delta)
    {
        if (stage == null)
            return;

        if (stage.HasPlayer)
        {
            UpdateNeededBars();
            UpdateCompoundBars(delta);
            UpdateReproductionProgress();
            UpdateAbilitiesHotBar();
        }

        UpdateATP(delta);
        UpdateHealth(delta);
        UpdateHoverInfo(delta);

        UpdatePopulation();
        UpdateProcessPanel();
        UpdatePanelSizing(delta);

        UpdateFossilisationButtons();
    }

    public void PauseButtonPressed(bool buttonState)
    {
        if (menu.Visible)
        {
            bottomLeftBar.Paused = paused;
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        paused = !paused;
        bottomLeftBar.Paused = paused;

        if (paused)
        {
            pausePrompt.Show();
            ShowFossilisationButtons();

            // Pause the game
            PauseManager.Instance.AddPause(nameof(IStageHUD));
        }
        else
        {
            pausePrompt.Hide();
            HideFossilisationButtons();

            // Unpause the game
            PauseManager.Instance.Resume(nameof(IStageHUD));
        }
    }

    /// <summary>
    ///   Enables the editor button.
    /// </summary>
    public void ShowReproductionDialog()
    {
        if (!editorButton.Disabled || stage?.HasPlayer != true)
            return;

        GUICommon.Instance.PlayCustomSound(MicrobePickupOrganelleSound);

        editorButton.Disabled = false;
        editorButton.GetNode<TextureRect>("Highlight").Show();
        editorButton.GetNode<TextureProgress>("ReproductionBar/PhosphateReproductionBar").TintProgress =
            new Color(1, 1, 1, 1);
        editorButton.GetNode<TextureProgress>("ReproductionBar/AmmoniaReproductionBar").TintProgress =
            new Color(1, 1, 1, 1);
        editorButton.GetNode<TextureRect>("ReproductionBar/PhosphateIcon").Texture = PhosphatesBW;
        editorButton.GetNode<TextureRect>("ReproductionBar/AmmoniaIcon").Texture = AmmoniaBW;
        editorButton.GetNode<AnimationPlayer>("AnimationPlayer").Play("EditorButtonFlash");
    }

    /// <summary>
    ///   Disables the editor button.
    /// </summary>
    public void HideReproductionDialog()
    {
        if (!editorButton.Disabled)
            editorButton.Disabled = true;

        editorButton.GetNode<TextureRect>("Highlight").Hide();
        editorButton.GetNode<Control>("ReproductionBar").Show();
        editorButton.GetNode<TextureProgress>("ReproductionBar/PhosphateReproductionBar").TintProgress =
            new Color(0.69f, 0.42f, 1, 1);
        editorButton.GetNode<TextureProgress>("ReproductionBar/AmmoniaReproductionBar").TintProgress =
            new Color(1, 0.62f, 0.12f, 1);
        editorButton.GetNode<TextureRect>("ReproductionBar/PhosphateIcon").Texture = PhosphatesInv;
        editorButton.GetNode<TextureRect>("ReproductionBar/AmmoniaIcon").Texture = AmmoniaInv;
        editorButton.GetNode<AnimationPlayer>("AnimationPlayer").Stop();
    }

    public void OnEnterStageTransition(bool longerDuration, bool returningFromEditor)
    {
        if (stage == null)
            throw new InvalidOperationException("Stage not setup for HUD");

        if (stage.IsLoadedFromSave && !returningFromEditor)
        {
            // TODO: make it so that the below sequence can be added anyway to not have to have this special logic here
            stage.OnFinishTransitioning();
            return;
        }

        // Fade out for that smooth satisfying transition
        stage.TransitionFinished = false;

        TransitionManager.Instance.AddSequence(
            ScreenFade.FadeType.FadeIn, longerDuration ? 1.0f : 0.5f, stage.OnFinishTransitioning);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            foreach (var hoveredCompoundControl in hoveredCompoundControls)
            {
                hoveredCompoundControl.Value.UpdateTranslation();
            }
        }
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

    public void ShowPatchName(string localizedPatchName)
    {
        patchNameOverlay.ShowName(localizedPatchName);
    }

    public void ShowExtinctionBox()
    {
        if (extinctionBox != null)
            return;

        winExtinctBoxHolder.Show();

        extinctionBox = ExtinctionBoxScene.Instance<CustomDialog>();
        winExtinctBoxHolder.AddChild(extinctionBox);
        extinctionBox.Show();
    }

    public void ShowPatchExtinctionBox()
    {
        if (patchExtinctionBox == null)
        {
            patchExtinctionBox = PatchExtinctionBoxScene.Instance<PatchExtinctionBox>();
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
    }

    public void UpdateEnvironmentalBars(BiomeConditions biome)
    {
        var oxygenPercentage = biome.CurrentCompoundAmounts[oxygen].Ambient * 100;
        var co2Percentage = biome.CurrentCompoundAmounts[carbondioxide].Ambient * 100;
        var nitrogenPercentage = biome.CurrentCompoundAmounts[nitrogen].Ambient * 100;
        var sunlightPercentage = Math.Round(biome.CurrentCompoundAmounts[sunlight].Ambient * 100, 0);
        var averageTemperature = biome.CurrentCompoundAmounts[temperature].Ambient;

        var percentageFormat = TranslationServer.Translate("PERCENTAGE_VALUE");
        var unitFormat = TranslationServer.Translate("VALUE_WITH_UNIT");

        oxygenBar.MaxValue = 100;
        oxygenBar.Value = oxygenPercentage;
        oxygenBar.GetNode<Label>("Value").Text = percentageFormat.FormatSafe(oxygenPercentage);

        co2Bar.MaxValue = 100;
        co2Bar.Value = co2Percentage;
        co2Bar.GetNode<Label>("Value").Text = percentageFormat.FormatSafe(co2Percentage);

        nitrogenBar.MaxValue = 100;
        nitrogenBar.Value = nitrogenPercentage;
        nitrogenBar.GetNode<Label>("Value").Text = percentageFormat.FormatSafe(nitrogenPercentage);

        sunlightLabel.GetNode<Label>("Value").Text = percentageFormat.FormatSafe(sunlightPercentage);
        temperatureBar.GetNode<Label>("Value").Text = unitFormat.FormatSafe(averageTemperature, temperature.Unit);

        // TODO: pressure?
    }

    /// <summary>
    ///   Creates and displays a fossilisation button above each on-screen organism.
    /// </summary>
    public abstract void ShowFossilisationButtons();

    /// <summary>
    ///   Destroys all fossilisation buttons on screen.
    /// </summary>
    public void HideFossilisationButtons()
    {
        fossilisationButtonLayer.QueueFreeChildren();
    }

    /// <summary>
    ///   Opens the dialog to a fossilise the species selected with a given fossilisation button.
    /// </summary>
    /// <param name="button">The button attached to the organism to fossilise</param>
    public void ShowFossilisationDialog(FossilisationButton button)
    {
        if (button.AttachedEntity is Microbe microbe)
        {
            fossilisationDialog.SelectedSpecies = microbe.Species;
            fossilisationDialog.PopupCenteredShrink();
        }
        else
        {
            throw new NotImplementedException("Saving non-microbe species is not yet implemented");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (CompoundsGroupAnimationPlayerPath != null)
            {
                CompoundsGroupAnimationPlayerPath.Dispose();
                EnvironmentGroupAnimationPlayerPath.Dispose();
                PanelsTweenPath.Dispose();
                LeftPanelsPath.Dispose();
                MouseHoverPanelPath.Dispose();
                HoveredCellsContainerPath.Dispose();
                MenuPath.Dispose();
                AtpLabelPath.Dispose();
                HpLabelPath.Dispose();
                PopulationLabelPath.Dispose();
                PatchOverlayPath.Dispose();
                EditorButtonPath.Dispose();
                EnvironmentPanelPath.Dispose();
                OxygenBarPath.Dispose();
                Co2BarPath.Dispose();
                NitrogenBarPath.Dispose();
                TemperaturePath.Dispose();
                SunlightPath.Dispose();
                PressurePath.Dispose();
                EnvironmentPanelBarContainerPath.Dispose();
                CompoundsPanelPath.Dispose();
                GlucoseBarPath.Dispose();
                AmmoniaBarPath.Dispose();
                PhosphateBarPath.Dispose();
                HydrogenSulfideBarPath.Dispose();
                IronBarPath.Dispose();
                EnvironmentPanelExpandButtonPath.Dispose();
                EnvironmentPanelCompressButtonPath.Dispose();
                CompoundsPanelExpandButtonPath.Dispose();
                CompoundsPanelCompressButtonPath.Dispose();
                CompoundsPanelBarContainerPath.Dispose();
                AtpBarPath.Dispose();
                HealthBarPath.Dispose();
                AmmoniaReproductionBarPath.Dispose();
                PhosphateReproductionBarPath.Dispose();
                EditorButtonFlashPath.Dispose();
                ProcessPanelPath.Dispose();
                HintTextPath.Dispose();
                HotBarPath.Dispose();
                EngulfHotkeyPath.Dispose();
                SecreteSlimeHotkeyPath.Dispose();
                SignallingAgentsHotkeyPath.Dispose();
                MicrobeControlRadialPath.Dispose();
                PausePromptPath.Dispose();
                PauseInfoPath.Dispose();
                HoveredCompoundsContainerPath.Dispose();
                HoverPanelSeparatorPath.Dispose();
                AgentsPanelPath.Dispose();
                OxytoxyBarPath.Dispose();
                MucilageBarPath.Dispose();
                AgentsPanelBarContainerPath.Dispose();
                FireToxinHotkeyPath.Dispose();
                BottomLeftBarPath.Dispose();
                FossilisationButtonLayerPath.Dispose();
                FossilisationDialogPath.Dispose();
            }

            compoundBars?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Makes sure the game is unpaused (at least by us)
    /// </summary>
    protected void EnsureGameIsUnpausedForEditor()
    {
        if (Paused)
        {
            PauseButtonPressed(!Paused);

            if (PauseManager.Instance.Paused)
                GD.PrintErr("Unpausing the game after editor button press didn't work");
        }
    }

    protected void UpdateEnvironmentPanelState()
    {
        if (environmentPanelBarContainer == null)
            return;

        var bars = environmentPanelBarContainer.GetChildren();

        if (environmentCompressed)
        {
            environmentPanelCompressButton.Pressed = true;
            environmentPanelBarContainer.Columns = 2;
            environmentPanelBarContainer.AddConstantOverride("vseparation", 20);
            environmentPanelBarContainer.AddConstantOverride("hseparation", 17);

            foreach (ProgressBar bar in bars)
            {
                panelsTween?.InterpolateProperty(bar, "rect_min_size:x", 95, 73, 0.3f);
                panelsTween?.Start();

                bar.GetNode<Label>("Label").Hide();
                bar.GetNode<Label>("Value").Align = Label.AlignEnum.Center;
            }
        }

        if (!environmentCompressed)
        {
            environmentPanelExpandButton.Pressed = true;
            environmentPanelBarContainer.Columns = 1;
            environmentPanelBarContainer.AddConstantOverride("vseparation", 4);
            environmentPanelBarContainer.AddConstantOverride("hseparation", 0);

            foreach (ProgressBar bar in bars)
            {
                panelsTween?.InterpolateProperty(bar, "rect_min_size:x", bar.RectMinSize.x, 162, 0.3f);
                panelsTween?.Start();

                bar.GetNode<Label>("Label").Show();
                bar.GetNode<Label>("Value").Align = Label.AlignEnum.Right;
            }
        }
    }

    protected void UpdateCompoundsPanelState()
    {
        if (compoundsPanelBarContainer == null)
            return;

        var bars = compoundsPanelBarContainer.GetChildren();

        if (compoundCompressed)
        {
            compoundsPanelCompressButton.Pressed = true;
            compoundsPanelBarContainer.AddConstantOverride("vseparation", 20);
            compoundsPanelBarContainer.AddConstantOverride("hseparation", 14);

            if (bars.Count < 4)
            {
                compoundsPanelBarContainer.Columns = 2;
            }
            else
            {
                compoundsPanelBarContainer.Columns = 3;
            }

            foreach (ProgressBar bar in bars)
            {
                panelsTween?.InterpolateProperty(bar, "rect_min_size:x", 90, 64, 0.3f);
                panelsTween?.Start();

                bar.GetNode<Label>("Label").Hide();
            }
        }

        if (!compoundCompressed)
        {
            compoundsPanelExpandButton.Pressed = true;
            compoundsPanelBarContainer.Columns = 1;
            compoundsPanelBarContainer.AddConstantOverride("vseparation", 5);
            compoundsPanelBarContainer.AddConstantOverride("hseparation", 0);

            foreach (ProgressBar bar in bars)
            {
                panelsTween?.InterpolateProperty(bar, "rect_min_size:x", bar.RectMinSize.x, 220, 0.3f);
                panelsTween?.Start();

                bar.GetNode<Label>("Label").Show();
            }
        }
    }

    protected virtual void UpdateHealth(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        var hp = 0.0f;

        // Update to the player's current HP, unless the player does not exist
        if (stage!.HasPlayer)
            ReadPlayerHitpoints(out hp, out maxHP);

        healthBar.MaxValue = maxHP;
        GUICommon.SmoothlyUpdateBar(healthBar, hp, delta);
        var hpText = StringUtils.FormatNumber(Mathf.Round(hp)) + " / " + StringUtils.FormatNumber(maxHP);
        hpLabel.Text = hpText;
        hpLabel.HintTooltip = hpText;
    }

    protected abstract void ReadPlayerHitpoints(out float hp, out float maxHP);

    protected void SetEditorButtonFlashEffect(bool enabled)
    {
        editorButtonFlash.Visible = enabled;
    }

    protected void UpdatePopulation()
    {
        var playerSpecies = stage!.GameWorld.PlayerSpecies;
        var population = stage.GameWorld.Map.CurrentPatch!.GetSpeciesGameplayPopulation(playerSpecies);

        if (population <= 0 && stage.HasPlayer)
            population = 1;

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

        UpdateBarVisibility(GetIsUsefulCheck());
    }

    protected abstract CompoundBag? GetPlayerUsefulCompounds();

    protected abstract Func<Compound, bool> GetIsUsefulCheck();

    /// <summary>
    ///   Potentially special handling for a compound bar
    /// </summary>
    /// <param name="bar">The bar to handle</param>
    /// <returns>True if handled, in which case default handling is skipped</returns>
    protected abstract bool SpecialHandleBar(ProgressBar bar);

    protected abstract bool ShouldShowAgentsPanel();

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
                TranslationServer.Translate("CATEGORY_AN_ABUNDANCE"),
            >= Constants.COMPOUND_DENSITY_CATEGORY_QUITE_A_BIT =>
                TranslationServer.Translate("CATEGORY_QUITE_A_BIT"),
            >= Constants.COMPOUND_DENSITY_CATEGORY_FAIR_AMOUNT =>
                TranslationServer.Translate("CATEGORY_A_FAIR_AMOUNT"),
            >= Constants.COMPOUND_DENSITY_CATEGORY_SOME =>
                TranslationServer.Translate("CATEGORY_SOME"),
            >= Constants.COMPOUND_DENSITY_CATEGORY_LITTLE =>
                TranslationServer.Translate("CATEGORY_LITTLE"),
            >= Constants.COMPOUND_DENSITY_CATEGORY_VERY_LITTLE =>
                TranslationServer.Translate("CATEGORY_VERY_LITTLE"),
            _ => null,
        };
    }

    /// <summary>
    ///   Updates the compound bars with the correct values.
    /// </summary>
    protected virtual void UpdateCompoundBars(float delta)
    {
        var compounds = GetPlayerStorage();

        glucoseBar.MaxValue = compounds.GetCapacityForCompound(glucose);
        GUICommon.SmoothlyUpdateBar(glucoseBar, compounds.GetCompoundAmount(glucose), delta);
        glucoseBar.GetNode<Label>("Value").Text = glucoseBar.Value + " / " + glucoseBar.MaxValue;

        ammoniaBar.MaxValue = compounds.GetCapacityForCompound(ammonia);
        GUICommon.SmoothlyUpdateBar(ammoniaBar, compounds.GetCompoundAmount(ammonia), delta);
        ammoniaBar.GetNode<Label>("Value").Text = ammoniaBar.Value + " / " + ammoniaBar.MaxValue;

        phosphateBar.MaxValue = compounds.GetCapacityForCompound(phosphates);
        GUICommon.SmoothlyUpdateBar(phosphateBar, compounds.GetCompoundAmount(phosphates), delta);
        phosphateBar.GetNode<Label>("Value").Text = phosphateBar.Value + " / " + phosphateBar.MaxValue;

        hydrogenSulfideBar.MaxValue = compounds.GetCapacityForCompound(hydrogensulfide);
        GUICommon.SmoothlyUpdateBar(hydrogenSulfideBar, compounds.GetCompoundAmount(hydrogensulfide), delta);
        hydrogenSulfideBar.GetNode<Label>("Value").Text = hydrogenSulfideBar.Value + " / " +
            hydrogenSulfideBar.MaxValue;

        ironBar.MaxValue = compounds.GetCapacityForCompound(iron);
        GUICommon.SmoothlyUpdateBar(ironBar, compounds.GetCompoundAmount(iron), delta);
        ironBar.GetNode<Label>("Value").Text = ironBar.Value + " / " + ironBar.MaxValue;

        oxytoxyBar.MaxValue = compounds.GetCapacityForCompound(oxytoxy);
        GUICommon.SmoothlyUpdateBar(oxytoxyBar, compounds.GetCompoundAmount(oxytoxy), delta);
        oxytoxyBar.GetNode<Label>("Value").Text = oxytoxyBar.Value + " / " + oxytoxyBar.MaxValue;

        mucilageBar.MaxValue = compounds.GetCapacityForCompound(mucilage);
        GUICommon.SmoothlyUpdateBar(mucilageBar, compounds.GetCompoundAmount(mucilage), delta);
        mucilageBar.GetNode<Label>("Value").Text = mucilageBar.Value + " / " + mucilageBar.MaxValue;
    }

    protected void UpdateReproductionProgress()
    {
        CalculatePlayerReproductionProgress(
            out Dictionary<Compound, float> gatheredCompounds,
            out Dictionary<Compound, float> totalNeededCompounds);

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

        ammoniaReproductionBar.Value = fractionOfAmmonia * ammoniaReproductionBar.MaxValue;
        phosphateReproductionBar.Value = fractionOfPhosphates * phosphateReproductionBar.MaxValue;

        CheckAmmoniaProgressHighlight(fractionOfAmmonia);
        CheckPhosphateProgressHighlight(fractionOfPhosphates);
    }

    protected abstract void CalculatePlayerReproductionProgress(out Dictionary<Compound, float> gatheredCompounds,
        out Dictionary<Compound, float> totalNeededCompounds);

    protected void UpdateATP(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

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
        var atpText = atpAmount.ToString("F1", CultureInfo.CurrentCulture) + " / "
            + maxATP.ToString("F1", CultureInfo.CurrentCulture);
        atpLabel.Text = atpText;
        atpLabel.HintTooltip = atpText;
    }

    protected abstract ICompoundStorage GetPlayerStorage();

    protected void UpdateProcessPanel()
    {
        if (!processPanel.Visible)
            return;

        processPanel.ShownData = stage is { HasPlayer: true } ? GetPlayerProcessStatistics() : null;
    }

    protected abstract ProcessStatistics? GetPlayerProcessStatistics();

    protected void UpdatePanelSizing(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        var environmentPanelVBoxContainer = environmentPanel.GetNode<VBoxContainer>("VBoxContainer");
        var compoundsPanelVBoxContainer = compoundsPanel.GetNode<VBoxContainer>("VBoxContainer");
        var agentsPanelVBoxContainer = agentsPanel.GetNode<VBoxContainer>("VBoxContainer");

        environmentPanelVBoxContainer.RectSize = new Vector2(environmentPanelVBoxContainer.RectMinSize.x, 0);
        compoundsPanelVBoxContainer.RectSize = new Vector2(compoundsPanelVBoxContainer.RectMinSize.x, 0);
        agentsPanelVBoxContainer.RectSize = new Vector2(agentsPanelVBoxContainer.RectMinSize.x, 0);

        // Multiply interpolation value with delta time to make it not be affected by framerate
        var environmentPanelSize = environmentPanel.RectMinSize.LinearInterpolate(
            new Vector2(environmentPanel.RectMinSize.x, environmentPanelVBoxContainer.RectSize.y), 5 * delta);

        var compoundsPanelSize = compoundsPanel.RectMinSize.LinearInterpolate(
            new Vector2(compoundsPanel.RectMinSize.x, compoundsPanelVBoxContainer.RectSize.y), 5 * delta);

        var agentsPanelSize = agentsPanel.RectMinSize.LinearInterpolate(
            new Vector2(agentsPanel.RectMinSize.x, agentsPanelVBoxContainer.RectSize.y), 5 * delta);

        environmentPanel.RectMinSize = environmentPanelSize;
        compoundsPanel.RectMinSize = compoundsPanelSize;
        agentsPanel.RectMinSize = agentsPanelSize;
    }

    /// <summary>
    ///   Updates the mouse hover indicator / player look at box with stuff.
    /// </summary>
    protected virtual void UpdateHoverInfo(float delta)
    {
        hoverInfoTimeElapsed += delta;

        if (hoverInfoTimeElapsed < Constants.HOVER_PANEL_UPDATE_INTERVAL)
            return;

        hoverInfoTimeElapsed = 0;

        // Refresh cells list
        hoveredCellsContainer.FreeChildren();

        var container = mouseHoverPanel.GetNode("PanelContainer/MarginContainer/VBoxContainer");

        // var mousePosLabel = container.GetNode<Label>("MousePos");
        var nothingHere = container.GetNode<MarginContainer>("NothingHere");

        if (showMouseCoordinates)
        {
            throw new NotImplementedException();

            // mousePosLabel.Text = GetMouseHoverCoordinateText() + "\n";
        }

        var hoveredCompounds = GetHoveredCompounds();

        // Show hovered compound information in GUI
        bool anyCompoundVisible = false;
        foreach (var compound in hoveredCompoundControls)
        {
            var compoundControl = compound.Value;
            hoveredCompounds.TryGetValue(compound.Key, out float amount);

            // It is not useful to show trace amounts of a compound, so those are skipped
            if (amount < Constants.COMPOUND_DENSITY_CATEGORY_VERY_LITTLE)
            {
                compoundControl.Visible = false;
                continue;
            }

            compoundControl.Category = GetCompoundDensityCategory(amount);
            compoundControl.CategoryColor = GetCompoundDensityCategoryColor(amount);
            compoundControl.Visible = true;
            anyCompoundVisible = true;
        }

        hoveredCompoundsContainer.GetParent<VBoxContainer>().Visible = anyCompoundVisible;

        // Show the species name and count of hovered cells
        hoveredSpeciesCounts.Clear();
        foreach (var (isPlayer, species) in GetHoveredSpecies())
        {
            if (isPlayer)
            {
                AddHoveredCellLabel(species.FormattedName +
                    " (" + TranslationServer.Translate("PLAYER_CELL") + ")");
                continue;
            }

            hoveredSpeciesCounts.TryGetValue(species, out int count);
            hoveredSpeciesCounts[species] = count + 1;
        }

        foreach (var hoveredSpeciesCount in hoveredSpeciesCounts)
        {
            if (hoveredSpeciesCount.Value > 1)
            {
                AddHoveredCellLabel(
                    TranslationServer.Translate("SPECIES_N_TIMES").FormatSafe(hoveredSpeciesCount.Key.FormattedName,
                        hoveredSpeciesCount.Value));
            }
            else
            {
                AddHoveredCellLabel(hoveredSpeciesCount.Key.FormattedName);
            }
        }

        hoveredCellsSeparator.Visible = hoveredCellsContainer.GetChildCount() > 0 &&
            anyCompoundVisible;

        hoveredCellsContainer.GetParent<VBoxContainer>().Visible = hoveredCellsContainer.GetChildCount() > 0;

        nothingHere.Visible = hoveredCellsContainer.GetChildCount() == 0 && !anyCompoundVisible;
    }

    protected abstract IEnumerable<(bool Player, Species Species)> GetHoveredSpecies();
    protected abstract IReadOnlyDictionary<Compound, float> GetHoveredCompounds();

    protected abstract string GetMouseHoverCoordinateText();

    protected void AddHoveredCellLabel(string cellInfo)
    {
        hoveredCellsContainer.AddChild(new Label
        {
            Valign = Label.VAlign.Center,
            Text = cellInfo,
        });
    }

    protected abstract void UpdateAbilitiesHotBar();

    protected void UpdateBaseAbilitiesBar(bool showEngulf, bool showToxin, bool showSlime,
        bool showingSignaling, bool engulfOn)
    {
        engulfHotkey.Visible = showEngulf;
        fireToxinHotkey.Visible = showToxin;
        secreteSlimeHotkey.Visible = showSlime;
        signallingAgentsHotkey.Visible = showingSignaling;

        engulfHotkey.Pressed = engulfOn;
        fireToxinHotkey.Pressed = Input.IsActionPressed(fireToxinHotkey.ActionName);
        secreteSlimeHotkey.Pressed = Input.IsActionPressed(secreteSlimeHotkey.ActionName);
        signallingAgentsHotkey.Pressed = Input.IsActionPressed(signallingAgentsHotkey.ActionName);
    }

    protected void OpenMenu()
    {
        EmitSignal(nameof(OnOpenMenu));
    }

    protected void OpenHelp()
    {
        EmitSignal(nameof(OnOpenMenuToHelp));
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

    /// <summary>
    ///   Called when the player died out in a patch and selected a new one
    /// </summary>
    private void MoveToNewPatchAfterExtinctInCurrent(Patch patch)
    {
        winExtinctBoxHolder.Hide();
        stage!.MoveToPatch(patch);
    }

    /// <summary>
    ///  Updates the different bars and panels that should be displayed to the screen
    /// </summary>
    private void UpdateBarVisibility(Func<Compound, bool> isUseful)
    {
        if (ShouldShowAgentsPanel())
        {
            agentsPanel.Show();
        }
        else
        {
            agentsPanel.Hide();
        }

        if (compoundBars == null)
            throw new InvalidOperationException("This HUD is not initialized");

        foreach (ProgressBar bar in compoundBars)
        {
            if (SpecialHandleBar(bar))
                continue;

            var compound = SimulationParameters.Instance.GetCompound(bar.Name);

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

    private void CheckAmmoniaProgressHighlight(float fractionOfAmmonia)
    {
        if (fractionOfAmmonia < 1.0f)
            return;

        ammoniaReproductionBar.TintProgress = new Color(1, 1, 1, 1);
        editorButton.GetNode<TextureRect>("ReproductionBar/AmmoniaIcon").Texture = AmmoniaBW;
    }

    private void CheckPhosphateProgressHighlight(float fractionOfPhosphates)
    {
        if (fractionOfPhosphates < 1.0f)
            return;

        phosphateReproductionBar.TintProgress = new Color(1, 1, 1, 1);
        editorButton.GetNode<TextureRect>("ReproductionBar/PhosphateIcon").Texture = PhosphatesBW;
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

    private void EnvironmentButtonPressed(bool wantedState)
    {
        if (environmentPanelActive == !wantedState)
            return;

        if (!environmentPanelActive)
        {
            environmentPanelActive = true;
            environmentGroupAnimationPlayer.Play("HideEnvironmentPanel");
        }
        else
        {
            environmentPanelActive = false;
            environmentGroupAnimationPlayer.Play("ShowEnvironmentPanel");
        }
    }

    private void CompoundButtonPressed(bool wantedState)
    {
        if (compoundsPanelActive == !wantedState)
            return;

        if (!compoundsPanelActive)
        {
            compoundsPanelActive = true;
            compoundsGroupAnimationPlayer.Play("HideCompoundsPanels");
        }
        else
        {
            compoundsPanelActive = false;
            compoundsGroupAnimationPlayer.Play("ShowCompoundsPanels");
        }
    }

    private void OnEnvironmentPanelSizeButtonPressed(string mode)
    {
        if (mode == "compress")
        {
            EnvironmentPanelCompressed = true;
        }
        else if (mode == "expand")
        {
            EnvironmentPanelCompressed = false;
        }
    }

    private void OnCompoundsPanelSizeButtonPressed(string mode)
    {
        if (mode == "compress")
        {
            CompoundsPanelCompressed = true;
        }
        else if (mode == "expand")
        {
            CompoundsPanelCompressed = false;
        }
    }

    private void HelpButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        menu.OpenToHelp();
    }

    private void StatisticsButtonPressed()
    {
        menu.OpenToStatistics();
    }

    private void OnEditorButtonMouseEnter()
    {
        if (editorButton.Disabled)
            return;

        editorButton.GetNode<TextureRect>("Highlight").Hide();
        editorButton.GetNode<AnimationPlayer>("AnimationPlayer").Stop();
    }

    private void OnEditorButtonMouseExit()
    {
        if (editorButton.Disabled)
            return;

        editorButton.GetNode<TextureRect>("Highlight").Show();
        editorButton.GetNode<AnimationPlayer>("AnimationPlayer").Play();
    }

    private void UpdatePausePrompt()
    {
        var text = UnPauseHelpText;

        if (text != null)
        {
            pauseInfo.ExtendedBbcode = text;
        }
        else
        {
            pauseInfo.Visible = false;
        }
    }

    private void UpdateFossilisationButtons()
    {
        if (!fossilisationButtonLayer.Visible)
            return;

        foreach (FossilisationButton button in fossilisationButtonLayer.GetChildren())
        {
            button.UpdatePosition();
        }
    }
}
