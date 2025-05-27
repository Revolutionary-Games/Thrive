using System.Linq;
using Godot;

/// <summary>
///   Controls the process panel contents
/// </summary>
public partial class ProcessPanel : CustomWindow
{
    [Export]
    public bool ShowCustomCloseButton;

#pragma warning disable CA2213
    [Export]
    private ProcessList processList = null!;

    [Export]
    private Container closeButtonContainer = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void ToggleProcessPressedEventHandler(ChemicalEquation equation);

    public ProcessStatistics? ShownData { get; set; }

    public float ExternalSpeedModifier
    {
        get => processList.ExternalSpeedModifier;

        set => processList.ExternalSpeedModifier = value;
    }

    public override void _Ready()
    {
        closeButtonContainer.Visible = ShowCustomCloseButton;
    }

    public override void _Process(double delta)
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

    private void ToggleProcessToggled(ChemicalEquation equation, bool enabled)
    {
        EmitSignal(SignalName.ToggleProcessPressed, equation, enabled);
    }
}
