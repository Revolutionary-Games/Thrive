using Godot;

public partial class PlayButton : MarginContainer
{
#pragma warning disable CA2213
    private Button? pauseButton;
    private Button? playButton;
#pragma warning restore CA2213

    private string? pauseButtonTooltip;
    private string? playButtonTooltip;
    private bool paused;

    [Signal]
    public delegate void OnPressedEventHandler(bool paused);

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

    /// <summary>
    ///   Toggles pause state. Emits paused signal.
    /// </summary>
    public void Toggle()
    {
        Paused = !Paused;
        OnButtonPressed(Paused ? "pause" : "play");
    }

    private void UpdateButton()
    {
        if (pauseButton == null || playButton == null)
            return;

        pauseButton.SetPressedNoSignal(!paused);
        pauseButton.Visible = !paused;
        playButton.SetPressedNoSignal(paused);
        playButton.Visible = paused;

        var styleBox = (StyleBoxFlat)playButton.GetThemeStylebox("normal");
        styleBox.BgColor = PauseButtonMode ? new Color(0.067f, 1.0f, 0.835f) : new Color(0.067f, 0.169f, 0.212f);
    }

    private void OnButtonUpdate()
    {
        if (pauseButton == null || playButton == null)
            return;

        var pauseButtonDrawMode = pauseButton.GetDrawMode();
        var playButtonDrawMode = playButton.GetDrawMode();

        var pauseButtonIcon = pauseButton.GetChild<TextureRect>(0);
        var playButtonIcon = playButton.GetChild<TextureRect>(0);

        if (pauseButtonDrawMode is BaseButton.DrawMode.Pressed or BaseButton.DrawMode.HoverPressed)
        {
            pauseButtonIcon.Modulate = Colors.Black;
        }
        else if (playButtonDrawMode is BaseButton.DrawMode.Pressed or BaseButton.DrawMode.HoverPressed)
        {
            playButtonIcon.Modulate = Colors.Black;
        }
        else if (playButtonDrawMode == BaseButton.DrawMode.Hover)
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

        if (pauseButtonTooltip != null)
            pauseButton.TooltipText = Localization.Translate(pauseButtonTooltip);

        if (playButtonTooltip != null)
            playButton.TooltipText = Localization.Translate(playButtonTooltip);
    }

    private void OnButtonPressed(string what)
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (what == "pause")
        {
            Paused = true;
        }
        else if (what == "play")
        {
            Paused = false;
        }
        else
        {
            GD.PrintErr("Unknown \"what\" for play button press");
        }

        EmitSignal(SignalName.OnPressed, Paused);
    }
}
