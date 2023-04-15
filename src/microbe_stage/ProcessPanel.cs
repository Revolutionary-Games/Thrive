using System.Linq;
using Godot;

/// <summary>
///   Controls the process panel contents
/// </summary>
public class ProcessPanel : CustomDialog
{
    [Export]
    public NodePath? ProcessListPath;

    [Export]
    public bool ShowCustomCloseButton;

    [Export]
    public NodePath CloseButtonContainerPath = null!;

#pragma warning disable CA2213
    private ProcessList processList = null!;

    private Container closeButtonContainer = null!;
#pragma warning restore CA2213

    public ProcessStatistics? ShownData { get; set; }

    public override void _Ready()
    {
        processList = GetNode<ProcessList>(ProcessListPath);
        closeButtonContainer = GetNode<Container>(CloseButtonContainerPath);

        closeButtonContainer.Visible = ShowCustomCloseButton;
    }

    public override void _Process(float delta)
    {
        if (!IsVisibleInTree())
            return;

        if (ShownData != null)
        {
            // Update the list object
            processList.ProcessesToShow = ShownData.Processes.Select(p => p.Value.ComputeAverageValues());
        }
        else
        {
            processList.ProcessesToShow = null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ProcessListPath != null)
            {
                ProcessListPath.Dispose();
                CloseButtonContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
