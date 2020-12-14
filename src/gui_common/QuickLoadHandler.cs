using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles triggering quick load whenever the quick load key is pressed
/// </summary>
public class QuickLoadHandler : Node
{
    [Export]
    public NodePath DifferentVersionDialogPath;

    private readonly InputGroup inputs;

    private readonly InputTrigger load = new InputTrigger("quick_load");

    private AcceptDialog differentVersionDialog;

    public QuickLoadHandler()
    {
        inputs = new InputGroup(new List<IInputReceiver> { load });
    }

    public override void _Ready()
    {
        differentVersionDialog = GetNode<AcceptDialog>(DifferentVersionDialogPath);

        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;
    }

    public override void _Input(InputEvent @event)
    {
        if (inputs.CheckInput(@event))
        {
            GetTree().SetInputAsHandled();
        }
    }

    public override void _Notification(int focus)
    {
        // If the window goes out of focus, we don't receive the key released events
        // We reset our held down keys if the player tabs out while pressing a key
        if (focus == MainLoop.NotificationWmFocusOut)
        {
            inputs.FocusLost();
        }
    }

    public override void _Process(float delta)
    {
        inputs.OnFrameChanged();

        if (load.ReadTrigger())
        {
            if (!InProgressLoad.IsLoading)
            {
                GD.Print("Quick load pressed, attempting to load latest save");

                if (!SaveHelper.QuickLoad())
                    differentVersionDialog.PopupCenteredMinsize();
            }
            else
            {
                GD.Print("Quick load pressed, cancelled because another is already in progress");
            }
        }
    }
}
