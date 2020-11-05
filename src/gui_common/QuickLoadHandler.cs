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
        RunOnInputAttribute.InputClasses.Add(this);
    }

    [RunOnKey("quick_load", RunOnKeyAttribute.InputType.Press)]
    public void QuickLoad()
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

    public override void _Ready()
    {
        differentVersionDialog = GetNode<AcceptDialog>(DifferentVersionDialogPath);

        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;
    }
}
