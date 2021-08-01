using System;
using Godot;

public class CustomWindowDialog : CustomAcceptDialog
{
    public override void _EnterTree()
    {
        GetOk().Hide();
        RectSize = new Vector2(RectSize.x, RectSize.y - 10);
        base._EnterTree();
    }
}
