using Godot;

/// <summary>
///   Shows the microbe movement mode options for the user to select from. Shown at the start of the microbe stage.
/// </summary>
public partial class MovementModeSelectionPopup : Control
{
#pragma warning disable CA2213
    [Export]
    private CustomWindow actualPopup = null!;

    [Export]
    private VideoStreamPlayer cellRelativePlayer = null!;

    [Export]
    private CheckBox cellRelativeCheckBox = null!;

    [Export]
    private VideoStreamPlayer screenRelativePlayer = null!;

    [Export]
    private CheckBox screenRelativeCheckBox = null!;

    [Export]
    private CheckBox dismissPermanentlyCheckBox = null!;

#pragma warning restore CA2213

    private bool makingSelection;

    public override void _Ready()
    {
        base._Ready();

        // For editor usability this is hidden there
        Visible = true;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (makingSelection)
        {
            OnClose();
        }
    }

    public void ShowSelection()
    {
        if (KeyPromptHelper.InputMethod != ActiveInputMethod.Keyboard)
            GD.PrintErr("Doesn't make sense to show movement mode selection popup when not using keyboard input");

        // Apply the current settings state
        switch (Settings.Instance.TwoDimensionalMovement.Value)
        {
            case TwoDimensionalMovementMode.Automatic:
            case TwoDimensionalMovementMode.PlayerRelative:
                cellRelativeCheckBox.ButtonPressed = true;
                screenRelativeCheckBox.ButtonPressed = false;
                break;

            case TwoDimensionalMovementMode.ScreenRelative:
                cellRelativeCheckBox.ButtonPressed = false;
                screenRelativeCheckBox.ButtonPressed = true;
                break;

            default:
                GD.PrintErr("Unknown movement mode: ", Settings.Instance.TwoDimensionalMovement.Value);
                goto case TwoDimensionalMovementMode.Automatic;
        }

        actualPopup.PopupCenteredShrink();

        if (!makingSelection)
        {
            makingSelection = true;

            PauseManager.Instance.AddPause(nameof(MovementModeSelectionPopup));

            // Start the video players
            cellRelativePlayer.Stream =
                GD.Load<VideoStream>("res://assets/videos/movementDemonstrationCellRelative.ogv");
            cellRelativePlayer.Play();
            screenRelativePlayer.Stream =
                GD.Load<VideoStream>("res://assets/videos/movementDemonstrationScreenRelative.ogv");
            screenRelativePlayer.Play();
        }
    }

    private void OnAccepted()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var settings = Settings.Instance;

        // Apply the desired movement mode
        if (cellRelativeCheckBox.ButtonPressed)
        {
            if (settings.TwoDimensionalMovement.Value == TwoDimensionalMovementMode.ScreenRelative)
            {
                settings.TwoDimensionalMovement.Value = TwoDimensionalMovementMode.Automatic;
            }
        }
        else if (screenRelativeCheckBox.ButtonPressed)
        {
            if (settings.TwoDimensionalMovement.Value != TwoDimensionalMovementMode.ScreenRelative)
                settings.TwoDimensionalMovement.Value = TwoDimensionalMovementMode.ScreenRelative;
        }
        else
        {
            GD.PrintErr("No movement mode selected");
        }

        if (dismissPermanentlyCheckBox.ButtonPressed)
        {
            // This already saves so only the other branch uses an explicit save
            settings.PermanentlyDismissNotice(DismissibleNotice.MicrobeMovementMode);
        }
        else
        {
            if (!settings.Save())
                GD.PrintErr("Failed to save settings for selected input mode");
        }

        OnClose();
        actualPopup.Close();
    }

    private void LeftSelectionChanged(bool pressed)
    {
        cellRelativeCheckBox.ButtonPressed = pressed;
        screenRelativeCheckBox.ButtonPressed = !pressed;
    }

    private void RightSelectionChanged(bool pressed)
    {
        cellRelativeCheckBox.ButtonPressed = !pressed;
        screenRelativeCheckBox.ButtonPressed = pressed;
    }

    private void LeftSideGUIInput(InputEvent @event)
    {
        // If it is a mouse click, consider it to have selected that side
        if (@event is InputEventMouseButton { Pressed: true })
        {
            LeftSelectionChanged(true);
            GUICommon.Instance.PlayButtonPressSound();
        }
    }

    private void RightSideGUIInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true })
        {
            RightSelectionChanged(true);
            GUICommon.Instance.PlayButtonPressSound();
        }
    }

    private void OnClose()
    {
        cellRelativePlayer.Stop();
        cellRelativePlayer.Stream = null;
        screenRelativePlayer.Stop();
        screenRelativePlayer.Stream = null;

        if (makingSelection)
        {
            PauseManager.Instance.Resume(nameof(MovementModeSelectionPopup));

            makingSelection = false;
        }
    }
}
