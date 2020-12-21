using System.Collections.Generic;
using Godot;

/// <summary>
///   Shows a list of processes in a container
/// </summary>
public class ProcessList : VBoxContainer
{
    public List<IProcessDisplayInfo> ProcessesToShow { get; set; }

    public override void _Ready()
    {
    }

    public override void _Process(float delta)
    {
        if (!IsVisibleInTree())
            return;

        if (ProcessesToShow == null)
        {
            ClearChildren();
            return;
        }

        // Check that all children are up to date

        // TODO: debug test code
        ClearChildren();
        foreach (var process in ProcessesToShow)
        {
            var child = new Label();
            child.Text = process.Name;
            AddChild(child);
        }
    }

    private void ClearChildren()
    {
        this.FreeChildren();
    }
}
