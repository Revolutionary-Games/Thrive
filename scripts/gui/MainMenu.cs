using System;
using Godot;

/// <summary>
///   Class managing the main menu and everything in it
/// </summary>
public class MainMenu : Node
{
    public Godot.Collections.Array MenuArray;
    public Texture BackgroundImage;
    public TextureRect Background;
    public ColorRect ScreenFade;

    public AudioStreamPlayer MusicAudio;
    public AudioStreamPlayer GuiAudio;
    public AudioStream ButtonPressSound;
    public VideoPlayer MicrobeIntro;

    public string CurrentScene;
    public string CurrentCutscene;
    public int CurrentMenuIndex;

    public override void _Ready()
    {
        Background = GetNode<TextureRect>("Background");
        ScreenFade = GetNode<ColorRect>("ScreenFade");
        GuiAudio = GetNode<AudioStreamPlayer>("GUIAudio");
        MusicAudio = GetNode<AudioStreamPlayer>("Music");
        MicrobeIntro = GetNode<VideoPlayer>("MicrobeIntro");
        ScreenFade.MouseFilter = Control.MouseFilterEnum.Ignore;

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
        MenuArray = GetNode<Control>("MenuContainers/ButtonsCenterContainer/MenuItems")
            .GetChildren();
        if (MenuArray == null)
        {
            GD.PrintErr("Failed to find all the menu items!");
            return;
        }
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
        BackgroundImage = GD.Load<Texture>(filepath);
        Background.Texture = BackgroundImage;
    }

    public void PlayButtonPressSound()
    {
        if (ButtonPressSound == null)
        {
            ButtonPressSound = GD.Load<AudioStream>("res://assets/sounds/soundeffects/gui/button-hover-click.ogg");
        }

        GuiAudio.Stream = ButtonPressSound;
        GuiAudio.Play();
    }

    public void FadeInWithCutsceneTo(string scene, string cutscene, float fadeDuration)
    {
        var fader = GetNode<Tween>("Fader");
        if (fader == null)
        {
            GD.PrintErr("Failed to find fader node!");
            return;
        }

        ScreenFade.MouseFilter = Control.MouseFilterEnum.Stop;
        fader.InterpolateProperty(ScreenFade, "color", null, new Color(0, 0, 0, 1), fadeDuration);
        fader.Start();
        CurrentScene = scene;
        CurrentCutscene = cutscene;
    }

    public void PlayCutscene(string path)
    {
        var stream = GD.Load<VideoStream>(path);
        MicrobeIntro.Stream = stream;
        if (stream != null)
            MicrobeIntro.Play();
    }

    public void CancelCutscene()
    {
        if (!MicrobeIntro.IsPlaying())
            return;

        MicrobeIntro.Stop();
        OnMicrobeIntroEnded();
    }

    public void OnFaderFinished(Godot.Object obj, NodePath key)
    {
        ScreenFade.Color = new Color(0, 0, 0, 0);
        PlayCutscene(CurrentCutscene);
    }

    public void OnMicrobeIntroEnded()
    {
        ScreenFade.MouseFilter = Control.MouseFilterEnum.Ignore;
        GetTree().ChangeScene(CurrentScene);
    }

    public void SetCurrentMenu(int index)
    {
        var tween = GetNode<Tween>("MenuContainers/MenuTween");
        var curMenu = (Control)MenuArray[CurrentMenuIndex];
        var selectedMenu = (Control)MenuArray[index];

        // Play the slide down animation
        curMenu.Hide();
        selectedMenu.Show();
        tween.InterpolateProperty(selectedMenu, "custom_constants/separation", -35, 10, 0.3f,
            Tween.TransitionType.Sine);
        tween.Start();

        CurrentMenuIndex = index;
    }

    public void NewGamePressed()
    {
        PlayButtonPressSound();
        MusicAudio.Stop();
        FadeInWithCutsceneTo("res://src/microbe_stage/MicrobeStage.tscn",
            "res://assets/videos/microbe_intro2.webm", 1f);
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
