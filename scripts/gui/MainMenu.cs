using Godot;
using System;

public class MainMenu : Node
{
    public Godot.Collections.Array menuArray;
    public Texture backgroundImage;
    public TextureRect background;
    public ColorRect screenFade;
    
    public AudioStreamPlayer musicAudio;
    public AudioStreamPlayer guiAudio;
    public AudioStream buttonPressSound;
    public VideoPlayer microbeIntro;
    
    public string currentScene;
    public string currentCutscene;
    public int currentMenuIndex;

    public override void _Ready()
    {
        background = GetNode<TextureRect>("Background");
        screenFade = GetNode<ColorRect>("ScreenFade");
        guiAudio = GetNode<AudioStreamPlayer>("GUIAudio");
        musicAudio = GetNode<AudioStreamPlayer>("Music");
        microbeIntro = GetNode<VideoPlayer>("MicrobeIntro");
        screenFade.MouseFilter = Control.MouseFilterEnum.Ignore;

        RunMenuSetup();
        RandomizeBackground();
    }
    
    public override void _Input(InputEvent @event)
    {
        if(Input.IsKeyPressed((int)Godot.KeyList.Escape))
        {
            CancelCutscene();
        }
    }
    
    public void RunMenuSetup()
    {
        menuArray = GetNode<Control>("MenuContainers/ButtonsCenterContainer/MenuItems")
            .GetChildren();
        if(menuArray == null)
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
        } else if (num <= 6)
        {
            SetBackground("res://assets/textures/gui/BG_Menu02.png");
        } else if (num <= 9)
        {
            SetBackground("res://assets/textures/gui/BG_Menu03.png");
        }
    }
    
    public void SetBackground(string filepath)
    {
        backgroundImage = GD.Load<Texture>(filepath);
        background.Texture = backgroundImage;
    }
    
    public void PlayButtonPressSound()
    {
        if(buttonPressSound == null)
        {
            buttonPressSound = GD.Load<AudioStream>
            	("res://assets/sounds/soundeffects/gui/button-hover-click.ogg");
        }
        
        guiAudio.Stream = buttonPressSound;
        guiAudio.Play();
    }
    
    public void FadeInWithCutsceneTo(string scene, string cutscene, float fadeDuration)
    {
        var fader = GetNode<Tween>("Fader");
        if(fader == null)
        {
            GD.PrintErr("Failed to find fader node!");
            return;
        }
        
        screenFade.MouseFilter = Control.MouseFilterEnum.Stop;
    	fader.InterpolateProperty(screenFade, "color", null, new Color(0, 0, 0, 1), fadeDuration);
    	fader.Start();
        currentScene = scene;
        currentCutscene = cutscene;
    }
    
    public void PlayCutscene(string path)
    {
    	var stream = GD.Load<VideoStream>(path);
    	microbeIntro.Stream = stream;
    	if(stream != null)
    		microbeIntro.Play();
    }
    
    public void CancelCutscene()
    {
        if(!microbeIntro.IsPlaying())
            return;
        
        microbeIntro.Stop();
        OnMicrobeIntroEnded();
    }
    
    public void OnFaderFinished(Godot.Object obj, NodePath key)
    {
        screenFade.Color = new Color(0, 0, 0, 0);
        PlayCutscene(currentCutscene);
    }
    
    public void OnMicrobeIntroEnded()
    {
        screenFade.MouseFilter = Control.MouseFilterEnum.Ignore;
        GetTree().ChangeScene(currentScene);
    }
    
    public void SetCurrentMenu(int index)
    {
        var tween = GetNode<Tween>("MenuContainers/MenuTween");
        var curMenu = (Control)menuArray[currentMenuIndex];
        var selectedMenu = (Control)menuArray[index];

        // Play the slide down animation
        curMenu.Hide();
        selectedMenu.Show();
        tween.InterpolateProperty(selectedMenu, "custom_constants/separation", -35, 10, 0.3f,
            Tween.TransitionType.Sine);
        tween.Start();

        currentMenuIndex = index;
    }

    public void NewGamePressed()
    {
    	PlayButtonPressSound();
    	musicAudio.Stop();
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
