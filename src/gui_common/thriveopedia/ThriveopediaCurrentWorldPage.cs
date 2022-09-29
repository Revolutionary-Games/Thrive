using Godot;
using System.Globalization;

public class ThriveopediaCurrentWorldPage : ThriveopediaPage
{
    [Export]
    public NodePath DifficultyDetailsPath = null!;

    [Export]
    public NodePath PlanetDetailsPath = null!;

    [Export]
    public NodePath MiscDetailsPath = null!;

    private RichTextLabel difficultyDetails = null!;
    private RichTextLabel planetDetails = null!;
    private RichTextLabel miscDetails = null!;

    public override string PageName => "CurrentWorld";

    public override string TranslatedPageName => TranslationServer.Translate("CURRENT_WORLD_PAGE");

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

        // TODO: translate some of these values

        difficultyDetails.Text = string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("DIFFICULTY_DETAILS_STRING"),
            settings.Difficulty, settings.MPMultiplier, settings.AIMutationMultiplier, settings.CompoundDensity,
            settings.PlayerDeathPopulationPenalty, settings.GlucoseDecay, settings.OsmoregulationMultiplier, settings.FreeGlucoseCloud,
            settings.PassiveGainOfReproductionCompounds, settings.LimitReproductionCompoundUseSpeed);

        planetDetails.Text = string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("PLANET_DETAILS_STRING"),
            settings.MapType, settings.LAWK, settings.Origin, settings.Seed);

        miscDetails.Text = string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("MISC_DETAILS_STRING"),
            settings.IncludeMulticellular, settings.EasterEggs);
    }
}