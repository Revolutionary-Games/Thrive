using System.Linq;
using Godot;

/// <summary>
///   Controls the process panel contents
/// </summary>
public class ProcessPanel : WindowDialog
{
    [Export]
    public NodePath ProcessListPath;

    private ProcessList processList;

    public ProcessStatistics ShownData { get; set; }

    public override void _Ready()
    {
        processList = GetNode<ProcessList>(ProcessListPath);
    }

    public override void _Process(float delta)
    {
        if (!IsVisibleInTree())
            return;

        if (ShownData != null)
        {
            // Update the list object
            processList.ProcessesToShow = ShownData.Processes.Select(p => p.Value).Cast<IProcessDisplayInfo>().ToList();
        }
        else
        {
            processList.ProcessesToShow = null;
        }
    }

    private void ClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Visible = false;
    }
}
