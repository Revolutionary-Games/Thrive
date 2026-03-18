using System.Collections.Generic;
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
    private Control chosenCellBox = null!;

    [Export]
    private Label chosenCellLabel = null!;

    [Export]
    private ProcessList processList = null!;

    [Export]
    private Container closeButtonContainer = null!;

    [Export]
    private Container helpButtonContainer = null!;

    [Export]
    private CustomWindow multicellularProcessPanelExplanation = null!;
#pragma warning restore CA2213

    private bool isMicrobe;

    [Signal]
    public delegate void ToggleProcessPressedEventHandler(ChemicalEquation equation);

    [Signal]
    public delegate void ChoosenCellDeselectedEventHandler();

    public IEnumerable<IProcessDisplayInfo>? ShownData { get; set; }

    public bool IsMicrobe
    {
        get => isMicrobe;

        set
        {
            if (isMicrobe == value)
                return;

            isMicrobe = value;

            UpdateMulticellularStatus();
        }
    }

    public float ExternalSpeedModifier
    {
        get => processList.ExternalSpeedModifier;

        set => processList.ExternalSpeedModifier = value;
    }

    public override void _Ready()
    {
        closeButtonContainer.Visible = ShowCustomCloseButton;

        // To make sure processes refresh when the game is paused
        ProcessMode = ProcessModeEnum.Always;

        UpdateMulticellularStatus();
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
            return;

        processList.ProcessesToShow = ShownData;
    }

    public void OnHelpButtonPressed()
    {
        multicellularProcessPanelExplanation.PopupCenteredShrink();
    }

    public void ReportChosenCell(string chosenCellName)
    {
        UpdateChosenCellDisplay(chosenCellName);
    }

    public void DeselectChosenCell()
    {
        EmitSignal(SignalName.ChoosenCellDeselected);

        UpdateChosenCellDisplay(string.Empty);
    }

    private void UpdateChosenCellDisplay(string chosenCellName)
    {
        if (string.IsNullOrEmpty(chosenCellName))
        {
            // No cell chosen
            chosenCellBox.Visible = false;
            return;
        }

        chosenCellBox.Visible = true;
        chosenCellLabel.Text = Localization.Translate("CURRENTLY_SHOWING_CELL").FormatSafe(chosenCellName);
    }

    private void ToggleProcessToggled(ChemicalEquation equation, bool enabled)
    {
        EmitSignal(SignalName.ToggleProcessPressed, equation, enabled);
    }

    private void UpdateMulticellularStatus()
    {
        helpButtonContainer.Visible = !isMicrobe;

        WindowTitle = isMicrobe ?
            Localization.Translate("PROCESS_PANEL_TITLE_MICROBE") :
            Localization.Translate("PROCESS_PANEL_TITLE");
    }
}
