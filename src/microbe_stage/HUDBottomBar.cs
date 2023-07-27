using Godot;

public class HUDBottomBar : HBoxContainer
{
    /// <summary>
    ///   When false the compound and environment toggles are hidden
    /// </summary>
    [Export]
    public bool ShowCompoundPanelToggles = true;

    /// <summary>
    ///   When false the environment toggle is hidden
    /// </summary>
    [Export]
    public bool ShowEnvironmentButton = true;

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

    /// <summary>
    ///   When false the pause button is hidden
    /// </summary>
    [Export]
    public bool ShowPauseButton = true;

    /// <summary>
    ///   When false the chat button is hidden
    /// </summary>
    [Export]
    public bool ShowChatButton;

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

    [Export]
    public NodePath ChatButtonPath = null!;

#pragma warning disable CA2213
    private PlayButton pauseButton = null!;

    private TextureButton? compoundsButton;
    private TextureButton? environmentButton;
    private TextureButton? processPanelButton;
    private TextureButton? suicideButton;
    private TextureButton? chatButton;
#pragma warning restore CA2213

    private bool compoundsPressed = true;
    private bool environmentPressed = true;
    private bool processPanelPressed;
    private bool chatPressed;

    private bool paused;

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

    [Signal]
    public delegate void OnChatPressed(bool opened);

    public bool Paused
    {
        get => paused;
        set
        {
            paused = value;
            UpdatePauseButton();
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

    [Export]
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

    [Export]
    public bool ChatPressed
    {
        get => chatPressed;
        set
        {
            chatPressed = value;
            UpdateChatButton();
        }
    }

    public override void _Ready()
    {
        pauseButton = GetNode<PlayButton>(PauseButtonPath);

        compoundsButton = GetNode<TextureButton>(CompoundsButtonPath);
        environmentButton = GetNode<TextureButton>(EnvironmentButtonPath);
        processPanelButton = GetNode<TextureButton>(ProcessPanelButtonPath);
        suicideButton = GetNode<TextureButton>(SuicideButtonPath);
        chatButton = GetNode<TextureButton>(ChatButtonPath);

        UpdateCompoundButton();
        UpdateEnvironmentButton();
        UpdateProcessPanelButton();
        UpdatePauseButton();
        UpdateChatButton();

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

    private void ChatButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        ChatPressed = !ChatPressed;
        EmitSignal(nameof(OnChatPressed), ChatPressed);
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

    private void UpdatePauseButton()
    {
        if (pauseButton == null)
            return;

        pauseButton.Paused = Paused;
    }

    private void UpdateChatButton()
    {
        if (chatButton == null)
            return;

        chatButton.Pressed = ChatPressed;
    }

    private void UpdateButtonVisibility()
    {
        if (compoundsButton != null)
            compoundsButton.Visible = ShowCompoundPanelToggles;

        if (environmentButton != null)
            environmentButton.Visible = ShowCompoundPanelToggles || ShowEnvironmentButton;

        if (suicideButton != null)
            suicideButton.Visible = ShowSuicideButton;

        if (processPanelButton != null)
            processPanelButton.Visible = ShowProcessesButton;

        if (pauseButton != null)
            pauseButton.GetParent<Control>().Visible = ShowPauseButton;

        if (chatButton != null)
            chatButton.Visible = ShowChatButton;
    }
}
