/// <summary>
///   Parent page for all species info pages in the current world
/// </summary>
public partial class ThriveopediaWorldSpeciesPage : ThriveopediaPage, IThriveopediaPage
{
    public string PageName => "WorldSpecies";
    public string TranslatedPageName => Localization.Translate("THRIVEOPEDIA_WORLD_SPECIES_TITLE");
    public string ParentPageName => "CurrentWorld";
}
