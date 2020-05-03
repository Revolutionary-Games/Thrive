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

    private AnimationPlayer animationPlayer;
    private PanelContainer mouseHoverPanel;
    private VBoxContainer hoveredItems;
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

    /// <summary>
    ///   The HUD bars is contained in this array to avoid
    ///   having tons of extra separate variables.
    /// </summary>
    private Godot.Collections.Array hudBars;

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
        hudBars = GetTree().GetNodesInGroup("MicrobeHUDBar");
        textureHudBars = GetTree().GetNodesInGroup("MicrobeTextureHUDBar");

        mouseHoverPanel = GetNode<PanelContainer>(MouseHoverPanelPath);
        pauseButton = GetNode<TextureButton>(PauseButtonPath);
        resumeButton = GetNode<TextureButton>(ResumeButtonPath);
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
            UpdateBars();
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
        if (mode == "compress" && !environmentCompressed)
        {
            animationPlayer.Play("EnvironmentPanelCompress");
            environmentCompressed = true;
        }

        if (mode == "expand" && environmentCompressed)
        {
            animationPlayer.Play("EnvironmentPanelExpand");
            environmentCompressed = false;
        }
    }

    public void ResizeCompoundPanel(string mode)
    {
        if (mode == "compress" && !compundCompressed)
        {
            animationPlayer.Play("CompoundPanelCompress");
            compundCompressed = true;
        }

        if (mode == "expand" && compundCompressed)
        {
            animationPlayer.Play("CompoundPanelExpand");
            compundCompressed = false;
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
    ///   Updates the GUI bars with the correct values.
    /// </summary>
    private void UpdateBars()
    {
        var compounds = stage.Player.Compounds;

        foreach (ProgressBar bar in hudBars)
        {
            var label = bar.GetNode<Label>("Value");

            if (bar.Name == "GlucoseBar")
            {
                bar.MaxValue = compounds.Capacity;
                bar.Value = compounds.GetCompoundAmount("glucose");
                label.Text = bar.Value + " / " + bar.MaxValue;
            }

            if (bar.Name == "AmmoniaBar")
            {
                bar.MaxValue = compounds.Capacity;
                bar.Value = compounds.GetCompoundAmount("ammonia");
                label.Text = bar.Value + " / " + bar.MaxValue;
            }

            if (bar.Name == "PhosphateBar")
            {
                bar.MaxValue = compounds.Capacity;
                bar.Value = compounds.GetCompoundAmount("phosphates");
                label.Text = bar.Value + " / " + bar.MaxValue;
            }

            if (bar.Name == "HydrogenSulfideBar")
            {
                bar.MaxValue = compounds.Capacity;
                bar.Value = compounds.GetCompoundAmount("hydrogensulfide");
                label.Text = bar.Value + " / " + bar.MaxValue;
            }

            if (bar.Name == "IronBar")
            {
                bar.MaxValue = compounds.Capacity;
                bar.Value = compounds.GetCompoundAmount("iron");
                label.Text = bar.Value + " / " + bar.MaxValue;
            }

            if (bar.Name == "OxyToxyBar")
            {
                bar.MaxValue = compounds.Capacity;
                bar.Value = compounds.GetCompoundAmount("oxytoxy");
                label.Text = bar.Value + " / " + bar.MaxValue;
            }
        }
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
        GUICommon.Instance.PlayButtonPressSound();

        if (!leftPanelsActive)
        {
            animationPlayer.Play("HideLeftPanels");
            leftPanelsActive = true;
        }
        else
        {
            animationPlayer.Play("ShowLeftPanels");
            leftPanelsActive = false;
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
