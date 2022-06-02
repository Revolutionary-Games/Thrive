using Godot;

public class HUDBottomBar : HBoxContainer
{
    [Export]
    public NodePath PauseButtonPath = null!;

    [Export]
    public NodePath ResumeButtonPath = null!;

    [Export]
    public NodePath CompoundsButtonPath = null!;

    [Export]
    public NodePath ProcessPanelButtonPath = null!;

    private TextureButton? pauseButton;
    private TextureButton resumeButton = null!;

    private TextureButton? compoundsButton;
    private TextureButton? processPanelButton;

    private bool paused;
    private bool compoundsPressed = true;
    private bool processPanelPressed;

    [Signal]
    public delegate void OnMenuPressed();

    [Signal]
    public delegate void OnPausePressed(bool paused);

    [Signal]
    public delegate void OnProcessesPressed();

    [Signal]
    public delegate void OnCompoundsToggled(bool expanded);

    [Signal]
    public delegate void OnSuicidePressed();

    [Signal]
    public delegate void OnHelpPressed();

    public bool Paused
    {
        get => paused;
        set
        {
            paused = value;
            UpdatePauseButtonState();
        }
    }

    public bool CompoundsPressed
    {
        get => compoundsPressed;
        set
        {
            compoundsPressed = value;
            UpdateCompoundButton();
        }
    }

    public bool ProcessesPressed
    {
        get => processPanelPressed;
        set
        {
            processPanelPressed = value;
            UpdateProcessPanelButton();
        }
    }

    public override void _Ready()
    {
        pauseButton = GetNode<TextureButton>(PauseButtonPath);
        resumeButton = GetNode<TextureButton>(ResumeButtonPath);

        compoundsButton = GetNode<TextureButton>(CompoundsButtonPath);
        processPanelButton = GetNode<TextureButton>(ProcessPanelButtonPath);

        UpdatePauseButtonState();
        UpdateCompoundButton();
        UpdateProcessPanelButton();
    }

    private void MenuPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnMenuPressed));
    }

    private void TogglePause()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Paused = !Paused;
        EmitSignal(nameof(OnPausePressed), Paused);
    }

    private void ProcessButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnProcessesPressed));
    }

    private void CompoundsButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        CompoundsPressed = !CompoundsPressed;
        EmitSignal(nameof(OnCompoundsToggled), CompoundsPressed);
    }

    private void SuicideButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnSuicidePressed));
    }

    private void HelpButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnHelpPressed));
    }

    private void UpdatePauseButtonState()
    {
        if (pauseButton == null)
            return;

        if (Paused)
        {
            resumeButton.Visible = true;
            resumeButton.Pressed = true;

            pauseButton.Visible = false;
            pauseButton.Pressed = false;
        }
        else
        {
            resumeButton.Visible = false;
            resumeButton.Pressed = false;

            pauseButton.Visible = true;
        }
    }

    private void UpdateCompoundButton()
    {
        if (compoundsButton == null)
            return;

        compoundsButton.Pressed = CompoundsPressed;
    }

    private void UpdateProcessPanelButton()
    {
        if (processPanelButton == null)
            return;

        processPanelButton.Pressed = ProcessesPressed;
    }
}
