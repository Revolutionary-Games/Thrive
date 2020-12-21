using Godot;

/// <summary>
///   Shows a list of processes in a container
/// </summary>
public class ProcessList : VBoxContainer
{
    // private List<IProcessDisplayInfo> processesToShow;

    public override void _Ready()
    {
    }

    public override void _Process(float delta)
    {
        if (!IsVisibleInTree())
            return;
    }
}
