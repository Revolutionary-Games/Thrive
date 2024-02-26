using System;
using Godot;

/// <summary>
///   A loading screen that shows cool stuff. This is autoloaded overlay on top of other scenes.
/// </summary>
public partial class LoadingScreen : Control
{
    [Export]
    public NodePath? ArtworkPath;

    [Export]
    public NodePath ArtDescriptionPath = null!;

    [Export]
    public NodePath LoadingMessagePath = null!;

    [Export]
    public NodePath LoadingDescriptionPath = null!;

    [Export]
    public NodePath TipLabelPath = null!;

    [Export]
    public NodePath RandomizeTimerPath = null!;

    [Export]
    public NodePath SpinnerPath = null!;

    /// <summary>
    ///   How fast the loading indicator spins
    /// </summary>
    [Export]
    public float SpinnerSpeed = 180.0f;

    private static LoadingScreen? instance;

    private readonly Random random = new();

#pragma warning disable CA2213
    private CrossFadableTextureRect artworkRect = null!;
    private Label? artDescriptionLabel;
    private Label? loadingMessageLabel;
    private Label? loadingDescriptionLabel;
    private CustomRichTextLabel? tipLabel;
    private Control spinner = null!;

    private Timer randomizeTimer = null!;
#pragma warning restore CA2213

    private bool wasVisible;

    private string? loadingMessage;
    private string? tip;
    private string loadingDescription = string.Empty;
    private string? artDescription;

    private double totalElapsed;

    private LoadingScreen()
    {
        instance = this;
    }

    public static LoadingScreen Instance => instance ?? throw new InstanceNotLoadedYetException();

    public string LoadingMessage
    {
        get => loadingMessage ??= TranslationServer.Translate("LOADING");
        set
        {
            if (loadingMessage == value)
                return;

            loadingMessage = value;
            UpdateMessage();
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
            UpdateDescription();
        }
    }

    public string? ArtDescription
    {
        get => artDescription;
        set
        {
            if (artDescription == value)
                return;

            artDescription = value;
            UpdateArtDescription();
        }
    }

    public string? Tip
    {
        get => tip;
        set
        {
            if (tip == value)
                return;

            tip = value;
            UpdateTip();
        }
    }

    /// <summary>
    ///   The logical size of the Godot rendering area. This is needed by <see cref="InputManager"/> but as that is not
    ///   a control, it cannot get this info by itself, so this property needs to be in some suitable autoload even if
    ///   this doesn't fully make sense here.
    /// </summary>
    public Vector2 LogicalDrawingAreaSize => GetViewportRect().Size;

    private MainGameState CurrentlyLoadingGameState { get; set; } = MainGameState.Invalid;

    public override void _Ready()
    {
        artworkRect = GetNode<CrossFadableTextureRect>(ArtworkPath);
        artDescriptionLabel = GetNode<Label>(ArtDescriptionPath);
        loadingMessageLabel = GetNode<Label>(LoadingMessagePath);
        loadingDescriptionLabel = GetNode<Label>(LoadingDescriptionPath);
        tipLabel = GetNode<CustomRichTextLabel>(TipLabelPath);
        randomizeTimer = GetNode<Timer>(RandomizeTimerPath);
        spinner = GetNode<Control>(SpinnerPath);

        UpdateMessage();
        UpdateDescription();
        UpdateTip();
        UpdateArtDescription();

        Hide();
    }

    public override void _Process(double delta)
    {
        // Only elapse passed time if this is visible
        if (!Visible)
        {
            if (wasVisible)
            {
                wasVisible = false;
                randomizeTimer.Stop();
            }

            return;
        }

        // Spin the spinner
        totalElapsed += delta;

        spinner.Rotation = (int)(totalElapsed * SpinnerSpeed) % 360;
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

    public void RandomizeContent()
    {
        RandomizeTip();
        RandomizeArt();
    }

    public void RandomizeTip()
    {
        if (CurrentlyLoadingGameState == MainGameState.Invalid)
        {
            Tip = string.Empty;
            return;
        }

        var tips = SimulationParameters.Instance.GetHelpTexts(CurrentlyLoadingGameState + "Tips");
        var selectedTip = tips.Messages.Random(random).Message;
        Tip = selectedTip;
    }

    public void RandomizeArt()
    {
        var gameStateName = CurrentlyLoadingGameState.ToString();
        var gallery = SimulationParameters.Instance.GetGallery("ConceptArt");

        var category = gallery.AssetCategories.ContainsKey(gameStateName) ? gameStateName : "General";
        var artwork = gallery.AssetCategories[category].Assets.Random(random);

        artworkRect.Image = GD.Load<Texture2D>(artwork.ResourcePath);
        ArtDescription = artwork.BuildDescription(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ArtworkPath != null)
            {
                ArtworkPath.Dispose();
                ArtDescriptionPath.Dispose();
                LoadingMessagePath.Dispose();
                LoadingDescriptionPath.Dispose();
                TipLabelPath.Dispose();
                RandomizeTimerPath.Dispose();
                SpinnerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnBecomeVisible()
    {
        wasVisible = true;
        totalElapsed = 0;

        RandomizeContent();

        randomizeTimer.Start();
    }

    private void OnBecomeHidden()
    {
        artworkRect.Texture = null;
    }

    private void UpdateMessage()
    {
        if (loadingMessageLabel != null)
            loadingMessageLabel.Text = LoadingMessage;
    }

    private void UpdateDescription()
    {
        if (loadingDescriptionLabel != null)
            loadingDescriptionLabel.Text = LoadingDescription;
    }

    private void UpdateArtDescription()
    {
        if (artDescriptionLabel != null)
            artDescriptionLabel.Text = ArtDescription;
    }

    private void UpdateTip()
    {
        if (tipLabel != null)
            tipLabel.ExtendedBbcode = TranslationServer.Translate(Tip);
    }
}
