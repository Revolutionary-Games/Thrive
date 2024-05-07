/// <summary>
///  The bare minimum wiki page.
/// </summary>
public partial class SimpleWikiPage : ThriveopediaWikiPage
{
    public override string? ParentPageName => Parent;

    public string? Parent { get; set; }
}
