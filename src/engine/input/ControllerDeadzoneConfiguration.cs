using System.Collections.Generic;
using Godot;

public class ControllerDeadzoneConfiguration : CustomDialog
{
    [Export]
    public NodePath VisualizationContainerPath = null!;

    [Export]
    public NodePath StartButtonPath = null!;

    private ControllerInputAxisVisualizationContainer visualizationContainer = null!;

    private Button startButton = null!;

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
    }

    public override void _Process(float delta)
    {
        if (!Visible)
            return;
    }

    private void OnBecomeVisible()
    {
        visualizationContainer.Start();

        // As we modify the deadzones we need to make a deep copy here
        currentDeadzones = new List<float>(Settings.Instance.ControllerAxisDeadzoneAxes.Value);

        // Tweak to the right number of deadzones
        while (currentDeadzones.Count > (int)JoystickList.AxisMax)
        {
            currentDeadzones.RemoveAt(currentDeadzones.Count - 1);
        }

        while (currentDeadzones.Count < (int)JoystickList.AxisMax)
        {
            currentDeadzones.Add(currentDeadzones.Count > 0 ?
                currentDeadzones[0] :
                Constants.CONTROLLER_DEFAULT_DEADZONE);
        }

        startButton.GrabFocus();
    }

    private void OnCancel()
    {
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
    }
}
