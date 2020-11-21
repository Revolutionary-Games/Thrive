using Godot;

/// <summary>
///   Handles triggering quick load whenever the quick load key is pressed
/// </summary>
public class QuickLoadHandler : Node
{
    [Export]
    public NodePath DifferentVersionDialogPath;

    private AcceptDialog differentVersionDialog;

    public QuickLoadHandler()
    {
        InputManager.AddInstance(this);
    }

    public override void _Ready()
    {
        differentVersionDialog = GetNode<AcceptDialog>(DifferentVersionDialogPath);

        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;
    }

    public override void _Notification(int focus)
    {
        // If the window goes out of focus, we don't receive the key released events
        // We reset our held down keys if the player tabs out while pressing a key
        if (focus == MainLoop.NotificationWmFocusOut)
        {
            InputManager.FocusLost();
        }
    }

    [RunOnKeyDown("quick_load")]
    public void OnQuickLoad()
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
