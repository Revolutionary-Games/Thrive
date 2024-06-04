using Godot;

/// <summary>
///   Simple way to link mechanic pages from the mechanics root page
/// </summary>
public partial class MechanicLinkButton : Button
{
    [Export]
    public string PageName = null!;

    public override void _Pressed()
    {
        ThriveopediaManager.OpenPage(PageName);
    }
}
