using System;
using Godot;

/// <summary>
///   A loading screen that shows cool stuff. This is autoloaded overlay on top of other scenes.
/// </summary>
public class LoadingScreen : Control
{
    [Export]
    public NodePath ArtDescriptionPath;

    [Export]
    public NodePath LoadingMessagePath;

    [Export]
    public NodePath LoadingDescriptionPath;

    [Export]
    public NodePath TipLabelPath;

    [Export]
    public NodePath RandomizeTipTimerPath;

    /// <summary>
    ///   How fast the loading indicator spins
    /// </summary>
    [Export]
    public float SpinnerSpeed = 180.0f;

    private static LoadingScreen instance;

    private readonly Random random = new Random();

    private Label artDescription;
    private Label loadingMessageLabel;
    private Label loadingDescriptionLabel;
    private Label tipLabel;
    private Control spinner;

    private bool wasVisible;

    private Timer randomizeTipTimer;

    private string loadingMessage;
    private string tip;
    private string loadingDescription = string.Empty;

    private float totalElapsed;
    private MainGameState currentlyLoadingGameState = MainGameState.Invalid;

    private LoadingScreen()
    {
        instance = this;
    }

    public static LoadingScreen Instance => instance;

    public string LoadingMessage
    {
        get => loadingMessage ??= TranslationServer.Translate("LOADING");
        set
        {
            if (loadingMessage == value)
                return;

            loadingMessage = value;

            if (loadingMessageLabel != null)
            {
                UpdateMessage();
            }
        }
    }

    public string LoadingDescription
    {
        get => loadingDescription;
        set
        {
            if (loadingDescription == value)
                return;

            loadingDescription = value;

            if (loadingDescriptionLabel != null)
            {
                UpdateDescription();
            }
        }
    }

    public string Tip
    {
        get => tip;
        set
        {
            if (tip == value)
                return;

            tip = value;

            if (tipLabel != null)
            {
                UpdateTip();
            }
        }
    }

    private MainGameState CurrentlyLoadingGameState
    {
        get => currentlyLoadingGameState;
        set
        {
            if (currentlyLoadingGameState == value)
                return;

            currentlyLoadingGameState = value;
            RandomizeTip();
        }
    }

    public override void _Ready()
    {
        artDescription = GetNode<Label>(ArtDescriptionPath);
        loadingMessageLabel = GetNode<Label>(LoadingMessagePath);
        loadingDescriptionLabel = GetNode<Label>(LoadingDescriptionPath);
        tipLabel = GetNode<Label>(TipLabelPath);
        randomizeTipTimer = GetNode<Timer>(RandomizeTipTimerPath);

        spinner = GetNode<Control>("Spinner");

        UpdateMessage();
        UpdateDescription();
        UpdateTip();
        artDescription.Text = string.Empty;

        Hide();
    }

    /// <summary>
    ///   Shows this and updates the shown messages. If this just became visible also loads new art and tip
    /// </summary>
    public void Show(string message, MainGameState target, string description = "")
    {
        LoadingMessage = message;
        LoadingDescription = description;
        CurrentlyLoadingGameState = target;

        if (!Visible)
        {
            OnBecomeVisible();
            Show();
        }
    }

    public void RandomizeTip()
    {
        if (CurrentlyLoadingGameState == MainGameState.Invalid)
        {
            Tip = string.Empty;
            return;
        }

        var tips = SimulationParameters.Instance.GetHelpTexts(CurrentlyLoadingGameState + "Tips");
        var selectedTip = tips.Messages.Random(random);
        Tip = selectedTip;
    }

    public void RandomizeArt()
    {
        // TODO: implement randomized art showing
    }

    public override void _Process(float delta)
    {
        // Only elapse passed time if this is visible
        if (!Visible)
        {
            if (wasVisible)
            {
                wasVisible = false;
                randomizeTipTimer.Stop();
            }

            return;
        }

        // Spin the spinner
        totalElapsed += delta;

        spinner.RectRotation = (int)(totalElapsed * SpinnerSpeed) % 360;
    }

    private void OnBecomeVisible()
    {
        wasVisible = true;
        totalElapsed = 0;

        RandomizeArt();

        randomizeTipTimer.Start();
    }

    private void UpdateMessage()
    {
        loadingMessageLabel.Text = LoadingMessage;
    }

    private void UpdateDescription()
    {
        loadingDescriptionLabel.Text = LoadingDescription;
    }

    private void UpdateTip()
    {
        tipLabel.Text = Tip;
    }
}
