using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Popup that has controller deadzone configuration options for the player to use
/// </summary>
public partial class ControllerDeadzoneConfiguration : CustomWindow
{
    [Export]
    public NodePath? VisualizationContainerPath;

    [Export]
    public NodePath StartButtonPath = null!;

    [Export]
    public NodePath ApplyButtonPath = null!;

    [Export]
    public NodePath StatusLabelPath = null!;

    [Export]
    public NodePath ExplanationLabelPath = null!;

    private const double SettleDownTimeStart = 4.5f;
    private const double SettleDownTimeIncreaseMultiplier = 5;

#pragma warning disable CA2213
    private ControllerInputAxisVisualizationContainer visualizationContainer = null!;

    private Button startButton = null!;
    private Button applyButton = null!;

    private Label statusLabel = null!;

#pragma warning restore CA2213

    private bool calibrating;
    private double timeRemaining;
    private List<float>? currentDeadzones;

    public delegate void ControlsChangedDelegate(List<float> data);

    /// <summary>
    ///   Fired whenever deadzone configuration is confirmed
    /// </summary>
    public event ControlsChangedDelegate? OnDeadzonesConfirmed;

    public override void _Ready()
    {
        visualizationContainer = GetNode<ControllerInputAxisVisualizationContainer>(VisualizationContainerPath);

        startButton = GetNode<Button>(StartButtonPath);
        applyButton = GetNode<Button>(ApplyButtonPath);

        statusLabel = GetNode<Label>(StatusLabelPath);

        GetNode<Label>(ExplanationLabelPath).RegisterCustomFocusDrawer();

        // TODO: add separate sliders in this GUI to manually tweak all deadzones
    }

    public override void _Process(double delta)
    {
        if (!Visible || !calibrating)
            return;

        timeRemaining -= delta;

        if (timeRemaining < 0)
        {
            // Operation finished
            OnFinishedCalibrating();
        }
        else
        {
            // Check axis values, increase timeRemaining based on how much the values have changed
            foreach (var (axis, value) in visualizationContainer.GetAllAxisValues())
            {
                if (axis >= currentDeadzones!.Count)
                {
                    GD.PrintErr("Received value for axis that is out of range: ", axis);
                    continue;
                }

                var absoluteValue = Math.Abs(value);
                if (absoluteValue < currentDeadzones[axis])
                    continue;

                // A value is no longer within the deadzone, increase deadzone and increase time remaining
                currentDeadzones[axis] = absoluteValue * (1 + Constants.CONTROLLER_DEADZONE_CALIBRATION_MARGIN)
                    + Constants.CONTROLLER_DEADZONE_CALIBRATION_MARGIN_CONSTANT;
                timeRemaining += absoluteValue * SettleDownTimeIncreaseMultiplier;
            }
        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        statusLabel.Text = string.Empty;
        visualizationContainer.Start();

        // As we modify the deadzones we need to make a deep copy here
        currentDeadzones = new List<float>(Settings.Instance.ControllerAxisDeadzoneAxes.Value);

        // Tweak to the right number of deadzones
        while (currentDeadzones.Count > (int)JoyAxis.Max)
        {
            currentDeadzones.RemoveAt(currentDeadzones.Count - 1);
        }

        while (currentDeadzones.Count < (int)JoyAxis.Max)
        {
            currentDeadzones.Add(currentDeadzones.Count > 0 ?
                currentDeadzones[0] :
                Constants.CONTROLLER_DEFAULT_DEADZONE);
        }

        // For some reason a label grabs focus if we don't call this with a delay
        startButton.GrabFocusInOpeningPopup();
    }

    protected override void OnHidden()
    {
        base.OnHidden();
        OnCancel();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (VisualizationContainerPath != null)
            {
                VisualizationContainerPath.Dispose();
                StartButtonPath.Dispose();
                ApplyButtonPath.Dispose();
                StatusLabelPath.Dispose();
                ExplanationLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnCancel()
    {
        // TODO: this should also reset the text / timers so that reopening the configuration quickly works correctly

        visualizationContainer.Stop();
    }

    private void OnConfirmed()
    {
        if (currentDeadzones == null)
        {
            GD.PrintErr("Deadzones not set");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        OnDeadzonesConfirmed?.Invoke(currentDeadzones);
        Hide();
    }

    private void OnStart()
    {
        if (currentDeadzones == null)
        {
            GD.PrintErr("Deadzones not set");
            return;
        }

        timeRemaining = SettleDownTimeStart;

        // Deadzones start at 0 and get increased as they are moved
        for (int i = 0; i < currentDeadzones.Count; ++i)
        {
            currentDeadzones[i] = 0;
        }

        statusLabel.Text = Localization.Translate("DEADZONE_CALIBRATION_INPROGRESS");
        calibrating = true;
        applyButton.Disabled = true;
        startButton.Disabled = true;
    }

    private void ResetDeadzones()
    {
        // Cancel calibrating if currently calibrating
        if (calibrating)
        {
            OnFinishedCalibrating();
        }

        for (int i = 0; i < currentDeadzones!.Count; ++i)
        {
            currentDeadzones[i] = Constants.CONTROLLER_DEFAULT_DEADZONE;
        }

        visualizationContainer.OverrideDeadzones(currentDeadzones);

        statusLabel.Text = Localization.Translate("DEADZONE_CALIBRATION_IS_RESET");
    }

    private void OnFinishedCalibrating()
    {
        // Any axes that didn't get any values, reset to default
        for (int i = 0; i < currentDeadzones!.Count; ++i)
        {
            if (currentDeadzones[i] < MathUtils.EPSILON)
                currentDeadzones[i] = Constants.CONTROLLER_DEFAULT_DEADZONE;
        }

        visualizationContainer.OverrideDeadzones(currentDeadzones);

        statusLabel.Text = Localization.Translate("DEADZONE_CALIBRATION_FINISHED");
        calibrating = false;
        applyButton.Disabled = false;
        startButton.Disabled = false;
    }
}
