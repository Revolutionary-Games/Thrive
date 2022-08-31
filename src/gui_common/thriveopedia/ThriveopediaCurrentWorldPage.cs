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

    [Export]
    public NodePath EvolutionaryTreePath = null!;

    private RichTextLabel difficultyDetails = null!;
    private RichTextLabel planetDetails = null!;
    private RichTextLabel miscDetails = null!;
    private EvolutionaryTree evolutionaryTree = null!;

    private GameProperties currentGame = null!;

    public override string PageName => "CURRENT_WORLD_PAGE";

    public override void _Ready()
    {
        base._Ready();

        difficultyDetails = GetNode<RichTextLabel>(DifficultyDetailsPath);
        planetDetails = GetNode<RichTextLabel>(PlanetDetailsPath);
        miscDetails = GetNode<RichTextLabel>(MiscDetailsPath);
        evolutionaryTree = GetNode<EvolutionaryTree>(EvolutionaryTreePath);

        UpdateCurrentWorldDetails();
    }

    public override void UpdateCurrentWorldDetails()
    {
        // This page should never be visible if opened outside an active game, so ignore this case
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

        // TODO: split evolutionary tree to a separate page to fix scrolling issues

        evolutionaryTree.Init(CurrentGame.GameWorld.PlayerSpecies);
    }
}