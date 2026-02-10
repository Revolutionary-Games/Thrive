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
    private ProcessList processList = null!;

    [Export]
    private Container closeButtonContainer = null!;

    [Export]
    private Container helpButtonContainer = null!;

    [Export]
    private CustomWindow multicellularProcessPanelExplanation = null!;
#pragma warning restore CA2213

    private bool isMulticellular = false;

    [Signal]
    public delegate void ToggleProcessPressedEventHandler(ChemicalEquation equation);

    public IEnumerable<IProcessDisplayInfo>? ShownData { get; set; }

    public bool IsMulticellular
    {
        get => isMulticellular;

        set
        {
            if (isMulticellular == value)
                return;

            isMulticellular = value;

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

    private void ToggleProcessToggled(ChemicalEquation equation, bool enabled)
    {
        EmitSignal(SignalName.ToggleProcessPressed, equation, enabled);
    }

    private void UpdateMulticellularStatus()
    {
        helpButtonContainer.Visible = isMulticellular;

        WindowTitle = isMulticellular ? Localization.Translate("PROCESS_PANEL_TITLE_MULTICELLULAR")
            : Localization.Translate("PROCESS_PANEL_TITLE");
    }
}
