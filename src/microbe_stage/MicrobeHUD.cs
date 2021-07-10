﻿using System;
using System.Globalization;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   Manages the microbe HUD
/// </summary>
public class MicrobeHUD : Node
{
    [Export]
    public NodePath AnimationPlayerPath;

    [Export]
    public NodePath PanelsTweenPath;

    [Export]
    public NodePath LeftPanelsPath;

    [Export]
    public NodePath MouseHoverPanelPath;

    [Export]
    public NodePath HoveredCompoundsContainerPath;

    [Export]
    public NodePath HoverPanelSeparatorPath;

    [Export]
    public NodePath HoveredCellsContainerPath;

    [Export]
    public NodePath MenuPath;

    [Export]
    public NodePath PauseButtonPath;

    [Export]
    public NodePath ResumeButtonPath;

    [Export]
    public NodePath AtpLabelPath;

    [Export]
    public NodePath HpLabelPath;

    [Export]
    public NodePath PopulationLabelPath;

    [Export]
    public NodePath PatchLabelPath;

    [Export]
    public NodePath EditorButtonPath;

    [Export]
    public NodePath EnvironmentPanelPath;

    [Export]
    public NodePath OxygenBarPath;

    [Export]
    public NodePath Co2BarPath;

    [Export]
    public NodePath NitrogenBarPath;

    [Export]
    public NodePath TemperaturePath;

    [Export]
    public NodePath SunlightPath;

    [Export]
    public NodePath PressurePath;

    [Export]
    public NodePath EnvironmentPanelBarContainerPath;

    [Export]
    public NodePath CompoundsPanelPath;

    [Export]
    public NodePath GlucoseBarPath;

    [Export]
    public NodePath AmmoniaBarPath;

    [Export]
    public NodePath PhosphateBarPath;

    [Export]
    public NodePath HydrogenSulfideBarPath;

    [Export]
    public NodePath IronBarPath;

    [Export]
    public NodePath CompoundsPanelBarContainerPath;

    [Export]
    public NodePath AgentsPanelPath;

    [Export]
    public NodePath OxytoxyBarPath;

    [Export]
    public NodePath AgentsPanelBarContainerPath;

    [Export]
    public NodePath AtpBarPath;

    [Export]
    public NodePath HealthBarPath;

    [Export]
    public NodePath AmmoniaReproductionBarPath;

    [Export]
    public NodePath PhosphateReproductionBarPath;

    [Export]
    public NodePath ProcessPanelPath;

    [Export]
    public NodePath ProcessPanelButtonPath;

    [Export]
    public NodePath HintTextPath;

    [Export]
    public PackedScene ExtinctionBoxScene;

    [Export]
    public PackedScene WinBoxScene;

    [Export]
    public AudioStream MicrobePickupOrganelleSound;

    [Export]
    public Texture AmmoniaBW;

    [Export]
    public Texture PhosphatesBW;

    [Export]
    public Texture AmmoniaInv;

    [Export]
    public Texture PhosphatesInv;

    private readonly Compound ammonia = SimulationParameters.Instance.GetCompound("ammonia");
    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");
    private readonly Compound carbondioxide = SimulationParameters.Instance.GetCompound("carbondioxide");
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound hydrogensulfide = SimulationParameters.Instance.GetCompound("hydrogensulfide");
    private readonly Compound iron = SimulationParameters.Instance.GetCompound("iron");
    private readonly Compound nitrogen = SimulationParameters.Instance.GetCompound("nitrogen");
    private readonly Compound oxygen = SimulationParameters.Instance.GetCompound("oxygen");
    private readonly Compound oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
    private readonly Compound phosphates = SimulationParameters.Instance.GetCompound("phosphates");
    private readonly Compound sunlight = SimulationParameters.Instance.GetCompound("sunlight");

    private AnimationPlayer animationPlayer;
    private MarginContainer mouseHoverPanel;
    private VBoxContainer hoveredCompoundsContainer;
    private HSeparator hoveredCellsSeparator;
    private VBoxContainer hoveredCellsContainer;
    private Panel environmentPanel;
    private GridContainer environmentPanelBarContainer;
    private Panel compoundsPanel;

    private ProgressBar oxygenBar;
    private ProgressBar co2Bar;
    private ProgressBar nitrogenBar;
    private ProgressBar temperature;
    private ProgressBar sunlightLabel;

    // TODO: implement changing pressure conditions
    // ReSharper disable once NotAccessedField.Local
    private ProgressBar pressure;

    private GridContainer compoundsPanelBarContainer;
    private ProgressBar glucoseBar;
    private ProgressBar ammoniaBar;
    private ProgressBar phosphateBar;
    private ProgressBar hydrogenSulfideBar;
    private ProgressBar ironBar;

    private Control agentsPanel;
    private ProgressBar oxytoxyBar;

    private TextureProgress atpBar;
    private TextureProgress healthBar;
    private TextureProgress ammoniaReproductionBar;
    private TextureProgress phosphateReproductionBar;

    private PauseMenu menu;
    private TextureButton pauseButton;
    private TextureButton resumeButton;
    private Label atpLabel;
    private Label hpLabel;
    private Label populationLabel;
    private Label patchLabel;
    private TextureButton editorButton;
    private Node extinctionBox;
    private Node winBox;
    private Tween panelsTween;
    private Control winExtinctBoxHolder;
    private Label hintText;

    private Array compoundBars;

    private ProcessPanel processPanel;
    private TextureButton processPanelButton;

    /// <summary>
    ///   Access to the stage to retrieve information for display as
    ///   well as call some player initiated actions.
    /// </summary>
    private MicrobeStage stage;

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

        winExtinctBoxHolder = GetNode<Control>("WinExtinctBoxHolder");

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

        atpLabel = GetNode<Label>(AtpLabelPath);
        hpLabel = GetNode<Label>(HpLabelPath);
        menu = GetNode<PauseMenu>(MenuPath);
        animationPlayer = GetNode<AnimationPlayer>(AnimationPlayerPath);
        hoveredCompoundsContainer = GetNode<VBoxContainer>(HoveredCompoundsContainerPath);
        hoveredCellsSeparator = GetNode<HSeparator>(HoverPanelSeparatorPath);
        hoveredCellsContainer = GetNode<VBoxContainer>(HoveredCellsContainerPath);
        populationLabel = GetNode<Label>(PopulationLabelPath);
        patchLabel = GetNode<Label>(PatchLabelPath);
        editorButton = GetNode<TextureButton>(EditorButtonPath);
        hintText = GetNode<Label>(HintTextPath);

        processPanel = GetNode<ProcessPanel>(ProcessPanelPath);
        processPanelButton = GetNode<TextureButton>(ProcessPanelButtonPath);
    }

    public void OnEnterStageTransition(bool longerDuration)
    {
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
            UpdateReproductionProgress();
        }

        UpdateATP(delta);
        UpdateHealth(delta);

        if (stage.Camera != null)
        {
            UpdateHoverInfo(delta);
        }

        UpdatePopulation();
        UpdateProcessPanel();
        UpdatePanelSizing(delta);
    }

    public void Init(MicrobeStage stage)
    {
        this.stage = stage;
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
        if (!editorButton.Disabled)
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
        stage.Player?.Damage(9999.0f, "suicide");
    }

    public void UpdatePatchInfo(string patchName)
    {
        // Patch: {0}
        patchLabel.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("MICROBE_PATCH_LABEL"), patchName);
    }

    public void EditorButtonPressed()
    {
        GD.Print("Move to editor pressed");

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
    }

    public void ShowExtinctionBox()
    {
        if (extinctionBox != null)
            return;

        winExtinctBoxHolder.Show();

        extinctionBox = ExtinctionBoxScene.Instance();
        winExtinctBoxHolder.AddChild(extinctionBox);
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

        winBox = WinBoxScene.Instance();
        winExtinctBoxHolder.AddChild(winBox);

        winBox.GetNode<Timer>("Timer").Connect("timeout", this, nameof(ToggleWinBox));
    }

    /// <summary>
    ///   Updates the GUI bars to show only needed compounds
    /// </summary>
    public void UpdateNeededBars()
    {
        var compounds = stage.Player?.Compounds;

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

    public void UpdateEnvironmentalBars(BiomeConditions biome)
    {
        var oxygenPercentage = biome.Compounds[oxygen].Dissolved * 100;
        var co2Percentage = biome.Compounds[carbondioxide].Dissolved * 100;
        var nitrogenPercentage = biome.Compounds[nitrogen].Dissolved * 100;
        var sunlightPercentage = biome.Compounds[sunlight].Dissolved * 100;
        var averageTemperature = biome.AverageTemperature;

        oxygenBar.MaxValue = 100;
        oxygenBar.Value = oxygenPercentage;
        oxygenBar.GetNode<Label>("Value").Text = oxygenPercentage + "%";

        co2Bar.MaxValue = 100;
        co2Bar.Value = co2Percentage;
        co2Bar.GetNode<Label>("Value").Text = co2Percentage + "%";

        nitrogenBar.MaxValue = 100;
        nitrogenBar.Value = nitrogenPercentage;
        nitrogenBar.GetNode<Label>("Value").Text = nitrogenPercentage + "%";

        sunlightLabel.GetNode<Label>("Value").Text = sunlightPercentage + "%";
        temperature.GetNode<Label>("Value").Text = averageTemperature + " °C";

        // TODO: pressure?
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

        // Refresh compounds list

        // Using QueueFree leaves a gap at the bottom of the panel
        hoveredCompoundsContainer.FreeChildren();

        // Refresh cells list
        hoveredCellsContainer.FreeChildren();

        if (mouseHoverPanel.RectSize != new Vector2(240, 80))
            mouseHoverPanel.RectSize = new Vector2(240, 80);

        if (mouseHoverPanel.MarginLeft != -240)
            mouseHoverPanel.MarginLeft = -240;
        if (mouseHoverPanel.MarginRight != 0)
            mouseHoverPanel.MarginRight = 0;

        var container = mouseHoverPanel.GetNode("PanelContainer/MarginContainer/VBoxContainer");
        var mousePosLabel = container.GetNode<Label>("MousePos");
        var nothingHere = container.GetNode<MarginContainer>("NothingHere");

        if (showMouseCoordinates)
        {
            mousePosLabel.Text = string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("STUFF_AT"),
                stage.Camera.CursorWorldPos.x, stage.Camera.CursorWorldPos.z) + "\n";
        }

        if (stage.CompoundsAtMouse.Count == 0)
        {
            hoveredCompoundsContainer.GetParent<VBoxContainer>().Visible = false;
        }
        else
        {
            hoveredCompoundsContainer.GetParent<VBoxContainer>().Visible = true;

            // Create for each compound the information in GUI
            foreach (var entry in stage.CompoundsAtMouse)
            {
                // It is not useful to show trace amounts of a compound, so those are skipped
                if (entry.Value < 0.1)
                    continue;

                var hBox = new HBoxContainer();
                var compoundName = new Label();
                var compoundValue = new Label();

                var compoundIcon = GUICommon.Instance.CreateCompoundIcon(entry.Key.InternalName, 20, 20);

                compoundName.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
                compoundName.Text = entry.Key.Name;

                compoundValue.Text = string.Format(CultureInfo.CurrentCulture, "{0:F1}", entry.Value);

                hBox.AddChild(compoundIcon);
                hBox.AddChild(compoundName);
                hBox.AddChild(compoundValue);
                hoveredCompoundsContainer.AddChild(hBox);
            }
        }

        // Show the species name of hovered cells
        foreach (var entry in stage.MicrobesAtMouse)
        {
            // TODO: Combine cells of same species within mouse over
            // into a single line with total number of them

            var microbeText = new Label();
            microbeText.Valign = Label.VAlign.Center;
            hoveredCellsContainer.AddChild(microbeText);

            microbeText.Text = entry.Species.FormattedName;

            if (entry.IsPlayerMicrobe)
                microbeText.Text += " (" + TranslationServer.Translate("PLAYER_CELL") + ")";
        }

        hoveredCellsSeparator.Visible = hoveredCellsContainer.GetChildCount() > 0 &&
            hoveredCompoundsContainer.GetChildCount() > 0;

        hoveredCellsContainer.GetParent<VBoxContainer>().Visible = hoveredCellsContainer.GetChildCount() > 0;

        if (stage.CompoundsAtMouse.Count > 0 || hoveredCellsContainer.GetChildCount() > 0)
        {
            nothingHere.Hide();
        }
        else
        {
            nothingHere.Show();
        }
    }

    /// <summary>
    ///   Updates the compound bars with the correct values.
    /// </summary>
    private void UpdateCompoundBars()
    {
        var compounds = GetPlayerColonyOrPlayerStorage();

        glucoseBar.MaxValue = compounds.Capacity;
        glucoseBar.Value = compounds.GetCompoundAmount(glucose);
        glucoseBar.GetNode<Label>("Value").Text = glucoseBar.Value + " / " + glucoseBar.MaxValue;

        ammoniaBar.MaxValue = compounds.Capacity;
        ammoniaBar.Value = compounds.GetCompoundAmount(ammonia);
        ammoniaBar.GetNode<Label>("Value").Text = ammoniaBar.Value + " / " + ammoniaBar.MaxValue;

        phosphateBar.MaxValue = compounds.Capacity;
        phosphateBar.Value = compounds.GetCompoundAmount(phosphates);
        phosphateBar.GetNode<Label>("Value").Text = phosphateBar.Value + " / " + phosphateBar.MaxValue;

        hydrogenSulfideBar.MaxValue = compounds.Capacity;
        hydrogenSulfideBar.Value = compounds.GetCompoundAmount(hydrogensulfide);
        hydrogenSulfideBar.GetNode<Label>("Value").Text = hydrogenSulfideBar.Value + " / " +
            hydrogenSulfideBar.MaxValue;

        ironBar.MaxValue = compounds.Capacity;
        ironBar.Value = compounds.GetCompoundAmount(iron);
        ironBar.GetNode<Label>("Value").Text = ironBar.Value + " / " + ironBar.MaxValue;

        oxytoxyBar.MaxValue = compounds.Capacity;
        oxytoxyBar.Value = compounds.GetCompoundAmount(oxytoxy);
        oxytoxyBar.GetNode<Label>("Value").Text = oxytoxyBar.Value + " / " + oxytoxyBar.MaxValue;
    }

    private void UpdateReproductionProgress()
    {
        // Get player reproduction progress
        stage.Player.CalculateReproductionProgress(
            out System.Collections.Generic.Dictionary<Compound, float> gatheredCompounds,
            out System.Collections.Generic.Dictionary<Compound, float> totalNeededCompounds);

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
        var capacity = 4.0f;

        if (stage.Player != null)
        {
            var compounds = GetPlayerColonyOrPlayerStorage();

            atpAmount = Mathf.Ceil(compounds.GetCompoundAmount(atp));
            capacity = compounds.Capacity;
        }

        atpBar.MaxValue = capacity;
        atpBar.Value = MathUtils.Lerp((float)atpBar.Value, atpAmount, 3.0f * delta, 0.1f);
        atpLabel.Text = StringUtils.FormatNumber(atpAmount) + " / " + StringUtils.FormatNumber(capacity);
    }

    private ICompoundStorage GetPlayerColonyOrPlayerStorage()
    {
        return stage.Player.Colony?.ColonyCompounds ?? (ICompoundStorage)stage.Player.Compounds;
    }

    private void UpdateHealth(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        var hp = 0.0f;
        var maxHP = 100.0f;

        if (stage.Player != null)
        {
            hp = stage.Player.Hitpoints;
            maxHP = stage.Player.MaxHitpoints;
        }

        healthBar.MaxValue = maxHP;
        healthBar.Value = MathUtils.Lerp((float)healthBar.Value, hp, 3.0f * delta, 0.1f);
        hpLabel.Text = StringUtils.FormatNumber(Mathf.Round(hp)) + " / " + StringUtils.FormatNumber(maxHP);
    }

    private void UpdatePopulation()
    {
        populationLabel.Text = StringUtils.FormatNumber(stage.GameWorld.PlayerSpecies.Population);

        // Reset box height
        populationLabel.GetParent<Control>().MarginTop = 0;
    }

    private void UpdateProcessPanel()
    {
        if (!processPanel.Visible)
            return;

        if (stage.Player == null)
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
}
