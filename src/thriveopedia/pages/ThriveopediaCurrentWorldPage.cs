using Godot;

/// <summary>
///   Thriveopedia page displaying information about the current game in progress.
/// </summary>
public partial class ThriveopediaCurrentWorldPage : ThriveopediaPage
{
    [Export]
    public NodePath? DifficultyDetailsPath;

    [Export]
    public NodePath PlanetDetailsPath = null!;

    [Export]
    public NodePath MiscDetailsPath = null!;

#pragma warning disable CA2213
    private RichTextLabel difficultyDetails = null!;
    private RichTextLabel planetDetails = null!;
    private RichTextLabel miscDetails = null!;
#pragma warning restore CA2213

    public override string PageName => "CurrentWorld";
    public override string TranslatedPageName => TranslationServer.Translate("THRIVEOPEDIA_CURRENT_WORLD_PAGE_TITLE");

    public override string? ParentPageName => null;

    public override void _Ready()
    {
        base._Ready();
        difficultyDetails = GetNode<RichTextLabel>(DifficultyDetailsPath);
        planetDetails = GetNode<RichTextLabel>(PlanetDetailsPath);
        miscDetails = GetNode<RichTextLabel>(MiscDetailsPath);

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

    public override void OnTranslationChanged()
    {
        UpdateCurrentWorldDetails();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (DifficultyDetailsPath != null)
            {
                DifficultyDetailsPath.Dispose();
                PlanetDetailsPath.Dispose();
                MiscDetailsPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
