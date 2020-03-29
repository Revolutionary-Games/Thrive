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
    public Godot.Collections.Array<AudioStream> MusicTracks;

    [Export]
    public Godot.Collections.Array<AudioStream> AmbientTracks;

    public AudioStreamPlayer MusicAudio;
    public AudioStreamPlayer AmbientAudio;
    public AudioStreamPlayer GUIAudio;

    private AnimationPlayer animationPlayer;

    private PanelContainer mouseHoverPanel;

    private VBoxContainer hoveredItems;

    private Control menu;

    private Control pauseButtonContainer;

    /// <summary>
    ///   The panel that displays HP and ATP values.
    /// </summary>
    private Control dataValue;

    private Label atpLabel;

    /// <summary>
    ///   The HUD bars is contained in this array to avoid
    ///   having tons of extra separate variables.
    /// </summary>
    private Godot.Collections.Array hudBars;

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
        mouseHoverPanel = GetNode<PanelContainer>("MouseHoverPanel");
        pauseButtonContainer = GetNode("BottomBar").
            GetNode<MarginContainer>("PauseButtonMargin");
        dataValue = GetNode("BottomRight").GetNode<PanelContainer>("DataValue");
        atpLabel = dataValue.GetNode("Margin").GetNode("VBox").
            GetNode<Label>("ATPValue");
        MusicAudio = GetNode<AudioStreamPlayer>("MusicAudio");
        AmbientAudio = GetNode<AudioStreamPlayer>("AmbientAudio");
        GUIAudio = GetNode<AudioStreamPlayer>("MicrobeGUIAudio");
        menu = GetNode<Control>("PauseMenu");
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        hudBars = GetTree().GetNodesInGroup("MicrobeHUDBar");
        hoveredItems = mouseHoverPanel.GetChild(0).GetChild(0).
            GetNode<VBoxContainer>("HoveredItems");

        // Play the tracks
        PlayRandomMusic();

        PlayRandomAmbience();
    }

    public override void _Process(float delta)
    {
        if (stage == null)
            return;

        if (stage.Player != null)
        {
            UpdateBars();
        }

        if (stage.Camera != null)
        {
            UpdateHoverInfo();
        }
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

    /// <summary>
    ///   Function to play a blinky sound when a button is pressed.
    /// </summary>
    public void PlayButtonPressSound()
    {
        var sound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");

        GUIAudio.Stream = sound;
        GUIAudio.Play();
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
        // code goes here
    }

    /// <summary>
    ///   Disables the editor button.
    /// </summary>
    public void HideReproductionDialog()
    {
        // code goes here
    }

    public void PlayRandomMusic()
    {
        if (MusicTracks == null)
        {
            GD.Print("No music track found");
            return;
        }

        var random = new Random();
        int index = random.Next(MusicTracks.Count);

        MusicAudio.Stream = MusicTracks[index];
        MusicAudio.Play();
    }

    public void PlayRandomAmbience()
    {
        if (AmbientTracks == null)
        {
            GD.Print("No ambient track found");
            return;
        }

        var random = new Random();
        int index = random.Next(AmbientTracks.Count);

        // Lower the audio volume if the current track playing
        // is microbe-ambience2 as it is quite loud.
        // todo: eventually use audio mixing?
        if (index == 1)
        {
            AmbientAudio.VolumeDb = -25;
        }
        else
        {
            AmbientAudio.VolumeDb = -5;
        }

        AmbientAudio.Stream = AmbientTracks[index];
        AmbientAudio.Play();
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
    public void UpdateHoverInfo()
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

        var compounds = stage.Clouds.GetAllAvailableAt(stage.Camera.CursorWorldPos);

        StringBuilder builder = new StringBuilder(string.Empty, 250);

        if (showMouseCoordinates)
        {
            builder.AppendFormat("Stuff at {0:F1}, {1:F1}:\n",
                stage.Camera.CursorWorldPos.x, stage.Camera.CursorWorldPos.z);
        }

        var mousePosLabel = hoveredItems.GetParent().GetNode<Label>("MousePos");

        var simulation = SimulationParameters.Instance;

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
                    var type = new Label();
                    type.RectMinSize = new Vector2(238, 35);
                    type.Valign = Label.VAlign.Center;
                    hoveredItems.AddChild(type);
                    type.Text = "Compounds: ";
                }

                first = false;

                var hBox = new HBoxContainer();
                var image = new TextureRect();
                var compoundText = new Label();

                hBox.AddChild(image);
                hBox.AddChild(compoundText);
                hoveredItems.AddChild(hBox);

                var readableName = simulation.GetCompound(entry.Key).Name;

                var src = "res://assets/textures/gui/bevel/";
                src += readableName.ReplaceN(" ", string.Empty) + ".png";

                image.Texture = GD.Load<Texture>(src);
                image.Expand = true;
                image.RectMinSize = new Vector2(25, 25);
                image.SizeFlagsVertical = 0;

                StringBuilder compoundsText = new StringBuilder(readableName, 150);
                compoundsText.AppendFormat(": {0:F1}", entry.Value);

                compoundText.Text = compoundsText.ToString();
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
    public void UpdateBars()
    {
        var compounds = stage.Player.Compounds;

        foreach (Node node in hudBars)
        {
            if (node.GetClass() == "ProgressBar")
            {
                var bar = (ProgressBar)node;
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
            else if (node.GetClass() == "TextureProgress")
            {
                var bar = (TextureProgress)node;

                if (node.Name == "ATPBar")
                {
                    bar.MaxValue = compounds.Capacity;
                    bar.Value = compounds.GetCompoundAmount("atp");
                    atpLabel.Text = bar.Value + " / " + bar.MaxValue;
                }

                // todo: Health bar
            }
        }
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

        PlayButtonPressSound();
    }

    private void PauseButtonPressed()
    {
        PlayButtonPressSound();

        var pauseButton = pauseButtonContainer.GetNode<TextureButton>("Pause");
        var pausedButton = pauseButtonContainer.GetNode<TextureButton>("Resume");

        paused = !paused;
        if (paused)
        {
            pauseButton.Hide();
            pausedButton.Show();
            pauseButton.Pressed = false;

            // Pause the game
            GetTree().Paused = true;
        }
        else
        {
            pauseButton.Show();
            pausedButton.Hide();
            pausedButton.Pressed = false;

            // Unpause the game
            GetTree().Paused = false;
        }
    }

    private void CompoundButtonPressed()
    {
        PlayButtonPressSound();

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
        PlayButtonPressSound();
        GetTree().Quit();
    }
}
