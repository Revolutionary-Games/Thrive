using Godot;

/// <summary>
///   Thriveopedia page displaying welcome information and links to websites.
/// </summary>
public class ThriveopediaHomePage : ThriveopediaPage
{
    [Export]
    public NodePath? ContentPath;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private CustomRichTextLabel content = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

    public override string PageName => "Home";
    public override string TranslatedPageName => TranslationServer.Translate("THRIVEOPEDIA_HOME_PAGE_TITLE");

    public override void _Ready()
    {
        base._Ready();

        content = GetNode<CustomRichTextLabel>(ContentPath);
    }

    public override void OnThriveopediaOpened()
    {
    }

    public override void UpdateCurrentWorldDetails()
    {
    }

    public override void OnNavigationPanelSizeChanged(bool collapsed)
    {
    }

    public override void OnTranslationChanged()
    {
        content.ExtendedBbcode = TranslationServer.Translate("THRIVEOPEDIA_HOME_INFO");
    }
}
