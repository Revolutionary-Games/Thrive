using System.Collections.Generic;
using Godot;

public class RadialPopup : CustomDialog
{
    [Export]
    public NodePath RadialPath = null!;

    [Signal]
    public delegate void OnItemSelected(int itemId);

    [Signal]
    public delegate void OnCanceled(int itemId);

    public RadialMenu Radial { get; private set; } = null!;

    public override void _Ready()
    {
        Radial = GetNode<RadialMenu>(RadialPath);

        FullRect = true;
        Decorate = false;

        Radial.Connect(nameof(RadialMenu.OnItemSelected), this, nameof(ForwardSelected));
        Radial.Visible = false;
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        InputManager.RegisterReceiver(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        InputManager.UnregisterReceiver(this);
    }

    public void ShowWithItems(IEnumerable<(string Text, int Id)> items)
    {
        Popup_();
        Radial.ShowWithItems(items);
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.SUBMENU_CANCEL_PRIORITY)]
    public bool RadialPopupCanceled()
    {
        if (!Visible)
            return false;

        Hide();
        return true;
    }

    private void ForwardSelected(int itemId)
    {
        EmitSignal(nameof(OnItemSelected), itemId);
        Hide();
    }

    private void OnClosed()
    {
        EmitSignal(nameof(OnCanceled));
        Radial.Visible = false;
    }
}
