/// <summary>
///   The bare minimum wiki page. Requires the parent to be set manually when created.
/// </summary>
public partial class ThriveopediaSimpleWikiPage : ThriveopediaWikiPage
{
    public override string? ParentPageName => Parent;

    public string? Parent { get; set; }
}
