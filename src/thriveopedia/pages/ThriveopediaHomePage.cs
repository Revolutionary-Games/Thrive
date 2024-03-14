/// <summary>
///   Thriveopedia page displaying welcome information and links to websites.
/// </summary>
public partial class ThriveopediaHomePage : ThriveopediaPage, IThriveopediaPage
{
    public string PageName => "Home";
    public string TranslatedPageName => Localization.Translate("THRIVEOPEDIA_HOME_PAGE_TITLE");

    public string? ParentPageName => null;
}
