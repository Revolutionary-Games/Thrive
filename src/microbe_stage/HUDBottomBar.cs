using Godot;

public class HUDBottomBar : HBoxContainer
{
    /// <summary>
    ///   When false the compound and environment toggles are hidden
    /// </summary>
    [Export]
    public bool ShowCompoundPanelToggles = true;

    /// <summary>
    ///   When false the suicide button is hidden
    /// </summary>
    [Export]
    public bool ShowSuicideButton = true;

    /// <summary>
    ///   When false the microbe processes button is hidden
    /// </summary>
    [Export]
    public bool ShowProcessesButton = true;

    [Export]
    public NodePath? PauseButtonPath;

    [Export]
    public NodePath CompoundsButtonPath = null!;

    [Export]
    public NodePath EnvironmentButtonPath = null!;

    [Export]
    public NodePath ProcessPanelButtonPath = null!;

    [Export]
    public NodePath SuicideButtonPath = null!;

#pragma warning disable CA2213
    private PlayButton pauseButton = null!;

    private TextureButton? compoundsButton;
    private TextureButton? environmentButton;
    private TextureButton? processPanelButton;
    private TextureButton? suicideButton;
#pragma warning restore CA2213

    private bool compoundsPressed = true;
    private bool environmentPressed = true;
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
    public delegate void OnEnvironmentToggled(bool expanded);

    [Signal]
    public delegate void OnSuicidePressed();

    [Signal]
    public delegate void OnHelpPressed();

    [Signal]
    public delegate void OnStatisticsPressed();

    public bool Paused
    {
        get => pauseButton.Paused;
        set => pauseButton.Paused = value;
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

    public bool EnvironmentPressed
    {
        get => environmentPressed;
        set
        {
            environmentPressed = value;
            UpdateEnvironmentButton();
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
        pauseButton = GetNode<PlayButton>(PauseButtonPath);

        compoundsButton = GetNode<TextureButton>(CompoundsButtonPath);
        environmentButton = GetNode<TextureButton>(EnvironmentButtonPath);
        processPanelButton = GetNode<TextureButton>(ProcessPanelButtonPath);
        suicideButton = GetNode<TextureButton>(SuicideButtonPath);

        UpdateCompoundButton();
        UpdateEnvironmentButton();
        UpdateProcessPanelButton();

        UpdateButtonVisibility();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PauseButtonPath != null)
            {
                PauseButtonPath.Dispose();
                CompoundsButtonPath.Dispose();
                EnvironmentButtonPath.Dispose();
                ProcessPanelButtonPath.Dispose();
                SuicideButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
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

    private void EnvironmentButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EnvironmentPressed = !EnvironmentPressed;
        EmitSignal(nameof(OnEnvironmentToggled), EnvironmentPressed);
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

    private void StatisticsButtonPressed()
    {
        // No need to play a sound as changing Thriveopedia page does it anyway
        EmitSignal(nameof(OnStatisticsPressed));
    }

    private void PausePressed(bool paused)
    {
        EmitSignal(nameof(OnPausePressed), paused);
    }

    private void UpdateCompoundButton()
    {
        if (compoundsButton == null)
            return;

        compoundsButton.Pressed = CompoundsPressed;
    }

    private void UpdateEnvironmentButton()
    {
        if (environmentButton == null)
            return;

        environmentButton.Pressed = EnvironmentPressed;
    }

    private void UpdateProcessPanelButton()
    {
        if (processPanelButton == null)
            return;

        processPanelButton.Pressed = ProcessesPressed;
    }

    private void UpdateButtonVisibility()
    {
        if (compoundsButton != null)
            compoundsButton.Visible = ShowCompoundPanelToggles;

        if (environmentButton != null)
            environmentButton.Visible = ShowCompoundPanelToggles;

        if (suicideButton != null)
            suicideButton.Visible = ShowSuicideButton;

        if (processPanelButton != null)
            processPanelButton.Visible = ShowProcessesButton;
    }
}
