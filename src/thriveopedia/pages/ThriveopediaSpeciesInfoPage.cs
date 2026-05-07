/// <summary>
///   Thriveopedia page for displaying information about a specific species.
/// </summary>
public partial class ThriveopediaSpeciesInfoPage : ThriveopediaPage, IThriveopediaPage
{
    private string cachedName = "ERROR_UNINITIALIZED_SPECIES_INFO";

    public string PageName => cachedName;

    public string TranslatedPageName =>
        Localization.Translate("THRIVEOPEDIA_SPECIES_PAGE").FormatSafe(SpeciesToShow.FormattedName);

    public string ParentPageName => "WorldSpecies";

    /// <summary>
    ///   Has to be set to the species to show on this page before doing anything else with this object
    /// </summary>
    public Species SpeciesToShow
    {
        get;
        set
        {
            field = value;
            cachedName = $"species:{SpeciesToShow.ID}";
        }
    } = null!;

    /// <summary>
    ///   Only pinned pages persist, all others are deleted to not clutter the tree
    /// </summary>
    public bool Pinned { get; set; }
}
