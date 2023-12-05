public class SimpleWikiPage : ThriveopediaWikiPage
{
    public override string? ParentPageName => Parent;

    public string? Parent { get; set; }
}