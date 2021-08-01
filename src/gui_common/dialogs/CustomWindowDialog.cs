using Godot;

public class CustomWindowDialog : CustomAcceptDialog
{
    public override void _EnterTree()
    {
        // WindowDialog is not regarded as exclusive by default.
        isExclusive = false;
        GetOk().Hide();
        base._EnterTree();
    }
}
