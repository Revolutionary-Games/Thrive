using System;
using System.Collections.Generic;
using System.Text;
using Godot;

/// <summary>
///   Manages the microbe HUD display
/// </summary>
public class MicrobeHUD : Node
{
    [Export]
    public NodePath AnimationPlayerPath;
    [Export]
    public NodePath LeftPanelsPath;
    [Export]
    public NodePath MouseHoverPanelPath;
    [Export]
    public NodePath HoveredItemsContainerPath;
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
    public NodePath HelpScreenPath;

    public NodePath EnvironmentPanelPath;
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

    public GridContainer CompoundsPanelBarContainer;
    private AnimationPlayer animationPlayer;
    private PanelContainer mouseHoverPanel;
    private VBoxContainer hoveredItems;
    private NinePatchRect environmentPanel;
    private GridContainer environmentPanelBarContainer;
    private NinePatchRect compoundsPanel;
    private ProgressBar glucoseBar;
    private ProgressBar ammoniaBar;
    private ProgressBar phosphateBar;
    private ProgressBar hydrogenSulfideBar;
    private ProgressBar ironBar;
    private Control agentsPanel;
    private ProgressBar oxytoxyBar;
    private VBoxContainer leftPanels;
    private Control menu;
    private TextureButton pauseButton;
    private TextureButton resumeButton;
    private Label atpLabel;
    private Label hpLabel;
    private Label populationLabel;
    private Label patchLabel;
    private TextureButton editorButton;
    private Node extinctionBox;
    private Node winBox;
    private Control helpScreen;

    private List<HBoxContainer> compoundBarArray = new List<HBoxContainer>();

    /// <summary>
    ///   The TextureProgress node version of the bars.
    /// </summary>
    private Godot.Collections.Array textureHudBars;

    /// <summary>
    ///   Access to the stage to retrieve information for display as
    ///   well as call some player initiated actions.
    /// </summary>
    private MicrobeStage stage;

    /// <summary>
    ///   Show mouse coordinates data in the mouse
    ///   hover box, useful during develop.
    /// </summary>
    private bool showMouseCoordinates = false;

    /// <summary>
    ///   For toggling paused with the pause button.
    /// </summary>
    private bool paused = false;

    // Checks
    private bool environmentCompressed = false;
    private bool compundCompressed = false;
    private bool leftPanelsActive = false;

    public override void _Ready()
    {
        textureHudBars = GetTree().GetNodesInGroup("MicrobeTextureHUDBar");

        mouseHoverPanel = GetNode<PanelContainer>(MouseHoverPanelPath);
        pauseButton = GetNode<TextureButton>(PauseButtonPath);
        resumeButton = GetNode<TextureButton>(ResumeButtonPath);
        leftPanels = GetNode<VBoxContainer>(LeftPanelsPath);
        agentsPanel = GetNode<Control>(AgentsPanelPath);
        environmentPanel = GetNode<NinePatchRect>(EnvironmentPanelPath);
        environmentPanelBarContainer = GetNode<GridContainer>(EnvironmentPanelBarContainerPath);
        glucoseBar = GetNode<ProgressBar>(GlucoseBarPath);
        ammoniaBar = GetNode<ProgressBar>(AmmoniaBarPath);
        phosphateBar = GetNode<ProgressBar>(PhosphateBarPath);
        hydrogenSulfideBar = GetNode<ProgressBar>(HydrogenSulfideBarPath);
        ironBar = GetNode<ProgressBar>(IronBarPath);
        compoundsPanel = GetNode<NinePatchRect>(CompoundsPanelPath);
        CompoundsPanelBarContainer = GetNode<GridContainer>(CompoundsPanelBarContainerPath);
        oxytoxyBar = GetNode<ProgressBar>(OxytoxyBarPath);
        atpLabel = GetNode<Label>(AtpLabelPath);
        hpLabel = GetNode<Label>(HpLabelPath);
        menu = GetNode<Control>(MenuPath);
        animationPlayer = GetNode<AnimationPlayer>(AnimationPlayerPath);
        hoveredItems = GetNode<VBoxContainer>(HoveredItemsContainerPath);
        populationLabel = GetNode<Label>(PopulationLabelPath);
        patchLabel = GetNode<Label>(PatchLabelPath);
        editorButton = GetNode<TextureButton>(EditorButtonPath);
        helpScreen = GetNode<Control>(HelpScreenPath);

        OnEnterStageTransition();

        StoreCompoundBar();
    }

    public void OnEnterStageTransition()
    {
        // Fade out for that smooth satisfying transition
        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeOut, 0.5f);
        TransitionManager.Instance.StartTransitions(null, string.Empty);
    }

    public override void _Process(float delta)
    {
        if (stage == null)
            return;

        if (stage.Player != null)
        {
            UpdateCompoundBars();
            UpdateReproductionProgress();
            UpdateATP();
            UpdateHealth();
        }

        if (stage.Camera != null)
        {
            UpdateHoverInfo();
        }

        UpdatePopulation();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            OpenMicrobeStageMenuPressed();
        }
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

            GUICommon.Instance.TweenUIProperty(environmentPanel, "rect_min_size", environmentPanel.RectMinSize,
                new Vector2(195, 150), 0.3f);
            environmentPanelBarContainer.Columns = 2;
            environmentPanelBarContainer.AddConstantOverride("vseparation", 10);

            foreach (Node bar in bars)
            {
                var progressBar = bar.GetNode<ProgressBar>("Bar");

                GUICommon.Instance.TweenUIProperty(progressBar, "rect_min_size", new Vector2(95, 25),
                    new Vector2(73, 25), 0.3f);
                progressBar.GetNode<Label>("Label").Hide();
                progressBar.GetNode<Label>("Value").Align = Label.AlignEnum.Center;
            }
        }

        if (mode == "expand" && environmentCompressed)
        {
            environmentCompressed = false;

            GUICommon.Instance.TweenUIProperty(environmentPanel, "rect_min_size", environmentPanel.RectMinSize,
                new Vector2(195, 224), 0.3f);
            environmentPanelBarContainer.Columns = 1;
            environmentPanelBarContainer.AddConstantOverride("vseparation", 4);

            foreach (Node bar in bars)
            {
                var progressBar = bar.GetNode<ProgressBar>("Bar");

                GUICommon.Instance.TweenUIProperty(progressBar, "rect_min_size", progressBar.RectMinSize,
                    new Vector2(162, 25), 0.3f);
                progressBar.GetNode<Label>("Label").Show();
                progressBar.GetNode<Label>("Value").Align = Label.AlignEnum.Left;
            }
        }
    }

    public void ResizeCompoundPanel(string mode)
    {
        var bars = CompoundsPanelBarContainer.GetChildren();

        if (mode == "compress" && !compundCompressed)
        {
            compundCompressed = true;

            GUICommon.Instance.TweenUIProperty(compoundsPanel, "rect_min_size", compoundsPanel.RectMinSize,
                new Vector2(253, 120), 0.3f);

            if (bars.Count < 4)
            {
                CompoundsPanelBarContainer.Columns = 2;
            }
            else
            {
                CompoundsPanelBarContainer.Columns = 3;
            }

            foreach (Node bar in bars)
            {
                var progressBar = bar.GetNode<ProgressBar>("Bar");

                GUICommon.Instance.TweenUIProperty(progressBar, "rect_min_size", new Vector2(95, 25),
                    new Vector2(63, 25), 0.3f);
                progressBar.GetNode<Label>("Label").Hide();
            }
        }

        if (mode == "expand" && compundCompressed)
        {
            compundCompressed = false;

            GUICommon.Instance.TweenUIProperty(compoundsPanel, "rect_min_size", compoundsPanel.RectMinSize,
                new Vector2(253, 239), 0.3f);
            CompoundsPanelBarContainer.Columns = 1;

            foreach (Node bar in bars)
            {
                var progressBar = bar.GetNode<ProgressBar>("Bar");

                GUICommon.Instance.TweenUIProperty(progressBar, "rect_min_size", progressBar.RectMinSize,
                    new Vector2(220, 25), 0.3f);
                progressBar.GetNode<Label>("Label").Show();
            }
        }
    }

    /// <summary>
    ///   Enables the editor button.
    /// </summary>
    public void ShowReproductionDialog()
    {
        if (editorButton.Disabled)
        {
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
    }

    /// <summary>
    ///   Disables the editor button.
    /// </summary>
    public void HideReproductionDialog()
    {
        if (!editorButton.Disabled)
        {
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
    }

    public void OnSuicide()
    {
        if (stage.Player != null)
        {
            stage.Player.Damage(9999.0f, "suicide");
        }
    }

    public void UpdatePatchInfo(string patchName)
    {
        patchLabel.Text = "Patch: " + patchName;
    }

    public void EditorButtonPressed()
    {
        GD.Print("Move to editor pressed");

        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeIn, 0.3f, false);
        TransitionManager.Instance.StartTransitions(stage, nameof(MicrobeStage.MoveToEditor));
    }

    public void ShowExtinctionBox()
    {
        if (extinctionBox == null)
        {
            extinctionBox = ExtinctionBoxScene.Instance();
            AddChild(extinctionBox);
        }
    }

    public void ToggleWinBox()
    {
        if (winBox == null)
        {
            winBox = WinBoxScene.Instance();
            AddChild(winBox);

            winBox.GetNode<Timer>("Timer").Connect("timeout", this, nameof(ToggleWinBox));
        }
        else
        {
            winBox.QueueFree();
        }
    }

    public void ToggleHelpScreen()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (!helpScreen.Visible)
        {
            helpScreen.Show();
            menu.Hide();
        }
        else
        {
            helpScreen.Hide();
            menu.Show();
        }
    }

    /// <summary>
    ///   Updates the GUI bars to show only needed compounds
    /// </summary>
    public void UpdateNeededBars()
    {
        if (stage.Player == null)
            return;

        var compounds = stage.Player.Compounds;
        if (!compounds.HasAnyBeenSetUseful())
            return;
        if (compounds.IsUseful("oxytoxy"))
        {
            if (agentsPanel.GetParent() == null)
                leftPanels.AddChild(agentsPanel);
        }
        else
        {
            if (agentsPanel.GetParent() != null)
                leftPanels.RemoveChild(agentsPanel);
        }

        for (int i = 0; i < stage.CompoundArray.Count; i++)
        {
            if (compounds.IsUseful(stage.CompoundArray[i]))
            {
                if (compoundBarArray[i].GetParent() == null)
                    CompoundsPanelBarContainer.AddChild(compoundBarArray[i]);
            }
            else
            {
                if (compoundBarArray[i].GetParent() != null)
                    CompoundsPanelBarContainer.RemoveChild(compoundBarArray[i]);
            }
        }
    }

    /// <summary>
    ///   Stores the compound bar and icon in the class array
    ///   Prevents bar nad icon from being lost when removes from tree
    /// </summary>
    private void StoreCompoundBar()
    {
        foreach (HBoxContainer bar in CompoundsPanelBarContainer.GetChildren())
        {
            compoundBarArray.Add(bar);
        }
    }

    /// <summary>
    ///   Updates the mouse hover box with stuff.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This creates and removes GUI elements every frame.
    ///     Supposedly that's quite expensive, but I think that's
    ///     how the old JS code do it anyway.
    ///   </para>
    /// </remarks>

    private void UpdateHoverInfo()
    {
        foreach (Node children in hoveredItems.GetChildren())
        {
            hoveredItems.RemoveChild(children);

            // Using QueueFree leaves a gap at
            // the bottom of the panel
            children.Free();
        }

        if (mouseHoverPanel.RectSize != new Vector2(270, 130))
            mouseHoverPanel.RectSize = new Vector2(270, 130);

        if (mouseHoverPanel.MarginLeft != -280)
            mouseHoverPanel.MarginLeft = -280;
        if (mouseHoverPanel.MarginRight != -10)
            mouseHoverPanel.MarginRight = -10;

        var compounds = stage.Clouds.GetAllAvailableAt(stage.Camera.CursorWorldPos);

        var builder = new StringBuilder(string.Empty, 250);

        if (showMouseCoordinates)
        {
            builder.AppendFormat("Stuff at {0:F1}, {1:F1}:\n",
                stage.Camera.CursorWorldPos.x, stage.Camera.CursorWorldPos.z);
        }

        var mousePosLabel = hoveredItems.GetParent().GetNode<Label>("MousePos");

        if (compounds.Count == 0)
        {
            builder.Append("Nothing to eat here");
        }
        else
        {
            builder.Append("At cursor:");

            bool first = true;

            // Create for each compound the information in GUI
            foreach (var entry in compounds)
            {
                if (first)
                {
                    var compoundsLabel = new Label();
                    compoundsLabel.RectMinSize = new Vector2(238, 35);
                    compoundsLabel.Valign = Label.VAlign.Center;
                    hoveredItems.AddChild(compoundsLabel);
                    compoundsLabel.Text = "Compounds: ";
                }

                first = false;

                var hBox = new HBoxContainer();
                var compoundText = new Label();

                var readableName = SimulationParameters.Instance.GetCompound(entry.Key).Name;
                var compoundIcon = GUICommon.Instance.CreateCompoundIcon(readableName, 25, 25);

                var compoundsText = new StringBuilder(readableName, 150);
                compoundsText.AppendFormat(": {0:F1}", entry.Value);

                compoundText.Text = compoundsText.ToString();

                hBox.AddChild(compoundIcon);
                hBox.AddChild(compoundText);
                hoveredItems.AddChild(hBox);
            }
        }

        var aiMicrobes = GetTree().GetNodesInGroup(Constants.AI_GROUP);

        // Show the hovered over microbe's species
        foreach (Microbe entry in aiMicrobes)
        {
            var distance = (entry.Translation - stage.Camera.CursorWorldPos).Length();

            // Find only cells that have the mouse
            // position within their membrane
            if (distance > entry.Radius)
                continue;

            var microbeText = new Label();
            microbeText.RectMinSize = new Vector2(238, 40);
            microbeText.Valign = Label.VAlign.Center;
            hoveredItems.AddChild(microbeText);

            microbeText.Text = "Cell of species " + entry.Species.FormattedName;
        }

        mousePosLabel.Text = builder.ToString();
    }

    /// <summary>
    ///   Updates the compound bars with the correct values.
    /// </summary>
    private void UpdateCompoundBars()
    {
        var compounds = stage.Player.Compounds;

        glucoseBar.MaxValue = compounds.Capacity;
        glucoseBar.Value = compounds.GetCompoundAmount("glucose");
        glucoseBar.GetNode<Label>("Value").Text = glucoseBar.Value + " / " + glucoseBar.MaxValue;

        ammoniaBar.MaxValue = compounds.Capacity;
        ammoniaBar.Value = compounds.GetCompoundAmount("ammonia");
        ammoniaBar.GetNode<Label>("Value").Text = ammoniaBar.Value + " / " + ammoniaBar.MaxValue;

        phosphateBar.MaxValue = compounds.Capacity;
        phosphateBar.Value = compounds.GetCompoundAmount("phosphates");
        phosphateBar.GetNode<Label>("Value").Text = phosphateBar.Value + " / " + phosphateBar.MaxValue;

        hydrogenSulfideBar.MaxValue = compounds.Capacity;
        hydrogenSulfideBar.Value = compounds.GetCompoundAmount("hydrogensulfide");
        hydrogenSulfideBar.GetNode<Label>("Value").Text = hydrogenSulfideBar.Value + " / " +
            hydrogenSulfideBar.MaxValue;

        ironBar.MaxValue = compounds.Capacity;
        ironBar.Value = compounds.GetCompoundAmount("iron");
        ironBar.GetNode<Label>("Value").Text = ironBar.Value + " / " + ironBar.MaxValue;

        oxytoxyBar.MaxValue = compounds.Capacity;
        oxytoxyBar.Value = compounds.GetCompoundAmount("oxytoxy");
        oxytoxyBar.GetNode<Label>("Value").Text = oxytoxyBar.Value + " / " + oxytoxyBar.MaxValue;
    }

    private void UpdateReproductionProgress()
    {
        // Get player reproduction progress
        float totalProgress = stage.Player.CalculateReproductionProgress(
            out Dictionary<string, float> gatheredCompounds, out Dictionary<string, float> totalNeededCompounds);

        float fractionOfAmmonia = 0;
        float fractionOfPhosphates = 0;

        try
        {
            fractionOfAmmonia = gatheredCompounds["ammonia"] / totalNeededCompounds["ammonia"];
        }
        catch (Exception e)
        {
            GD.PrintErr("can't get reproduction ammonia progress: ", e);
        }

        try
        {
            fractionOfPhosphates = gatheredCompounds["phosphates"] / totalNeededCompounds["phosphates"];
        }
        catch (Exception e)
        {
            GD.PrintErr("can't get reproduction phosphates progress: ", e);
        }

        foreach (TextureProgress bar in textureHudBars)
        {
            if (bar.Name == "AmmoniaReproductionBar")
            {
                bar.Value = fractionOfAmmonia * bar.MaxValue;
            }

            if (bar.Name == "PhosphateReproductionBar")
            {
                bar.Value = fractionOfPhosphates * bar.MaxValue;
            }
        }
    }

    private void UpdateATP()
    {
        var atp = stage.Player.Compounds.GetCompoundAmount("atp");
        var capacity = stage.Player.Compounds.Capacity;

        foreach (TextureProgress bar in textureHudBars)
        {
            if (bar.Name == "ATPBar")
            {
                GUICommon.Instance.TweenBarValue(bar, atp, capacity);
                atpLabel.Text = Mathf.Round(atp) + " / " + capacity;

                // Hide the progress bar when the atp is less than 1.5
                if (bar.Value < 1.5)
                {
                    bar.TintProgress = new Color(0, 0, 0);
                }
                else
                {
                    bar.TintProgress = new Color(1, 1, 1);
                }
            }
        }
    }

    private void UpdateHealth()
    {
        var hp = stage.Player.Hitpoints;
        var maxHP = stage.Player.MaxHitpoints;

        foreach (TextureProgress bar in textureHudBars)
        {
            if (bar.Name == "HealthBar")
            {
                GUICommon.Instance.TweenBarValue(bar, hp, maxHP);
                hpLabel.Text = Mathf.RoundToInt(hp) + " / " + maxHP;
            }
        }
    }

    private void UpdatePopulation()
    {
        populationLabel.Text = stage.GameWorld.PlayerSpecies.Population.ToString();
    }

    /// <summary>
    ///   Received for button that opens the menu inside the Microbe Stage.
    /// </summary>
    private void OpenMicrobeStageMenuPressed()
    {
        if (menu.Visible)
        {
            menu.Hide();

            if (!paused)
                GetTree().Paused = false;
        }
        else
        {
            menu.Show();
            GetTree().Paused = true;
        }

        GUICommon.Instance.PlayButtonPressSound();
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
        var guiCommon = GUICommon.Instance;

        guiCommon.PlayButtonPressSound();

        if (!leftPanelsActive)
        {
            leftPanelsActive = true;

            guiCommon.TweenUIProperty(environmentPanel, "rect_position", environmentPanel.RectPosition, new Vector2(
                -300, environmentPanel.RectPosition.y), 0.3f, Tween.TransitionType.Linear, Tween.EaseType.Out, 0.1f);
            guiCommon.TweenUIProperty(compoundsPanel, "rect_position", compoundsPanel.RectPosition, new Vector2(
                -300, compoundsPanel.RectPosition.y), 0.3f, Tween.TransitionType.Linear, Tween.EaseType.Out, 0.05f);
            guiCommon.TweenUIProperty(agentsPanel, "rect_position", agentsPanel.RectPosition, new Vector2(
                -300, agentsPanel.RectPosition.y), 0.3f, Tween.TransitionType.Linear, Tween.EaseType.Out);

            guiCommon.TweenUIProperty(leftPanels, "modulate", leftPanels.Modulate, new Color(1, 1, 1, 0), 0.3f);
        }
        else
        {
            leftPanelsActive = false;

            guiCommon.TweenUIProperty(environmentPanel, "rect_position", environmentPanel.RectPosition, new Vector2(
                0, environmentPanel.RectPosition.y), 0.4f, Tween.TransitionType.Linear, Tween.EaseType.In, 0.1f);
            guiCommon.TweenUIProperty(compoundsPanel, "rect_position", compoundsPanel.RectPosition, new Vector2(
                0, compoundsPanel.RectPosition.y), 0.4f, Tween.TransitionType.Linear, Tween.EaseType.In, 0.05f);
            guiCommon.TweenUIProperty(agentsPanel, "rect_position", agentsPanel.RectPosition, new Vector2(
                0, agentsPanel.RectPosition.y), 0.4f, Tween.TransitionType.Linear, Tween.EaseType.In);

            guiCommon.TweenUIProperty(leftPanels, "modulate", new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), 0.3f);
        }
    }

    /// <summary>
    ///   Receiver for exiting game from microbe stage.
    /// </summary>
    private void ExitPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        GetTree().Quit();
    }
}
