using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   A loading screen that shows cool stuff. This is an autoloaded overlay on top of other scenes.
/// </summary>
public partial class LoadingScreen : Control
{
    /// <summary>
    ///   How fast the loading indicator spins
    /// </summary>
    [Export]
    public double SpinnerSpeed = Math.PI;

    private static LoadingScreen? instance;

    private readonly Random random = new();

    private readonly List<(Action Action, double Delay)> postLoadingActions = new();

#pragma warning disable CA2213
    [Export]
    private CrossFadableTextureRect artworkRect = null!;

    [Export]
    private Label? artDescriptionLabel;

    [Export]
    private Label? loadingMessageLabel;

    [Export]
    private Label? loadingDescriptionLabel;

    [Export]
    private CustomRichTextLabel? tipLabel;

    [Export]
    private Control spinner = null!;

    [Export]
    private Timer randomizeTimer = null!;
#pragma warning restore CA2213

    private bool wasVisible;

    private string? loadingMessage;
    private string tip = string.Empty;
    private string loadingDescription = string.Empty;
    private string? artDescription;

    private double totalElapsed;
    private double elapsedSinceTipChange;

    private LoadingScreen()
    {
        instance = this;
    }

    public static LoadingScreen Instance => instance ?? throw new InstanceNotLoadedYetException();

    public string LoadingMessage
    {
        get => loadingMessage ??= Localization.Translate("LOADING");
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

    public string Tip
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

    private string? OverrideTips { get; set; }

    /// <summary>
    ///   Handles mapping states that don't have tips to their parent states that do (for example, editors)
    /// </summary>
    /// <param name="target">Original target</param>
    /// <param name="overrideTipsCategory">Original category override</param>
    /// <returns>Mapped values</returns>
    /// <exception cref="ArgumentOutOfRangeException">If an unhandled state is provided</exception>
    public static (MainGameState State, string? OverrideCategory) GetStateFallbackForTips(MainGameState target,
        string? overrideTipsCategory)
    {
        switch (target)
        {
            case MainGameState.MicrobeEditor:
                return (MainGameState.MicrobeStage, overrideTipsCategory);
            case MainGameState.MulticellularEditor:
                return (MainGameState.MicrobeStage, "MulticellularStageTips");
            case MainGameState.MacroscopicEditor:
                return (MainGameState.MacroscopicStage, overrideTipsCategory);
            case MainGameState.AscensionCeremony:
                return (MainGameState.SpaceStage, overrideTipsCategory);

            // Passthrough states
            case MainGameState.Invalid:
            case MainGameState.MicrobeStage:
            case MainGameState.MacroscopicStage:
            case MainGameState.SocietyStage:
            case MainGameState.IndustrialStage:
            case MainGameState.SpaceStage:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, null);
        }

        // No mapping
        return (target, overrideTipsCategory);
    }

    public override void _Ready()
    {
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

            // Run post-loading screen actions
            for (int i = 0; i < postLoadingActions.Count; ++i)
            {
                var item = postLoadingActions[i];

                if (item.Delay > 0)
                {
                    postLoadingActions[i] = (item.Action, item.Delay - delta);
                }
                else
                {
                    item.Action();
                    postLoadingActions.RemoveAt(i);
                    --i;
                }
            }

            return;
        }

        // Spin the spinner
        totalElapsed += delta;

        elapsedSinceTipChange += delta;

        spinner.Rotation = (float)(totalElapsed * SpinnerSpeed) % MathF.Tau;
    }

    /// <summary>
    ///   Shows this and updates the shown messages. If this just became visible, also loads new art and tip
    /// </summary>
    public void Show(string message, MainGameState target, string description = "", string? overrideTipsCategory = null)
    {
        var oldCategory = CurrentlyLoadingGameState;

        // This is called in some cases from the save load method, which means that the main game state can be any
        // main game state, even some without tips, so we do a custom fallback mapping here
        try
        {
            (CurrentlyLoadingGameState, OverrideTips) = GetStateFallbackForTips(target, overrideTipsCategory);
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to get state fallback for tips: ", e);
            CurrentlyLoadingGameState = target;
            OverrideTips = overrideTipsCategory;
        }

        GD.Print($"Showing loading screen for stage {CurrentlyLoadingGameState} with override: {OverrideTips}");

        LoadingMessage = message;
        LoadingDescription = description;

        if (!Visible)
        {
            OnBecomeVisible();
            Show();
        }
        else if (oldCategory != CurrentlyLoadingGameState)
        {
            // Category changed, change tip if it's been a little bit since the tip change to give a chance for new
            // tips to be visible. This mostly applies to save load screen, which otherwise has such a fast end that
            // the user basically never sees stage-specific tips there.
            if (elapsedSinceTipChange >= 1.5 || string.IsNullOrWhiteSpace(Tip))
            {
                RandomizeTip();
            }
        }
    }

    /// <summary>
    ///   Allows queueing actions for when the loading screen ends. Used for the save load system to display the save
    ///   load message at an opportune time independent of how long the stage's loading screen lasts
    /// </summary>
    /// <param name="action">Action to run when the loading screen is next closed</param>
    /// <param name="delay">How long in seconds to wait when being visible</param>
    public void QueueActionForWhenHidden(Action action, double delay = 0)
    {
        postLoadingActions.Add((action, delay));
    }

    public void RandomizeContent()
    {
        RandomizeTip();
        RandomizeArt();
    }

    public void RandomizeTip()
    {
        elapsedSinceTipChange = 0;

        if (CurrentlyLoadingGameState == MainGameState.Invalid && string.IsNullOrWhiteSpace(OverrideTips))
        {
            Tip = string.Empty;
            return;
        }

        HelpTexts tips;

        // Some loading screens are not related to a main game state, so they use a special lookup key.
        // For example, multicellular tips work like this.
        if (!string.IsNullOrWhiteSpace(OverrideTips))
        {
            tips = SimulationParameters.Instance.GetHelpTexts(OverrideTips);
        }
        else
        {
            tips = SimulationParameters.Instance.GetHelpTexts(CurrentlyLoadingGameState + "Tips");
        }

        var selectedTip = tips.Messages.Random(random).Message;
        Tip = selectedTip;
    }

    public void RandomizeArt()
    {
        var gameStateName = CurrentlyLoadingGameState.ToString();
        var gallery = SimulationParameters.Instance.GetGallery("ConceptArt");

        if (!string.IsNullOrWhiteSpace(OverrideTips))
        {
            if (OverrideTips.EndsWith("Tips"))
            {
                gameStateName = OverrideTips.Substring(0, OverrideTips.Length - 4);
            }
            else
            {
                gameStateName = OverrideTips;
            }

            if (!gallery.AssetCategories.ContainsKey(gameStateName))
            {
                // Is not a good override, switch back
                gameStateName = CurrentlyLoadingGameState.ToString();
            }
        }

        var category = gallery.AssetCategories.ContainsKey(gameStateName) ? gameStateName : "General";
        var artwork = gallery.AssetCategories[category].Assets.Random(random);

        artworkRect.Image = GD.Load<Texture2D>(artwork.ResourcePath);
        ArtDescription = artwork.BuildDescription(true);
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

        // The loading screen is still visible, so a lag spike from GC here should be not noticeable, so we do a
        // collection here so that during gameplay it is less likely to run garbage collection
        GC.Collect();
    }

    private void UpdateMessage()
    {
        loadingMessageLabel?.Text = LoadingMessage;
    }

    private void UpdateDescription()
    {
        loadingDescriptionLabel?.Text = LoadingDescription;
    }

    private void UpdateArtDescription()
    {
        artDescriptionLabel?.Text = ArtDescription;
    }

    private void UpdateTip()
    {
        tipLabel?.ExtendedBbcode = Localization.Translate(Tip);
    }
}
