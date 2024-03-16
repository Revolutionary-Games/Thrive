using System.Collections.Generic;
using Godot;

/// <summary>
///   A popup that integrates <see cref="RadialMenu"/> into one full package.
/// </summary>
public partial class RadialPopup : CustomWindow
{
    [Export]
    public NodePath? RadialPath;

    [Signal]
    public delegate void OnItemSelectedEventHandler(int itemId);

    public RadialMenu Radial { get; private set; } = null!;

    public override void _Ready()
    {
        Radial = GetNode<RadialMenu>(RadialPath);

        FullRect = true;
        Decorate = false;

        Radial.Connect(RadialMenu.SignalName.OnItemSelected, new Callable(this, nameof(ForwardSelected)));
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
        EmitSignal(CustomWindow.SignalName.Canceled);
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
        EmitSignal(SignalName.OnItemSelected, itemId);
        Hide();
    }
}
