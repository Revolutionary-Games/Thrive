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

    /// <summary>
    ///   How fast the loading indicator spins
    /// </summary>
    [Export]
    public float SpinnerSpeed = 180.0f;

    private static LoadingScreen instance;

    private Label artDescription;
    private Label loadingMessageLabel;
    private Label loadingDescriptionLabel;
    private Label tipLabel;
    private Control spinner;

    private string loadingMessage = "Loading";
    private string loadingDescription = string.Empty;
    private string tip = "TIP: press the undo button in the editor to correct a mistake";

    private float totalElapsed;

    private LoadingScreen()
    {
        instance = this;
    }

    public static LoadingScreen Instance => instance;

    public string LoadingMessage
    {
        get => loadingMessage;
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

    public override void _Ready()
    {
        artDescription = GetNode<Label>(ArtDescriptionPath);
        loadingMessageLabel = GetNode<Label>(LoadingMessagePath);
        loadingDescriptionLabel = GetNode<Label>(LoadingDescriptionPath);
        tipLabel = GetNode<Label>(TipLabelPath);

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
    public void Show(string message, string description = "")
    {
        if (!Visible)
        {
            OnBecomeVisible();
            Show();
        }

        LoadingMessage = message;
        LoadingDescription = description;
    }

    public void RandomizeTip()
    {
        // TODO: implement randomized tip showing
    }

    public void RandomizeArt()
    {
        // TODO: implement randomized art showing
    }

    public override void _Process(float delta)
    {
        // Only elapse passed time if this is visible
        if (!Visible)
            return;

        // Spin the spinner
        totalElapsed += delta;

        spinner.RectRotation = (int)(totalElapsed * SpinnerSpeed) % 360;
    }

    private void OnBecomeVisible()
    {
        totalElapsed = 0;

        RandomizeArt();
        RandomizeTip();

        // TODO: setup timers to show next art and tip
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
