using Godot;

/// <summary>
///   Handles triggering quick load whenever the quick load key is pressed
/// </summary>
public class QuickLoadHandler : Node
{
    [Export]
    public NodePath DifferentVersionDialogPath;

    private AcceptDialog differentVersionDialog;

    public override void _Ready()
    {
        differentVersionDialog = GetNode<AcceptDialog>(DifferentVersionDialogPath);

        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("quick_load"))
        {
            if (!InProgressLoad.isLoading)
            {
                GD.Print("Quick load pressed, attempting to load latest save");
                SaveHelper.QuickLoad();
            }
            else
            {
                GD.Print("Quick load pressed, cancelled because another is already in progress");
            }

            if (!SaveHelper.QuickLoad())
                differentVersionDialog.PopupCenteredMinsize();
        }
    }
}
