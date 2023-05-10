using Godot;

/// <summary>
///   Display for an item in a build queue
/// </summary>
public class BuildQueueItem : VBoxContainer
{
    public override void _Ready()
    {
        // TODO: a cancel button?
    }

    public override void _Process(float delta)
    {
    }

    public void Display(IBuildQueueProgressItem buildQueueItemData)
    {
        throw new System.NotImplementedException();
    }
}
