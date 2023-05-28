using System.Collections.Generic;
using Godot;

public class RadialPopup : CustomWindow
{
    [Export]
    public NodePath? RadialPath;

    [Signal]
    public delegate void OnItemSelected(int itemId);

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
        Open();
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

    protected override void OnHidden()
    {
        base.OnHidden();
        EmitSignal(nameof(Cancelled));
        Radial.Visible = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RadialPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ForwardSelected(int itemId)
    {
        EmitSignal(nameof(OnItemSelected), itemId);
        Hide();
    }
}
