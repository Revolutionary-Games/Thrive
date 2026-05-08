using System.ComponentModel;
using Godot;

/// <summary>
///   Thriveopedia page for displaying information about a specific species.
/// </summary>
public partial class ThriveopediaSpeciesInfoPage : ThriveopediaPage, IThriveopediaPage, ITransientPage
{
#pragma warning disable CA2213
    [Export]
    private CustomRichTextLabel mainText = null!;

#pragma warning restore CA2213

    private string cachedName = "ERROR_UNINITIALIZED_SPECIES_INFO";

    public string PageName => cachedName;

    public string TranslatedPageName =>
        Localization.Translate("THRIVEOPEDIA_SPECIES_PAGE").FormatSafe(SpeciesToShow.FormattedName);

    public string ParentPageName => "WorldSpecies";

    /// <summary>
    ///   Has to be set to the species to show on this page before doing anything else with this object
    /// </summary>
    public required Species SpeciesToShow
    {
        get;
        set
        {
            field = value;
            cachedName = $"species:{SpeciesToShow.ID}";
        }
    }

    /// <summary>
    ///   Only pinned pages persist, all others are deleted to not clutter the tree
    /// </summary>
    public bool Pinned { get; set; }

    public override void _Ready()
    {
        base._Ready();
        RebuildInfo();
    }

    public override void OnThriveopediaOpened()
    {
        RebuildInfo();
    }

    public override void OnTranslationsChanged()
    {
        RebuildInfo();
    }

    private void RebuildInfo()
    {
        var areas = -1;

        if (CurrentGame != null)
        {
            areas = 0;

            foreach (var (_, patch) in CurrentGame.GameWorld.Map.Patches)
            {
                if (patch.FindSpeciesByID(SpeciesToShow.ID) != null)
                {
                    ++areas;
                }
            }
        }

        var stage = SpeciesToShow.StageForDisplay;

        var stageText = Localization.Translate(stage.GetAttribute<DescriptionAttribute>().Description);

        mainText.ExtendedBbcode = Localization.Translate("THRIVEOPEDIA_SPECIES_PAGE_INTRO_TEXT")
            .FormatSafe(SpeciesToShow.FormattedNameBbCode, SpeciesToShow.Population, areas, stageText);

        UpdateInfoBox();
    }

    private void UpdateInfoBox()
    {
        // TODO: implement!
    }
}
