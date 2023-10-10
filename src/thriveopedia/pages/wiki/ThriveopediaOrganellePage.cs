using Godot;

public class ThriveopediaOrganellePage : ThriveopediaWikiPage
{
    [Export]
    public NodePath InfoBoxPath = null!;

    [Export]
    public NodePath DescriptionPath = null!;

    [Export]
    public NodePath RequirementsPath = null!;

    [Export]
    public NodePath ProcessesPath = null!;

    [Export]
    public NodePath ModificationsPath = null!;

    [Export]
    public NodePath EffectsPath = null!;

    [Export]
    public NodePath UpgradesPath = null!;

    [Export]
    public NodePath StrategyPath = null!;

    [Export]
    public NodePath ScientificBackgroundPath = null!;

    private OrganelleInfoBox infoBox = null!;
    private CustomRichTextLabel description = null!;
    private CustomRichTextLabel requirements = null!;
    private CustomRichTextLabel processes = null!;
    private CustomRichTextLabel modifications = null!;
    private CustomRichTextLabel effects = null!;
    private CustomRichTextLabel upgrades = null!;
    private CustomRichTextLabel strategy = null!;
    private CustomRichTextLabel scientificBackground = null!;

    public override string Url => WikiPage.Url;
    public override string PageName => Organelle.InternalName;
    public override string TranslatedPageName => Organelle.Name;
    public override string? ParentPageName => null;

    public GameWiki.OrganelleWikiPage WikiPage { get; set; } = null!;

    public OrganelleDefinition Organelle { get; set; } = null!;

    public override void _Ready()
    {
        base._Ready();

        infoBox = GetNode<OrganelleInfoBox>(InfoBoxPath);
        description = GetNode<CustomRichTextLabel>(DescriptionPath);
        requirements = GetNode<CustomRichTextLabel>(RequirementsPath);
        processes = GetNode<CustomRichTextLabel>(ProcessesPath);
        modifications = GetNode<CustomRichTextLabel>(ModificationsPath);
        effects = GetNode<CustomRichTextLabel>(EffectsPath);
        upgrades = GetNode<CustomRichTextLabel>(UpgradesPath);
        strategy = GetNode<CustomRichTextLabel>(StrategyPath);
        scientificBackground = GetNode<CustomRichTextLabel>(ScientificBackgroundPath);

        UpdateValues();
    }

    private void UpdateValues()
    {
        infoBox.Organelle = Organelle;
        description.ExtendedBbcode = WikiPage.Sections.Description;
        requirements.ExtendedBbcode = WikiPage.Sections.Requirements;
        processes.ExtendedBbcode = WikiPage.Sections.Processes;
        modifications.ExtendedBbcode = WikiPage.Sections.Modifications;
        effects.ExtendedBbcode = WikiPage.Sections.Effects;
        upgrades.ExtendedBbcode = WikiPage.Sections.Upgrades;
        strategy.ExtendedBbcode = WikiPage.Sections.Strategy;
        scientificBackground.ExtendedBbcode = WikiPage.Sections.ScientificBackground;
    }
}
