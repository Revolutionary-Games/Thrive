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
    public NodePath HoveredItemsLabelPath;

    [Export]
    public NodePath AtpLabelPath;

    [Export]
    public NodePath PauseButtonContainerPath;

    [Export]
    public Godot.Collections.Array<AudioStream> MusicTracks;

    [Export]
    public Godot.Collections.Array<AudioStream> AmbientTracks;

    private RichTextLabel hoveredItemsLabel;

    private Control menu;

    private Control pauseButtonContainer;

    private Label atpLabel;

    public AudioStreamPlayer MusicAudio;
    public AudioStreamPlayer AmbientAudio;
    public AudioStreamPlayer GUIAudio;

    private AnimationPlayer animationPlayer;

    /// <summary>
    ///   The HUD bars is contained in this array to avoid
    ///   having tons of separate variables.
    /// </summary>
    private Godot.Collections.Array hudBars;

    /// <summary>
    ///   Access to the stage to retrieve information for display as
    ///   well as call some player initiated actions.
    /// </summary>
    private MicrobeStage stage;

    private bool paused = false;
    private bool environmentCompressed = false;
    private bool compundCompressed = false;
    private bool leftPanelsActive = false;

    public override void _Ready()
    {
        hoveredItemsLabel = GetNode<RichTextLabel>(HoveredItemsLabelPath);
        pauseButtonContainer = GetNode<Control>(PauseButtonContainerPath);
        atpLabel = GetNode<Label>(AtpLabelPath);
        MusicAudio = GetNode<AudioStreamPlayer>("MusicAudio");
        AmbientAudio = GetNode<AudioStreamPlayer>("AmbientAudio");
        GUIAudio = GetNode<AudioStreamPlayer>("MicrobeGUIAudio");
        menu = GetNode<Control>("PauseMenu");
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        hudBars = GetTree().GetNodesInGroup("MicrobeHUDBar");

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
            var compounds = stage.Clouds.GetAllAvailableAt(stage.Camera.CursorWorldPos);

            StringBuilder builder = new StringBuilder("Things at ", 250);

            builder.AppendFormat("{0:F1}, {1:F1}:\n",
                stage.Camera.CursorWorldPos.x, stage.Camera.CursorWorldPos.z);
            builder.Append(CompoundsToString(compounds));

            hoveredItemsLabel.Text = builder.ToString();
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

    // Function to play a blinky sound when a button is pressed
    public void PlayButtonPressSound()
    {
        var sound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");

        GUIAudio.Stream = sound;
        GUIAudio.Play();
    }

    // Received for button that opens the menu inside the Microbe Stage
    public void OpenMicrobeStageMenuPressed()
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

    public void PauseButtonPressed()
    {
        PlayButtonPressSound();

        var pauseButton = pauseButtonContainer.
            GetNode<TextureButton>("Pause");
        var pausedButton = pauseButtonContainer.
            GetNode<TextureButton>("Resume");

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

    public void CompoundButtonPressed()
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

    // Receiver for exiting game from microbe stage
    private void ExitPressed()
    {
        PlayButtonPressSound();
        GetTree().Quit();
    }

    private void PlayRandomMusic()
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

    private void PlayRandomAmbience()
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

    private string CompoundsToString(Dictionary<string, float> compounds)
    {
        var simulation = SimulationParameters.Instance;

        StringBuilder compoundsText = new StringBuilder(string.Empty, 150);

        bool first = true;

        foreach (var entry in compounds)
        {
            if (!first)
                compoundsText.Append("\n");
            first = false;

            var readableName = simulation.GetCompound(entry.Key).Name;
            compoundsText.Append(readableName);
            compoundsText.AppendFormat(": {0:F1}", entry.Value);
        }

        return compoundsText.ToString();
    }

    private void UpdateBars()
    {
        var compounds = stage.Player.Compounds;

        foreach (Node node in hudBars)
        {
            if (node.GetClass() == "ProgressBar")
            {
                var bar = (ProgressBar)node;

                if (bar.Name == "GlucoseBar")
                {
                    bar.MaxValue = compounds.Capacity;
                    bar.Value = compounds.GetCompoundAmount("glucose");
                    bar.GetNode<Label>("Value").Text =
                        bar.Value + " / " + bar.MaxValue;
                }

                if (bar.Name == "AmmoniaBar")
                {
                    bar.MaxValue = compounds.Capacity;
                    bar.Value = compounds.GetCompoundAmount("ammonia");
                    bar.GetNode<Label>("Value").Text =
                        bar.Value + " / " + bar.MaxValue;
                }

                if (bar.Name == "PhosphateBar")
                {
                    bar.MaxValue = compounds.Capacity;
                    bar.Value = compounds.GetCompoundAmount("phosphates");
                    bar.GetNode<Label>("Value").Text =
                        bar.Value + " / " + bar.MaxValue;
                }

                if (bar.Name == "HydrogenSulfideBar")
                {
                    bar.MaxValue = compounds.Capacity;
                    bar.Value = compounds.GetCompoundAmount("hydrogensulfide");
                    bar.GetNode<Label>("Value").Text =
                        bar.Value + " / " + bar.MaxValue;
                }

                if (bar.Name == "IronBar")
                {
                    bar.MaxValue = compounds.Capacity;
                    bar.Value = compounds.GetCompoundAmount("iron");
                    bar.GetNode<Label>("Value").Text =
                        bar.Value + " / " + bar.MaxValue;
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
            }
        }
    }
}
