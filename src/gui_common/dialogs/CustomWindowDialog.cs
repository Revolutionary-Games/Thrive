public class CustomWindowDialog : CustomAcceptDialog
{
    public override void _EnterTree()
    {
        GetOk().Hide();
        base._EnterTree();
    }
}
