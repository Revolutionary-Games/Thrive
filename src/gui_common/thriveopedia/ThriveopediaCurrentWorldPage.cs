using Godot;
using System.Globalization;

public class ThriveopediaCurrentWorldPage : ThriveopediaPage
{
    [Export]
    public NodePath DisabledLabelPath = null!;

    [Export]
    public NodePath WorldContentPath = null!;

    [Export]
    public NodePath DifficultyDetailsPath = null!;

    [Export]
    public NodePath PlanetDetailsPath = null!;

    [Export]
    public NodePath MiscDetailsPath = null!;

    private Label disabledLabel = null!;
    private VBoxContainer worldContent = null!;
    private RichTextLabel difficultyDetails = null!;
    private RichTextLabel planetDetails = null!;
    private RichTextLabel miscDetails = null!;

    public override string PageName => "CURRENT_WORLD_PAGE";

    public override string TranslatedPageName => TranslationServer.Translate("CURRENT_WORLD_PAGE");

    public override void _Ready()
    {
        base._Ready();

        disabledLabel = GetNode<Label>(DisabledLabelPath);
        worldContent = GetNode<VBoxContainer>(WorldContentPath);
        difficultyDetails = GetNode<RichTextLabel>(DifficultyDetailsPath);
        planetDetails = GetNode<RichTextLabel>(PlanetDetailsPath);
        miscDetails = GetNode<RichTextLabel>(MiscDetailsPath);

        UpdateCurrentWorldDetails();
    }

    public override void UpdateCurrentWorldDetails()
    {
        disabledLabel.Visible = CurrentGame == null;
        worldContent.Visible = CurrentGame != null;

        // This page should never be visible if opened outside an active game, so ignore this case
        if (CurrentGame == null)
            return;

        AddPageAsChild("ThriveopediaEvolutionaryTreePage", PageName);

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