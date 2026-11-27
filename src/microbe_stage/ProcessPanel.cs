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
    private CustomWindow processPanelExplanation = null!;

    [Export]
    private Label multicellularExplanationLabel = null!;
#pragma warning restore CA2213

    private bool isMicrobe;

    [Signal]
    public delegate void ToggleProcessPressedEventHandler(ChemicalEquation equation);

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
        processPanelExplanation.PopupCenteredShrink();
    }

    private void ToggleProcessToggled(ChemicalEquation equation, bool enabled)
    {
        EmitSignal(SignalName.ToggleProcessPressed, equation, enabled);
    }

    private void UpdateMulticellularStatus()
    {
        multicellularExplanationLabel.Visible = !isMicrobe;

        WindowTitle = isMicrobe ?
            Localization.Translate("PROCESS_PANEL_TITLE_MICROBE") :
            Localization.Translate("PROCESS_PANEL_TITLE");
    }
}
