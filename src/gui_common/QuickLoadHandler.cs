using Godot;

/// <summary>
///   Handles triggering quick load whenever the quick load key is pressed
/// </summary>
public partial class QuickLoadHandler : NodeWithInput
{
#pragma warning disable CA2213
    [Export]
    private CustomConfirmationDialog differentVersionDialog = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        // Keep this node running while paused
        ProcessMode = ProcessModeEnum.Always;
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
}
