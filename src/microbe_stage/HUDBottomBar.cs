using Godot;

/// <summary>
///   The common HUD bottom left buttons
/// </summary>
public partial class HUDBottomBar : HBoxContainer
{
    /// <summary>
    ///   When false, the compound and environment toggles are hidden
    /// </summary>
    [Export]
    public bool ShowCompoundPanelToggles = true;

    /// <summary>
    ///   When false, the suicide button is hidden
    /// </summary>
    [Export]
    public bool ShowSuicideButton = true;

    /// <summary>
    ///   When false, the microbe processes button is hidden
    /// </summary>
    [Export]
    public bool ShowProcessesButton = true;

#pragma warning disable CA2213
    [Export]
    private PlayButton pauseButton = null!;

    [Export]
    private TextureButton heatButton = null!;

    [Export]
    private BaseButton? speedButton;

    [Export]
    private TextureButton? compoundsButton;

    [Export]
    private TextureButton? environmentButton;

    [Export]
    private TextureButton? processPanelButton;

    [Export]
    private TextureButton? suicideButton;
#pragma warning restore CA2213

    private bool compoundsPressed = true;
    private bool environmentPressed = true;
    private bool processPanelPressed;

    private bool speedModePressed;
    private bool speedModeAvailable = true;

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

    [Signal]
    public delegate void OnHeatToggledEventHandler(bool expanded);

    [Signal]
    public delegate void OnSpeedModeToggledEventHandler(bool enabled);

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

    public bool HeatViewAvailable
    {
        get => !heatButton.Disabled;
        set
        {
            var wanted = !value;
            var previous = heatButton.Disabled;

            if (previous == wanted)
                return;

            heatButton.Disabled = wanted;

            // Ensure the heat view doesn't get stuck on
            if (wanted && heatButton.ButtonPressed)
            {
                heatButton.ButtonPressed = false;
                EmitSignal(SignalName.OnHeatToggled, false);
            }
        }
    }

    [Export]
    public bool SpeedModeAvailable
    {
        get => speedModeAvailable;
        set
        {
            speedModeAvailable = value;

            UpdateSpeedButton();
        }
    }

    public bool SpeedModePressed
    {
        get => speedModePressed;
        set
        {
            if (value == speedModePressed)
                return;

            speedModePressed = value;
            UpdateSpeedButton();
        }
    }

    [Export]
    public Control? ProcessPanelButtonControl { get; set; }

    public override void _Ready()
    {
        UpdateCompoundButton();
        UpdateEnvironmentButton();
        UpdateProcessPanelButton();
        UpdateSpeedButton();

        UpdateButtonVisibility();
    }

    private void MenuPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnMenuPressed);
    }

    private void TogglePause()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Paused = !Paused;
        EmitSignal(SignalName.OnPausePressed, Paused);
    }

    private void ProcessButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnProcessesPressed);
    }

    private void CompoundsButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        CompoundsPressed = !CompoundsPressed;
        EmitSignal(SignalName.OnCompoundsToggled, CompoundsPressed);
    }

    private void EnvironmentButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EnvironmentPressed = !EnvironmentPressed;
        EmitSignal(SignalName.OnEnvironmentToggled, EnvironmentPressed);
    }

    private void SuicideButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnSuicidePressed);
    }

    private void HelpButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnHelpPressed);
    }

    private void StatisticsButtonPressed()
    {
        // No need to play a sound as changing Thriveopedia page does it anyway
        EmitSignal(SignalName.OnStatisticsPressed);
    }

    private void PausePressed(bool paused)
    {
        EmitSignal(SignalName.OnPausePressed, paused);
    }

    private void HeatButtonPressed(bool pressed)
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnHeatToggled, pressed);
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

    private void SpeedButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        SpeedModePressed = !SpeedModePressed;
        EmitSignal(SignalName.OnSpeedModeToggled, SpeedModePressed);
    }

    private void UpdateSpeedButton()
    {
        if (speedButton == null)
            return;

        speedButton.ButtonPressed = SpeedModePressed;
        speedButton.Visible = speedModeAvailable;
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
