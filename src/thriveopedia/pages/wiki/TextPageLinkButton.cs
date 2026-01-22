using System;
using System.Diagnostics;
using Godot;

/// <summary>
///   Button which links to a Thriveopedia page but uses only text. Added manually.
/// </summary>
/// <seealso cref="IconPageLinkButton"/>
public partial class TextPageLinkButton : Button
{
    [Export]
    public string PageName = null!;

#pragma warning disable CA2213
    [Export]
    public ThriveopediaWikiPage ParentPage = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        ParentPage.Connect(ThriveopediaWikiPage.SignalName.OnStageChanged,
            new Callable(this, nameof(OnSelectedStageChanged)));
    }

    public override void _Pressed()
    {
        ThriveopediaManager.OpenPage(PageName);
    }

    public void OnSelectedStageChanged()
    {
        IThriveopediaPage page;
        try
        {
            page = ThriveopediaManager.GetPage(PageName);
        }
        catch (Exception e)
        {
#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif

            GD.PrintErr($"Failed to get page with name \"{PageName}\" for link button: ", e);
            return;
        }

        if (page is ThriveopediaWikiPage wikiPage)
        {
            Visible = wikiPage.VisibleInTree;
        }
    }
}
