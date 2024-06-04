using Godot;

public partial class MechanicLinkButton : Button
{
    [Export]
    public string PageName = null!;

    public override void _Pressed()
    {
        ThriveopediaManager.OpenPage(PageName);
    }
}
