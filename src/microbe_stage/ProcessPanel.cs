using System.Linq;
using Godot;

/// <summary>
///   Controls the process panel contents
/// </summary>
public class ProcessPanel : WindowDialog
{
    [Export]
    public NodePath ProcessListPath;

    [Export]
    public bool ShowCloseButton;

    [Export]
    public NodePath CloseButtonContainerPath;

    private ProcessList processList;

    private Container closeButtonContainer;

    [Signal]
    public delegate void OnClosed();

    public ProcessStatistics ShownData { get; set; }

    public override void _Ready()
    {
        processList = GetNode<ProcessList>(ProcessListPath);
        closeButtonContainer = GetNode<Container>(CloseButtonContainerPath);

        closeButtonContainer.Visible = ShowCloseButton;
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
        Visible = false;
    }

    private void OnHidden()
    {
        EmitSignal(nameof(OnClosed));
    }
}
