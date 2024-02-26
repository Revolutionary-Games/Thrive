using Godot;

public partial class HUDBottomBar : HBoxContainer
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
    public delegate void OnMenuPressedEventHandler();

    [Signal]
    public delegate void OnPausePressedEventHandler(bool paused);

    [Signal]
    public delegate void OnProcessesPressedEventHandler();

    [Signal]
    public delegate void OnCompoundsToggledEventHandler(bool expanded);

    [Signal]
    public delegate void OnEnvironmentToggledEventHandler(bool expanded);

    [Signal]
    public delegate void OnSuicidePressedEventHandler();

    [Signal]
    public delegate void OnHelpPressedEventHandler();

    [Signal]
    public delegate void OnStatisticsPressedEventHandler();

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
        EmitSignal(nameof(OnMenuPressedEventHandler));
    }

    private void TogglePause()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Paused = !Paused;
        EmitSignal(nameof(OnPausePressedEventHandler), Paused);
    }

    private void ProcessButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnProcessesPressedEventHandler));
    }

    private void CompoundsButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        CompoundsPressed = !CompoundsPressed;
        EmitSignal(nameof(OnCompoundsToggledEventHandler), CompoundsPressed);
    }

    private void EnvironmentButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EnvironmentPressed = !EnvironmentPressed;
        EmitSignal(nameof(OnEnvironmentToggledEventHandler), EnvironmentPressed);
    }

    private void SuicideButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnSuicidePressedEventHandler));
    }

    private void HelpButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnHelpPressedEventHandler));
    }

    private void StatisticsButtonPressed()
    {
        // No need to play a sound as changing Thriveopedia page does it anyway
        EmitSignal(nameof(OnStatisticsPressedEventHandler));
    }

    private void PausePressed(bool paused)
    {
        EmitSignal(nameof(OnPausePressedEventHandler), paused);
    }

    private void UpdateCompoundButton()
    {
        if (compoundsButton == null)
            return;

        compoundsButton.ButtonPressed = CompoundsPressed;
    }

    private void UpdateEnvironmentButton()
    {
        if (environmentButton == null)
            return;

        environmentButton.ButtonPressed = EnvironmentPressed;
    }

    private void UpdateProcessPanelButton()
    {
        if (processPanelButton == null)
            return;

        processPanelButton.ButtonPressed = ProcessesPressed;
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
