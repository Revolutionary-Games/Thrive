using Godot;

/// <summary>
///   Thriveopedia page displaying information about the current game in progress.
/// </summary>
public partial class ThriveopediaCurrentWorldPage : ThriveopediaPage, IThriveopediaPage
{
#pragma warning disable CA2213
    [Export]
    private RichTextLabel difficultyDetails = null!;
    [Export]
    private RichTextLabel planetDetails = null!;
    [Export]
    private RichTextLabel miscDetails = null!;
#pragma warning restore CA2213

    public string PageName => "CurrentWorld";
    public string TranslatedPageName => Localization.Translate("THRIVEOPEDIA_CURRENT_WORLD_PAGE_TITLE");

    public string? ParentPageName => null;

    public override void _Ready()
    {
        base._Ready();
        UpdateCurrentWorldDetails();
    }

    public override void UpdateCurrentWorldDetails()
    {
        if (CurrentGame == null)
            return;

        var settings = CurrentGame.GameWorld.WorldSettings;

        // For now, just display the world generation settings associated with this game
        difficultyDetails.Text = settings.GetTranslatedDifficultyString();
        planetDetails.Text = settings.GetTranslatedPlanetString();
        miscDetails.Text = settings.GetTranslatedMiscString();
    }

    public override void OnTranslationsChanged()
    {
        UpdateCurrentWorldDetails();
    }
}
