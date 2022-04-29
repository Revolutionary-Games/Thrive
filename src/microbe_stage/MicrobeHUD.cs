using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   Manages the microbe HUD
/// </summary>
public class MicrobeHUD : Control
{
    [Export]
    public NodePath AnimationPlayerPath = null!;

    [Export]
    public NodePath PanelsTweenPath = null!;

    [Export]
    public NodePath LeftPanelsPath = null!;

    [Export]
    public NodePath MouseHoverPanelPath = null!;

    [Export]
    public NodePath HoveredCompoundsContainerPath = null!;

    [Export]
    public NodePath HoverPanelSeparatorPath = null!;

    [Export]
    public NodePath HoveredCellsContainerPath = null!;

    [Export]
    public NodePath MenuPath = null!;

    [Export]
    public NodePath PauseButtonPath = null!;

    [Export]
    public NodePath ResumeButtonPath = null!;

    [Export]
    public NodePath AtpLabelPath = null!;

    [Export]
    public NodePath HpLabelPath = null!;

    [Export]
    public NodePath PopulationLabelPath = null!;

    [Export]
    public NodePath PatchLabelPath = null!;

    [Export]
    public NodePath PatchOverlayAnimatorPath = null!;

    [Export]
    public NodePath EditorButtonPath = null!;

    [Export]
    public NodePath MulticellularButtonPath = null!;

    [Export]
    public NodePath MulticellularConfirmPopupPath = null!;

    [Export]
    public NodePath MacroscopicButtonPath = null!;

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
    public NodePath CompoundsPanelBarContainerPath = null!;

    [Export]
    public NodePath AgentsPanelPath = null!;

    [Export]
    public NodePath OxytoxyBarPath = null!;

    [Export]
    public NodePath AgentsPanelBarContainerPath = null!;

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
    public NodePath ProcessPanelButtonPath = null!;

    [Export]
    public NodePath HintTextPath = null!;

    [Export]
    public PackedScene ExtinctionBoxScene = null!;

    [Export]
    public PackedScene WinBoxScene = null!;

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
    public NodePath HotBarPath = null!;

    [Export]
    public NodePath EngulfHotkeyPath = null!;

    [Export]
    public NodePath FireToxinHotkeyPath = null!;

    [Export]
    public NodePath BindingModeHotkeyPath = null!;

    [Export]
    public NodePath UnbindAllHotkeyPath = null!;

    [Export]
    public NodePath SignallingAgentsHotkeyPath = null!;

    [Export]
    public NodePath MicrobeControlRadialPath = null!;

    // Formatter and code checks disagree here
    // ReSharper disable RedundantNameQualifier
    private readonly System.Collections.Generic.Dictionary<Species, int> hoveredSpeciesCounts = new();

    private readonly System.Collections.Generic.Dictionary<Compound, HoveredCompoundControl> hoveredCompoundControls =
        new();

    // ReSharper restore RedundantNameQualifier

    private Compound ammonia = null!;
    private Compound atp = null!;
    private Compound carbondioxide = null!;
    private Compound glucose = null!;
    private Compound hydrogensulfide = null!;
    private Compound iron = null!;
    private Compound nitrogen = null!;
    private Compound oxygen = null!;
    private Compound oxytoxy = null!;
    private Compound phosphates = null!;
    private Compound sunlight = null!;

    private AnimationPlayer animationPlayer = null!;
    private MarginContainer mouseHoverPanel = null!;
    private VBoxContainer hoveredCompoundsContainer = null!;
    private HSeparator hoveredCellsSeparator = null!;
    private VBoxContainer hoveredCellsContainer = null!;
    private Panel environmentPanel = null!;
    private GridContainer environmentPanelBarContainer = null!;
    private Panel compoundsPanel = null!;
    private HBoxContainer hotBar = null!;
    private ActionButton engulfHotkey = null!;
    private ActionButton fireToxinHotkey = null!;
    private ActionButton bindingModeHotkey = null!;
    private ActionButton unbindAllHotkey = null!;
    private ActionButton signallingAgentsHotkey = null!;

    // Store these statefully for after player death
    private float maxHP = 1.0f;
    private float maxATP = 1.0f;

    private ProgressBar oxygenBar = null!;
    private ProgressBar co2Bar = null!;
    private ProgressBar nitrogenBar = null!;
    private ProgressBar temperature = null!;
    private ProgressBar sunlightLabel = null!;

    // TODO: implement changing pressure conditions
    // ReSharper disable once NotAccessedField.Local
    private ProgressBar pressure = null!;

    private GridContainer compoundsPanelBarContainer = null!;
    private ProgressBar glucoseBar = null!;
    private ProgressBar ammoniaBar = null!;
    private ProgressBar phosphateBar = null!;
    private ProgressBar hydrogenSulfideBar = null!;
    private ProgressBar ironBar = null!;

    private Control agentsPanel = null!;
    private ProgressBar oxytoxyBar = null!;

    private TextureProgress atpBar = null!;
    private TextureProgress healthBar = null!;
    private TextureProgress ammoniaReproductionBar = null!;
    private TextureProgress phosphateReproductionBar = null!;
    private Light2D editorButtonFlash = null!;

    private PauseMenu menu = null!;
    private TextureButton pauseButton = null!;
    private TextureButton resumeButton = null!;
    private Label atpLabel = null!;
    private Label hpLabel = null!;
    private Label populationLabel = null!;
    private Label patchLabel = null!;
    private AnimationPlayer patchOverlayAnimator = null!;
    private TextureButton editorButton = null!;
    private Button multicellularButton = null!;
    private CustomDialog multicellularConfirmPopup = null!;
    private Button macroscopicButton = null!;

    private CustomDialog? extinctionBox;
    private CustomDialog? winBox;
    private Tween panelsTween = null!;
    private Control winExtinctBoxHolder = null!;
    private Label hintText = null!;

    private RadialPopup microbeControlRadial = null!;

    private Array compoundBars = null!;

    private ProcessPanel processPanel = null!;
    private TextureButton processPanelButton = null!;

    /// <summary>
    ///   Access to the stage to retrieve information for display as
    ///   well as call some player initiated actions.
    /// </summary>
    private MicrobeStage? stage;

    /// <summary>
    ///   Show mouse coordinates data in the mouse
    ///   hover box, useful during develop.
    /// </summary>
#pragma warning disable 649 // ignored until we get some GUI or something to change this
    private bool showMouseCoordinates;
#pragma warning restore 649

    /// <summary>
    ///   For toggling paused with the pause button.
    /// </summary>
    private bool paused;

    // Checks
    private bool environmentCompressed;
    private bool compoundCompressed;
    private bool leftPanelsActive;

    /// <summary>
    ///   Used by UpdateHoverInfo to run HOVER_PANEL_UPDATE_INTERVAL
    /// </summary>
    private float hoverInfoTimeElapsed;

    /// <summary>
    ///   If not null the signaling agent radial menu is open for the given microbe, which should be the player
    /// </summary>
    private Microbe? signalingAgentMenuOpenForMicrobe;

    private int? playerColonySize;

    /// <summary>
    ///   Gets and sets the text that appears at the upper HUD.
    /// </summary>
    public string HintText
    {
        get => hintText.Text;
        set => hintText.Text = value;
    }

    public override void _Ready()
    {
        compoundBars = GetTree().GetNodesInGroup("CompoundBar");

        winExtinctBoxHolder = GetNode<Control>("../WinExtinctBoxHolder");

        panelsTween = GetNode<Tween>(PanelsTweenPath);
        mouseHoverPanel = GetNode<MarginContainer>(MouseHoverPanelPath);
        pauseButton = GetNode<TextureButton>(PauseButtonPath);
        resumeButton = GetNode<TextureButton>(ResumeButtonPath);
        agentsPanel = GetNode<Control>(AgentsPanelPath);

        environmentPanel = GetNode<Panel>(EnvironmentPanelPath);
        environmentPanelBarContainer = GetNode<GridContainer>(EnvironmentPanelBarContainerPath);
        oxygenBar = GetNode<ProgressBar>(OxygenBarPath);
        co2Bar = GetNode<ProgressBar>(Co2BarPath);
        nitrogenBar = GetNode<ProgressBar>(NitrogenBarPath);
        temperature = GetNode<ProgressBar>(TemperaturePath);
        sunlightLabel = GetNode<ProgressBar>(SunlightPath);
        pressure = GetNode<ProgressBar>(PressurePath);

        compoundsPanel = GetNode<Panel>(CompoundsPanelPath);
        compoundsPanelBarContainer = GetNode<GridContainer>(CompoundsPanelBarContainerPath);
        glucoseBar = GetNode<ProgressBar>(GlucoseBarPath);
        ammoniaBar = GetNode<ProgressBar>(AmmoniaBarPath);
        phosphateBar = GetNode<ProgressBar>(PhosphateBarPath);
        hydrogenSulfideBar = GetNode<ProgressBar>(HydrogenSulfideBarPath);
        ironBar = GetNode<ProgressBar>(IronBarPath);

        oxytoxyBar = GetNode<ProgressBar>(OxytoxyBarPath);
        atpBar = GetNode<TextureProgress>(AtpBarPath);
        healthBar = GetNode<TextureProgress>(HealthBarPath);
        ammoniaReproductionBar = GetNode<TextureProgress>(AmmoniaReproductionBarPath);
        phosphateReproductionBar = GetNode<TextureProgress>(PhosphateReproductionBarPath);
        editorButtonFlash = GetNode<Light2D>(EditorButtonFlashPath);

        atpLabel = GetNode<Label>(AtpLabelPath);
        hpLabel = GetNode<Label>(HpLabelPath);
        menu = GetNode<PauseMenu>(MenuPath);
        animationPlayer = GetNode<AnimationPlayer>(AnimationPlayerPath);
        hoveredCompoundsContainer = GetNode<VBoxContainer>(HoveredCompoundsContainerPath);
        hoveredCellsSeparator = GetNode<HSeparator>(HoverPanelSeparatorPath);
        hoveredCellsContainer = GetNode<VBoxContainer>(HoveredCellsContainerPath);
        populationLabel = GetNode<Label>(PopulationLabelPath);
        patchLabel = GetNode<Label>(PatchLabelPath);
        patchOverlayAnimator = GetNode<AnimationPlayer>(PatchOverlayAnimatorPath);
        editorButton = GetNode<TextureButton>(EditorButtonPath);
        multicellularButton = GetNode<Button>(MulticellularButtonPath);
        multicellularConfirmPopup = GetNode<CustomDialog>(MulticellularConfirmPopupPath);
        macroscopicButton = GetNode<Button>(MacroscopicButtonPath);
        hintText = GetNode<Label>(HintTextPath);
        hotBar = GetNode<HBoxContainer>(HotBarPath);

        microbeControlRadial = GetNode<RadialPopup>(MicrobeControlRadialPath);

        engulfHotkey = GetNode<ActionButton>(EngulfHotkeyPath);
        fireToxinHotkey = GetNode<ActionButton>(FireToxinHotkeyPath);
        bindingModeHotkey = GetNode<ActionButton>(BindingModeHotkeyPath);
        unbindAllHotkey = GetNode<ActionButton>(UnbindAllHotkeyPath);
        signallingAgentsHotkey = GetNode<ActionButton>(SignallingAgentsHotkeyPath);

        processPanel = GetNode<ProcessPanel>(ProcessPanelPath);
        processPanelButton = GetNode<TextureButton>(ProcessPanelButtonPath);

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
        phosphates = SimulationParameters.Instance.GetCompound("phosphates");
        sunlight = SimulationParameters.Instance.GetCompound("sunlight");

        multicellularButton.Visible = false;
        macroscopicButton.Visible = false;
    }

    public void OnEnterStageTransition(bool longerDuration)
    {
        if (stage == null)
            throw new InvalidOperationException("Stage not setup for HUD");

        // Fade out for that smooth satisfying transition
        stage.TransitionFinished = false;
        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeIn, longerDuration ? 1.0f : 0.3f);
        TransitionManager.Instance.StartTransitions(stage, nameof(MicrobeStage.OnFinishTransitioning));
    }

    public override void _Process(float delta)
    {
        if (stage == null)
            return;

        if (stage.Player != null)
        {
            UpdateNeededBars();
            UpdateCompoundBars();
            UpdateReproductionProgress(stage.Player);
            UpdateAbilitiesHotBar(stage.Player);
            UpdateMulticellularButton(stage.Player);
            UpdateMacroscopicButton(stage.Player);
        }
        else
        {
            multicellularButton.Visible = false;
            macroscopicButton.Visible = false;
        }

        UpdateATP(delta);
        UpdateHealth(delta);
        UpdateHoverInfo(delta);

        UpdatePopulation();
        UpdateProcessPanel();
        UpdatePanelSizing(delta);
    }

    public void Init(MicrobeStage stage)
    {
        this.stage = stage;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            foreach (var hoveredCompoundControl in hoveredCompoundControls)
            {
                hoveredCompoundControl.Value.UpdateTranslation();
            }

            UpdateColonySizeForMulticellular();
            UpdateColonySizeForMacroscopic();
        }
    }

    public void ShowSignalingCommandsMenu(Microbe player)
    {
        if (microbeControlRadial.Visible)
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

        microbeControlRadial.Radial.CenterText = TranslationServer.Translate("SIGNAL_TO_EMIT");

        signalingAgentMenuOpenForMicrobe = player;
        microbeControlRadial.ShowWithItems(choices);
    }

    public MicrobeSignalCommand? SelectSignalCommandIfOpen()
    {
        // Return nothing if not open
        if (!microbeControlRadial.Visible)
            return null;

        var item = microbeControlRadial.Radial.HoveredItem;

        microbeControlRadial.Hide();

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

    public void ResizeEnvironmentPanel(string mode)
    {
        var bars = environmentPanelBarContainer.GetChildren();

        if (mode == "compress" && !environmentCompressed)
        {
            environmentCompressed = true;
            environmentPanelBarContainer.Columns = 2;
            environmentPanelBarContainer.AddConstantOverride("vseparation", 20);
            environmentPanelBarContainer.AddConstantOverride("hseparation", 17);

            foreach (ProgressBar bar in bars)
            {
                panelsTween.InterpolateProperty(bar, "rect_min_size:x", 95, 73, 0.3f);
                panelsTween.Start();

                bar.GetNode<Label>("Label").Hide();
                bar.GetNode<Label>("Value").Align = Label.AlignEnum.Center;
            }
        }

        if (mode == "expand" && environmentCompressed)
        {
            environmentCompressed = false;
            environmentPanelBarContainer.Columns = 1;
            environmentPanelBarContainer.AddConstantOverride("vseparation", 4);
            environmentPanelBarContainer.AddConstantOverride("hseparation", 0);

            foreach (ProgressBar bar in bars)
            {
                panelsTween.InterpolateProperty(bar, "rect_min_size:x", bar.RectMinSize.x, 162, 0.3f);
                panelsTween.Start();

                bar.GetNode<Label>("Label").Show();
                bar.GetNode<Label>("Value").Align = Label.AlignEnum.Right;
            }
        }
    }

    public void ResizeCompoundPanel(string mode)
    {
        var bars = compoundsPanelBarContainer.GetChildren();

        if (mode == "compress" && !compoundCompressed)
        {
            compoundCompressed = true;
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
                panelsTween.InterpolateProperty(bar, "rect_min_size:x", 90, 64, 0.3f);
                panelsTween.Start();

                bar.GetNode<Label>("Label").Hide();
            }
        }

        if (mode == "expand" && compoundCompressed)
        {
            compoundCompressed = false;
            compoundsPanelBarContainer.Columns = 1;
            compoundsPanelBarContainer.AddConstantOverride("vseparation", 5);
            compoundsPanelBarContainer.AddConstantOverride("hseparation", 0);

            foreach (ProgressBar bar in bars)
            {
                panelsTween.InterpolateProperty(bar, "rect_min_size:x", bar.RectMinSize.x, 220, 0.3f);
                panelsTween.Start();

                bar.GetNode<Label>("Label").Show();
            }
        }
    }

    /// <summary>
    ///   Enables the editor button.
    /// </summary>
    public void ShowReproductionDialog()
    {
        if (!editorButton.Disabled || stage?.Player == null)
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

    public void OnSuicide()
    {
        stage?.Player?.Damage(9999.0f, "suicide");
    }

    public void UpdatePatchInfo(string patchName)
    {
        patchLabel.Text = patchName;
    }

    public void PopupPatchInfo()
    {
        patchOverlayAnimator.Play("FadeInOut");
    }

    public void EditorButtonPressed()
    {
        GD.Print("Move to editor pressed");

        // TODO: find out when this can happen (this happened when a really laggy save was loaded and the editor button
        // was pressed before the stage fade in fully completed)
        if (stage?.Player == null)
        {
            GD.PrintErr("Trying to press editor button while having no player object");
            return;
        }

        // To prevent being clicked twice
        editorButton.Disabled = true;

        // Make sure the game is unpaused
        if (GetTree().Paused)
        {
            PauseButtonPressed();
        }

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.3f, false);
        TransitionManager.Instance.StartTransitions(stage, nameof(MicrobeStage.MoveToEditor));

        stage.MovingToEditor = true;

        // TODO: mitigation for https://github.com/Revolutionary-Games/Thrive/issues/3006 remove once solved
        // Start auto-evo if not started already to make sure it doesn't start after we are in the editor
        // scene, this is a potential mitigation for the issue linked above
        if (!Settings.Instance.RunAutoEvoDuringGamePlay)
        {
            GD.Print("Starting auto-evo while fading into the editor as mitigation for issue #3006");
            stage.GameWorld.IsAutoEvoFinished(true);
        }
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

    public void UpdateEnvironmentalBars(BiomeConditions biome)
    {
        var oxygenPercentage = biome.Compounds[oxygen].Dissolved * 100;
        var co2Percentage = biome.Compounds[carbondioxide].Dissolved * 100;
        var nitrogenPercentage = biome.Compounds[nitrogen].Dissolved * 100;
        var sunlightPercentage = biome.Compounds[sunlight].Dissolved * 100;
        var averageTemperature = biome.AverageTemperature;

        var percentageFormat = TranslationServer.Translate("PERCENTAGE_VALUE");

        oxygenBar.MaxValue = 100;
        oxygenBar.Value = oxygenPercentage;
        oxygenBar.GetNode<Label>("Value").Text =
            string.Format(CultureInfo.CurrentCulture, percentageFormat, oxygenPercentage);

        co2Bar.MaxValue = 100;
        co2Bar.Value = co2Percentage;
        co2Bar.GetNode<Label>("Value").Text =
            string.Format(CultureInfo.CurrentCulture, percentageFormat, co2Percentage);

        nitrogenBar.MaxValue = 100;
        nitrogenBar.Value = nitrogenPercentage;
        nitrogenBar.GetNode<Label>("Value").Text =
            string.Format(CultureInfo.CurrentCulture, percentageFormat, nitrogenPercentage);

        sunlightLabel.GetNode<Label>("Value").Text =
            string.Format(CultureInfo.CurrentCulture, percentageFormat, sunlightPercentage);
        temperature.GetNode<Label>("Value").Text = averageTemperature + " °C";

        // TODO: pressure?
    }

    /// <summary>
    ///   Updates the GUI bars to show only needed compounds
    /// </summary>
    private void UpdateNeededBars()
    {
        var compounds = stage!.Player?.Compounds;

        if (compounds?.HasAnyBeenSetUseful() != true)
            return;

        if (compounds.IsSpecificallySetUseful(oxytoxy))
        {
            agentsPanel.Show();
        }
        else
        {
            agentsPanel.Hide();
        }

        foreach (ProgressBar bar in compoundBars)
        {
            var compound = SimulationParameters.Instance.GetCompound(bar.Name);

            if (compounds.IsUseful(compound))
            {
                bar.Show();
            }
            else
            {
                bar.Hide();
            }
        }
    }

    /// <summary>
    ///   Updates the mouse hover indicator box with stuff.
    /// </summary>
    private void UpdateHoverInfo(float delta)
    {
        hoverInfoTimeElapsed += delta;

        if (hoverInfoTimeElapsed < Constants.HOVER_PANEL_UPDATE_INTERVAL)
            return;

        hoverInfoTimeElapsed = 0;

        // Refresh cells list
        hoveredCellsContainer.FreeChildren();

        var container = mouseHoverPanel.GetNode("PanelContainer/MarginContainer/VBoxContainer");
        var mousePosLabel = container.GetNode<Label>("MousePos");
        var nothingHere = container.GetNode<MarginContainer>("NothingHere");

        if (showMouseCoordinates)
        {
            mousePosLabel.Text = string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("STUFF_AT"),
                stage!.Camera.CursorWorldPos.x, stage.Camera.CursorWorldPos.z) + "\n";
        }

        // Show hovered compound information in GUI
        bool anyCompoundVisible = false;
        foreach (var compound in hoveredCompoundControls)
        {
            var compoundControl = compound.Value;
            stage!.HoverInfo.HoveredCompounds.TryGetValue(compound.Key, out float amount);

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
        foreach (var microbe in stage!.HoverInfo.HoveredMicrobes)
        {
            if (microbe.IsPlayerMicrobe)
            {
                AddHoveredCellLabel(microbe.Species.FormattedName +
                    " (" + TranslationServer.Translate("PLAYER_CELL") + ")");
                continue;
            }

            if (!hoveredSpeciesCounts.TryGetValue(microbe.Species, out int count))
            {
                count = 0;
            }

            hoveredSpeciesCounts[microbe.Species] = count + 1;
        }

        foreach (var hoveredSpeciesCount in hoveredSpeciesCounts)
        {
            if (hoveredSpeciesCount.Value > 1)
            {
                AddHoveredCellLabel(
                    string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("SPECIES_N_TIMES"),
                        hoveredSpeciesCount.Key.FormattedName, hoveredSpeciesCount.Value));
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

    private void AddHoveredCellLabel(string cellInfo)
    {
        hoveredCellsContainer.AddChild(new Label
        {
            Valign = Label.VAlign.Center,
            Text = cellInfo,
        });
    }

    private Color GetCompoundDensityCategoryColor(float amount)
    {
        return amount switch
        {
            >= Constants.COMPOUND_DENSITY_CATEGORY_AN_ABUNDANCE => new Color(0.282f, 0.788f, 0.011f),
            >= Constants.COMPOUND_DENSITY_CATEGORY_QUITE_A_BIT => new Color(0.011f, 0.768f, 0.466f),
            >= Constants.COMPOUND_DENSITY_CATEGORY_FAIR_AMOUNT => new Color(0.011f, 0.768f, 0.717f),
            >= Constants.COMPOUND_DENSITY_CATEGORY_SOME => new Color(0.011f, 0.705f, 0.768f),
            >= Constants.COMPOUND_DENSITY_CATEGORY_LITTLE => new Color(0.011f, 0.552f, 0.768f),
            >= Constants.COMPOUND_DENSITY_CATEGORY_VERY_LITTLE => new Color(0.011f, 0.290f, 0.768f),
            _ => new Color(1f, 1f, 1f),
        };
    }

    private string? GetCompoundDensityCategory(float amount)
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
    private void UpdateCompoundBars()
    {
        var compounds = GetPlayerColonyOrPlayerStorage();

        glucoseBar.MaxValue = compounds.GetCapacityForCompound(glucose);
        glucoseBar.Value = compounds.GetCompoundAmount(glucose);
        glucoseBar.GetNode<Label>("Value").Text = glucoseBar.Value + " / " + glucoseBar.MaxValue;

        ammoniaBar.MaxValue = compounds.GetCapacityForCompound(ammonia);
        ammoniaBar.Value = compounds.GetCompoundAmount(ammonia);
        ammoniaBar.GetNode<Label>("Value").Text = ammoniaBar.Value + " / " + ammoniaBar.MaxValue;

        phosphateBar.MaxValue = compounds.GetCapacityForCompound(phosphates);
        phosphateBar.Value = compounds.GetCompoundAmount(phosphates);
        phosphateBar.GetNode<Label>("Value").Text = phosphateBar.Value + " / " + phosphateBar.MaxValue;

        hydrogenSulfideBar.MaxValue = compounds.GetCapacityForCompound(hydrogensulfide);
        hydrogenSulfideBar.Value = compounds.GetCompoundAmount(hydrogensulfide);
        hydrogenSulfideBar.GetNode<Label>("Value").Text = hydrogenSulfideBar.Value + " / " +
            hydrogenSulfideBar.MaxValue;

        ironBar.MaxValue = compounds.GetCapacityForCompound(iron);
        ironBar.Value = compounds.GetCompoundAmount(iron);
        ironBar.GetNode<Label>("Value").Text = ironBar.Value + " / " + ironBar.MaxValue;

        oxytoxyBar.MaxValue = compounds.GetCapacityForCompound(oxytoxy);
        oxytoxyBar.Value = compounds.GetCompoundAmount(oxytoxy);
        oxytoxyBar.GetNode<Label>("Value").Text = oxytoxyBar.Value + " / " + oxytoxyBar.MaxValue;
    }

    private void UpdateReproductionProgress(Microbe player)
    {
        // Get player reproduction progress
        player.CalculateReproductionProgress(
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

    private void UpdateATP(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        var atpAmount = 0.0f;

        // Update to the player's current ATP, unless the player does not exist
        if (stage!.Player != null)
        {
            var compounds = GetPlayerColonyOrPlayerStorage();

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
        atpLabel.Text = atpAmount.ToString("F1", CultureInfo.CurrentCulture) + " / "
            + maxATP.ToString("F1", CultureInfo.CurrentCulture);
    }

    private ICompoundStorage GetPlayerColonyOrPlayerStorage()
    {
        return stage!.Player!.Colony?.ColonyCompounds ?? (ICompoundStorage)stage.Player.Compounds;
    }

    private void UpdateHealth(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        var hp = 0.0f;

        // Update to the player's current HP, unless the player does not exist
        if (stage!.Player != null)
        {
            hp = stage.Player.Hitpoints;
            maxHP = stage.Player.MaxHitpoints;
        }

        healthBar.MaxValue = maxHP;
        GUICommon.SmoothlyUpdateBar(healthBar, hp, delta);
        hpLabel.Text = StringUtils.FormatNumber(Mathf.Round(hp)) + " / " + StringUtils.FormatNumber(maxHP);
    }

    private void SetEditorButtonFlashEffect(bool enabled)
    {
        editorButtonFlash.Visible = enabled;
    }

    private void UpdatePopulation()
    {
        populationLabel.Text = stage!.GameWorld.PlayerSpecies.Population.FormatNumber();

        // Reset box height
        populationLabel.GetParent<Control>().MarginTop = 0;
    }

    private void UpdateProcessPanel()
    {
        if (!processPanel.Visible)
            return;

        if (stage!.Player == null)
        {
            processPanel.ShownData = null;
        }
        else
        {
            processPanel.ShownData = stage.Player.ProcessStatistics;
        }
    }

    private void UpdatePanelSizing(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        var environmentPanelVBoxContainer = environmentPanel.GetNode<VBoxContainer>("VBoxContainer");
        var compoundsPanelVBoxContainer = compoundsPanel.GetNode<VBoxContainer>("VBoxContainer");

        environmentPanelVBoxContainer.RectSize = new Vector2(environmentPanelVBoxContainer.RectMinSize.x, 0);
        compoundsPanelVBoxContainer.RectSize = new Vector2(compoundsPanelVBoxContainer.RectMinSize.x, 0);

        // Multiply interpolation value with delta time to make it not be affected by framerate
        var environmentPanelSize = environmentPanel.RectMinSize.LinearInterpolate(
            new Vector2(environmentPanel.RectMinSize.x, environmentPanelVBoxContainer.RectSize.y), 5 * delta);

        var compoundsPanelSize = compoundsPanel.RectMinSize.LinearInterpolate(
            new Vector2(compoundsPanel.RectMinSize.x, compoundsPanelVBoxContainer.RectSize.y), 5 * delta);

        environmentPanel.RectMinSize = environmentPanelSize;
        compoundsPanel.RectMinSize = compoundsPanelSize;
    }

    private void UpdateAbilitiesHotBar(Microbe player)
    {
        engulfHotkey.Visible = !player.CellTypeProperties.MembraneType.CellWall;
        bindingModeHotkey.Visible = player.CanBind;
        fireToxinHotkey.Visible = player.AgentVacuoleCount > 0;
        unbindAllHotkey.Visible = player.CanUnbind;
        signallingAgentsHotkey.Visible = player.HasSignalingAgent;

        engulfHotkey.Pressed = player.State == Microbe.MicrobeState.Engulf;
        bindingModeHotkey.Pressed = player.State == Microbe.MicrobeState.Binding;
        fireToxinHotkey.Pressed = Input.IsActionPressed(fireToxinHotkey.ActionName);
        unbindAllHotkey.Pressed = Input.IsActionPressed(unbindAllHotkey.ActionName);
        signallingAgentsHotkey.Pressed = Input.IsActionPressed(signallingAgentsHotkey.ActionName);
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

    /// <summary>
    ///   Received for button that opens the menu inside the Microbe Stage.
    /// </summary>
    private void OpenMicrobeStageMenuPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        OpenMenu();
    }

    private void OpenMenu()
    {
        menu.Show();
        GetTree().Paused = true;
    }

    private void CloseMenu()
    {
        menu.Hide();

        if (!paused)
            GetTree().Paused = false;
    }

    private void PauseButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        paused = !paused;
        if (paused)
        {
            pauseButton.Hide();
            resumeButton.Show();
            pauseButton.Pressed = false;

            // Pause the game
            GetTree().Paused = true;
        }
        else
        {
            pauseButton.Show();
            resumeButton.Hide();
            resumeButton.Pressed = false;

            // Unpause the game
            GetTree().Paused = false;
        }
    }

    private void CompoundButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (!leftPanelsActive)
        {
            leftPanelsActive = true;
            animationPlayer.Play("HideLeftPanels");
        }
        else
        {
            leftPanelsActive = false;
            animationPlayer.Play("ShowLeftPanels");
        }
    }

    private void HelpButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        OpenMenu();
        menu.ShowHelpScreen();
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

    private void ProcessPanelButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (processPanel.Visible)
        {
            processPanel.Visible = false;
        }
        else
        {
            processPanel.Show();
        }
    }

    private void OnProcessPanelClosed()
    {
        processPanelButton.Pressed = false;
    }

    private void OnAbilitiesHotBarDisplayChanged(bool displayed)
    {
        hotBar.Visible = displayed;
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

    private void OnBecomeMulticellularPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        multicellularConfirmPopup.PopupCenteredShrink();
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

        // Make sure the game is unpaused
        if (GetTree().Paused)
        {
            PauseButtonPressed();
        }

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.3f, false);
        TransitionManager.Instance.StartTransitions(stage, nameof(MicrobeStage.MoveToMulticellular));

        stage.MovingToEditor = true;
    }

    private void OnBecomeMacroscopicPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // TODO: late multicellular not done yet
        ToolTipManager.Instance.ShowPopup(TranslationServer.Translate("TO_BE_IMPLEMENTED"), 2.5f);
    }

    private class HoveredCompoundControl : HBoxContainer
    {
        private Label compoundName = null!;
        private Label compoundValue = null!;

        public HoveredCompoundControl(Compound compound)
        {
            Compound = compound;
        }

        public Compound Compound { get; }

        public string? Category
        {
            get => compoundValue.Text;
            set => compoundValue.Text = value;
        }

        public Color CategoryColor
        {
            get => compoundValue.Modulate;
            set => compoundValue.Modulate = value;
        }

        public override void _Ready()
        {
            compoundName = new Label();
            compoundValue = new Label();

            MouseFilter = MouseFilterEnum.Ignore;
            TextureRect compoundIcon = GUICommon.Instance.CreateCompoundIcon(Compound.InternalName, 20, 20);
            compoundName.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            compoundName.Text = Compound.Name;
            AddChild(compoundIcon);
            AddChild(compoundName);
            AddChild(compoundValue);
            Visible = false;
        }

        public void UpdateTranslation()
        {
            compoundName.Text = Compound.Name;
        }
    }
}
