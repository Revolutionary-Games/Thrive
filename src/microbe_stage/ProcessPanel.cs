using System.Linq;
using Godot;

/// <summary>
///   Controls the process panel contents
/// </summary>
public class ProcessPanel : CustomDialog
{
    [Export]
    public NodePath ProcessListPath = null!;

    [Export]
    public bool ShowCustomCloseButton;

    [Export]
    public NodePath CloseButtonContainerPath = null!;

    private ProcessList processList = null!;

    private Container closeButtonContainer = null!;

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
            processList.ProcessesToShow = ShownData.Processes.Select(p => p.Value.ComputeAverageValues()).ToList();
        }
        else
        {
            processList.ProcessesToShow = null;
        }
    }

    private void ClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Hide();
    }
}
