using System;
using Godot;

/// <summary>
///   Class managing the main menu and everything in it
/// </summary>
public class MainMenu : Node
{
    [Export]
    public uint CurrentMenuIndex;

    public Godot.Collections.Array MenuArray;
    public Texture BackgroundImage;
    public TextureRect Background;

    public AudioStreamPlayer MusicAudio;
    public AudioStreamPlayer GUIAudio;

    public Node CurrentCutsceneInstance;
    public Node CurrentFaderInstance;

    public override void _Ready()
    {
        RunMenuSetup();
        RandomizeBackground();
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsKeyPressed((int)Godot.KeyList.Escape))
        {
            CancelCutscene();
        }
    }

    public void RunMenuSetup()
    {
        if (HasNode("Background"))
            Background = GetNode<TextureRect>("Background");

        if (HasNode("GUIAudio"))
            GUIAudio = GetNode<AudioStreamPlayer>("GUIAudio");

        if (HasNode("Music"))
            MusicAudio = GetNode<AudioStreamPlayer>("Music");

        if (MenuArray != null)
            MenuArray.Clear();

        MenuArray = GetTree().GetNodesInGroup("MenuItem");

        if (MenuArray == null)
        {
            GD.PrintErr("Failed to find all the menu items!");
            return;
        }

        SetCurrentMenu(CurrentMenuIndex, false);
    }

    public void RandomizeBackground()
    {
        Random rand = new Random();
        int num = rand.Next() % 9;

        if (num <= 3)
        {
            SetBackground("res://assets/textures/gui/BG_Menu01.png");
        }
        else if (num <= 6)
        {
            SetBackground("res://assets/textures/gui/BG_Menu02.png");
        }
        else if (num <= 9)
        {
            SetBackground("res://assets/textures/gui/BG_Menu03.png");
        }
    }

    public void SetBackground(string filepath)
    {
        if (Background == null)
        {
            GD.PrintErr("Background object doesn't exist");
            return;
        }

        BackgroundImage = GD.Load<Texture>(filepath);
        Background.Texture = BackgroundImage;
    }

    public void PlayButtonPressSound()
    {
        var sound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");

        GUIAudio.Stream = sound;
        GUIAudio.Play();
    }

    public void FadeIn(string onFinishedMethod, float fadeDuration)
    {
        var scene = GD.Load<PackedScene>("res://scripts/gui/Fade.tscn");

        CurrentFaderInstance = scene.Instance();
        AddChild(CurrentFaderInstance);

        var rect = CurrentFaderInstance.GetNode<ColorRect>("Rect");
        var fader = CurrentFaderInstance.GetNode<Tween>("Fader");

        rect.MouseFilter = Control.MouseFilterEnum.Stop;

        fader.InterpolateProperty(rect, "color", null,
            new Color(0, 0, 0, 1), fadeDuration);

        fader.Start();
        fader.Connect("tween_completed", this, onFinishedMethod, null, 1);
    }

    public void PlayCutscene(string path, string onFinishedMethod)
    {
        var scene = GD.Load<PackedScene>("res://scripts/gui/Cutscene.tscn");

        if (scene == null)
        {
            GD.PrintErr("Failed to load the cutscene player scene");
            return;
        }

        CurrentCutsceneInstance = scene.Instance();
        AddChild(CurrentCutsceneInstance);

        var stream = GD.Load<VideoStream>(path);
        var videoPlayer = CurrentCutsceneInstance.GetNode<VideoPlayer>("VideoPlayer");

        videoPlayer.Stream = stream;
        videoPlayer.Play();
        videoPlayer.Connect("finished", this, onFinishedMethod, null, 1);
    }

    public void CancelCutscene()
    {
        if (CurrentCutsceneInstance == null)
        {
            GD.PrintErr("Cutscene instance doesn't exist");
            return;
        }

        CurrentCutsceneInstance.GetNode<VideoPlayer>("VideoPlayer").EmitSignal("finished");
    }

    public void SetCurrentMenu(uint index, bool slide = true)
    {
        var tween = GetNode<Tween>("MenuContainers/MenuTween");

        if (index > MenuArray.Count - 1)
        {
            GD.PrintErr("Selected menu index is out of range!");
            return;
        }

        foreach (Control menu in MenuArray)
        {
            menu.Hide();

            if (menu.GetIndex() == index)
            {
                menu.Show();

                // Play the slide down animation
                if (slide)
                {
                    tween.InterpolateProperty(menu, "custom_constants/separation", -35,
                        10, 0.3f, Tween.TransitionType.Sine);
                    tween.Start();
                }
            }
        }

        CurrentMenuIndex = index;
    }

    public void OnNewGameFadeFinished(Godot.Object obj, NodePath key)
    {
        // Remove the screen fade node instance
        CurrentFaderInstance.QueueFree();
        CurrentFaderInstance = null;

        PlayCutscene("res://assets/videos/microbe_intro2.webm", "OnMicrobeIntroEnded");
    }

    public void OnMicrobeIntroEnded()
    {
        // Remove the cutscene node instance
        CurrentCutsceneInstance.QueueFree();
        CurrentCutsceneInstance = null;

        GetTree().ChangeScene("res://src/microbe_stage/MicrobeStage.tscn");
    }

    public void NewGamePressed()
    {
        PlayButtonPressSound();
        MusicAudio.Stop();
        FadeIn("OnNewGameFadeFinished", 1f);
    }

    public void ToolsPressed()
    {
        PlayButtonPressSound();
        SetCurrentMenu(1);
    }

    public void FreebuildEditorPressed()
    {
        PlayButtonPressSound();
    }

    public void BackFromToolsPressed()
    {
        PlayButtonPressSound();
        SetCurrentMenu(0);
    }

    public void QuitPressed()
    {
        PlayButtonPressSound();
        GetTree().Quit();
    }
}
