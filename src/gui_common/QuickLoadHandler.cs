using Godot;

/// <summary>
///   Handles triggering quick load whenever the quick load key is pressed
/// </summary>
public class QuickLoadHandler : Node
{
    [RunOnKey("quick_load", RunOnKeyAttribute.InputType.Press)]
    public static void QuickLoad()
    {
        GD.Print("Quick load pressed, attempting to load latest save");
        SaveHelper.QuickLoad();
    }

    public override void _Ready()
    {
        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;
    }
}
