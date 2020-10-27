using Godot;

/// <summary>
///   Handles triggering quick load whenever the quick load key is pressed
/// </summary>
public class QuickLoadHandler : Node
{
    public override void _Ready()
    {
        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("quick_load"))
        {
            if (!InProgressLoad.CheckIsLoading())
            {
                GD.Print("Quick load pressed, attempting to load latest save");
                SaveHelper.QuickLoad();
            }
        }
    }
}
