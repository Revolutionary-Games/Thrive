using Godot;

/// <summary>
///   Handles triggering quick load whenever the quick load key is pressed
/// </summary>
public class QuickLoadHandler : NodeWithInput
{
    [Export]
    public NodePath? DifferentVersionDialogPath = null!;

#pragma warning disable CA2213
    private CustomConfirmationDialog differentVersionDialog = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        differentVersionDialog = GetNode<CustomConfirmationDialog>(DifferentVersionDialogPath);

        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;
    }

    [RunOnKeyDown("quick_load", OnlyUnhandled = false)]
    public void OnQuickLoad()
    {
        if (!InProgressLoad.IsLoading)
        {
            GD.Print("Quick load pressed, attempting to load latest save");

            if (SaveHelper.QuickLoad() == SaveHelper.QuickLoadError.VersionMismatch)
                differentVersionDialog.PopupCenteredShrink();
        }
        else
        {
            GD.Print("Quick load pressed, cancelled because another is already in progress");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DifferentVersionDialogPath?.Dispose();
        }

        base.Dispose(disposing);
    }
}
