using Godot;

public class PlayButton : MarginContainer
{
    private Button pauseButton = null!;
    private Button playButton = null!;

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

    [Export]
    public bool PauseButtonMode { get; set; }

    public override void _Ready()
    {
        pauseButton = GetNode<Button>("Pause");
        playButton = GetNode<Button>("Play");

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

        pauseButton.SetPressedNoSignal(!paused);
        pauseButton.Visible = !paused;
        playButton.SetPressedNoSignal(paused);
        playButton.Visible = paused;

        var styleBox = (StyleBoxFlat)playButton.GetStylebox("normal");
        styleBox.BgColor = new Color(PauseButtonMode ? "11ffd5" : "112b36");
    }

    private void OnButtonUpdate()
    {
        var pauseButtonDrawMode = pauseButton.GetDrawMode();
        var playButtonDrawMode = playButton.GetDrawMode();

        var pauseButtonIcon = pauseButton.GetChild<TextureRect>(0);
        var playButtonIcon = playButton.GetChild<TextureRect>(0);

        if (pauseButton.GetDrawMode() is BaseButton.DrawMode.Pressed or BaseButton.DrawMode.HoverPressed)
        {
            pauseButtonIcon.Modulate = Colors.Black;
        }
        else if (playButton.GetDrawMode() is BaseButton.DrawMode.Pressed or BaseButton.DrawMode.HoverPressed)
        {
            playButtonIcon.Modulate = Colors.Black;
        }
        else if (playButton.GetDrawMode() == BaseButton.DrawMode.Hover)
        {
            playButtonIcon.Modulate = Colors.White;
        }
        else
        {
            pauseButtonIcon.Modulate = Colors.White;
            playButtonIcon.Modulate = PauseButtonMode ? Colors.Black : Colors.White;
        }
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
