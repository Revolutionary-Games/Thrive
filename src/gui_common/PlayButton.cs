using Godot;

public class PlayButton : MarginContainer
{
    private TextureButton pauseButton = null!;
    private TextureButton playButton = null!;

    private string? pauseButtonTooltip;
    private string? playButtonTooltip;
    private bool paused;

    [Signal]
    public delegate void OnPressed(bool paused);

    [Export]
    public string? PauseButtonTooltip
    {
        get => pauseButtonTooltip;
        set
        {
            pauseButtonTooltip = value;
            UpdateTooltips();
        }
    }

    [Export]
    public string? PlayButtonTooltip
    {
        get => playButtonTooltip;
        set
        {
            playButtonTooltip = value;
            UpdateTooltips();
        }
    }

    [Export]
    public bool Paused
    {
        get => paused;
        set
        {
            if (paused == value)
                return;

            paused = value;
            UpdateButton();
        }
    }

    public override void _Ready()
    {
        pauseButton = GetNode<TextureButton>("Pause");
        playButton = GetNode<TextureButton>("Play");

        UpdateButton();
        UpdateTooltips();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
            UpdateTooltips();
    }

    private void UpdateButton()
    {
        if (pauseButton == null || playButton == null)
            return;

        pauseButton.Pressed = !paused;
        pauseButton.Visible = !paused;
        playButton.Pressed = paused;
        playButton.Visible = paused;
    }

    private void UpdateTooltips()
    {
        if (pauseButton == null || playButton == null)
            return;

        pauseButton.HintTooltip = TranslationServer.Translate(pauseButtonTooltip);
        playButton.HintTooltip = TranslationServer.Translate(playButtonTooltip);
    }

    private void OnButtonPressed(string what)
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (what == "pause")
        {
            Paused = true;
            EmitSignal(nameof(OnPressed), true);
        }
        else if (what == "play")
        {
            Paused = false;
            EmitSignal(nameof(OnPressed), false);
        }
    }
}
