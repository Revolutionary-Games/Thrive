﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

/// <summary>
///   Visualizes controller axis states. Doesn't listen to input so needs something like
///   <see cref="ControllerInputAxisVisualizationContainer"/> to tell this what to show.
/// </summary>
public class ControllerAxisVisualizer : MarginContainer
{
    [Export]
    public int DecimalsToDisplay = 4;

    [Export]
    public float CrossSize = 8;

    [Export]
    public float CrossLineWidth = 2;

    [Export]
    public NodePath DrawerNodePath = null!;

    [Export]
    public NodePath HorizontalLabelPath = null!;

    [Export]
    public NodePath HorizontalRawValuePath = null!;

    [Export]
    public NodePath HorizontalDeadzoneValuePath = null!;

    [Export]
    public NodePath VerticalLabelPath = null!;

    [Export]
    public NodePath VerticalRawValuePath = null!;

    [Export]
    public NodePath VerticalDeadzoneValuePath = null!;

    // The following 2 variables are for hiding the second axis when not configured
    [Export]
    public NodePath VerticalRawDisplayerPath = null!;

    [Export]
    public NodePath VerticalDeadzoneDisplayerPath = null!;

    private Control drawerNode = null!;
    private Label horizontalLabel = null!;
    private Label horizontalRawValue = null!;
    private Label horizontalDeadzoneValue = null!;
    private Label verticalLabel = null!;
    private Label verticalRawValue = null!;
    private Label verticalDeadzoneValue = null!;
    private Control verticalRawDisplayer = null!;
    private Control verticalDeadzoneDisplayer = null!;

    private int horizontalAxis = -1;
    private float horizontalValue;
    private float horizontalDeadzone;

    private int verticalAxis = -1;
    private float verticalValue;
    private float verticalDeadzone;

    public bool HasSecondAxis => verticalAxis != -1;

    public override void _Ready()
    {
        drawerNode = GetNode<Control>(DrawerNodePath);
        horizontalLabel = GetNode<Label>(HorizontalLabelPath);
        horizontalRawValue = GetNode<Label>(HorizontalRawValuePath);
        horizontalDeadzoneValue = GetNode<Label>(HorizontalDeadzoneValuePath);
        verticalLabel = GetNode<Label>(VerticalLabelPath);
        verticalRawValue = GetNode<Label>(VerticalRawValuePath);
        verticalDeadzoneValue = GetNode<Label>(VerticalDeadzoneValuePath);
        verticalRawDisplayer = GetNode<Control>(VerticalRawDisplayerPath);
        verticalDeadzoneDisplayer = GetNode<Control>(VerticalDeadzoneDisplayerPath);

        drawerNode.Connect("draw", this, nameof(Draw));

        SetVerticalAxisDisplay(false);
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Settings.Instance.ControllerAxisDeadzoneAxes.OnChanged += OnDeadzoneSettingsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Settings.Instance.ControllerAxisDeadzoneAxes.OnChanged -= OnDeadzoneSettingsChanged;
    }

    /// <summary>
    ///   Updates the horizontal axis display
    /// </summary>
    /// <param name="value">The axis value to show</param>
    public void SetHorizontalAxisValue(float value)
    {
        horizontalValue = value;

        drawerNode.Update();
        UpdateAxisValueLabel(horizontalRawValue, value);
    }

    /// <summary>
    ///   Updates the vertical axis display. May only be called if a second axis is configured with
    ///   <see cref="AddAxis"/>
    /// </summary>
    /// <exception cref="InvalidOperationException">If the vertical axis is not configured</exception>
    public void SetVerticalAxisValue(float value)
    {
        if (!HasSecondAxis)
            throw new InvalidOperationException("Second axis is not configured");

        verticalValue = value;

        drawerNode.Update();
        UpdateAxisValueLabel(verticalRawValue, value);
    }

    /// <summary>
    ///   Automatically determines which axis value to update
    /// </summary>
    /// <exception cref="ArgumentException">If the axis is unknown</exception>
    public void SetAxisValue(int axis, float value)
    {
        if (axis < 0)
            throw new ArgumentOutOfRangeException(nameof(axis), "Axis can't be negative");

        if (horizontalAxis == axis)
        {
            SetHorizontalAxisValue(value);
        }
        else if (verticalAxis == axis)
        {
            SetVerticalAxisValue(value);
        }
        else
        {
            throw new ArgumentException("Unknown axis");
        }
    }

    /// <summary>
    ///  Adds a new axis to track. Each instance can only track 2 axes.
    /// </summary>
    /// <param name="axis">The axis ID</param>
    public void AddAxis(int axis)
    {
        if (horizontalAxis == -1)
        {
            horizontalAxis = axis;
            horizontalLabel.Text = TranslationServer.Translate("HORIZONTAL_WITH_AXIS_NAME_COLON").FormatSafe(axis);
            ReadDeadzone(horizontalDeadzoneValue, horizontalAxis, Settings.Instance.ControllerAxisDeadzoneAxes);
        }
        else if (!HasSecondAxis)
        {
            verticalAxis = axis;
            verticalLabel.Text = TranslationServer.Translate("VERTICAL_WITH_AXIS_NAME_COLON").FormatSafe(axis);
            SetVerticalAxisDisplay(true);
            ReadDeadzone(verticalDeadzoneValue, verticalAxis, Settings.Instance.ControllerAxisDeadzoneAxes);
        }
        else
        {
            throw new InvalidOperationException("This instance already has 2 axes");
        }
    }

    private void Draw()
    {
        var size = drawerNode.RectSize;
        var center = size / 2;

        var circleSize = size.y / 2;

        drawerNode.DrawCircle(center, circleSize, Colors.Gray);
        drawerNode.DrawArc(center, circleSize, 0, (float)MathUtils.FULL_CIRCLE, 128, Colors.Black, 1, true);

        var adjustedHorizontal = horizontalValue;
        if (Math.Abs(adjustedHorizontal) < horizontalDeadzone)
            adjustedHorizontal = 0;

        var adjustedVertical = verticalValue;
        if (Math.Abs(adjustedVertical) < verticalDeadzone)
            adjustedVertical = 0;

        var crossPosition = center + circleSize * new Vector2(adjustedHorizontal, adjustedVertical);

        // Horizontal line for the axis value cross
        drawerNode.DrawLine(crossPosition - new Vector2(CrossSize, 0), crossPosition + new Vector2(CrossSize, 0),
            Colors.WhiteSmoke, CrossLineWidth);

        // Vertical
        drawerNode.DrawLine(crossPosition - new Vector2(0, CrossSize), crossPosition + new Vector2(0, CrossSize),
            Colors.WhiteSmoke, CrossLineWidth);
    }

    private void SetVerticalAxisDisplay(bool visible)
    {
        verticalLabel.Visible = visible;
        verticalRawDisplayer.Visible = visible;
        verticalDeadzoneDisplayer.Visible = visible;
    }

    private void OnDeadzoneSettingsChanged(List<float> newValue)
    {
        if (horizontalAxis != -1)
        {
            ReadDeadzone(horizontalDeadzoneValue, horizontalAxis, newValue);
        }

        if (HasSecondAxis)
        {
            ReadDeadzone(verticalDeadzoneValue, verticalAxis, newValue);
        }
    }

    private void ReadDeadzone(Label valueLabel, int axis, List<float> axisDeadzones)
    {
        var index = axis - (int)JoystickList.Axis0;

        float deadzone = 0;

        if (index < axisDeadzones.Count)
        {
            deadzone = axisDeadzones[index];
        }

        if (axis == horizontalAxis)
        {
            horizontalDeadzone = deadzone;
        }
        else if (axis == verticalAxis)
        {
            verticalDeadzone = deadzone;
        }

        valueLabel.Text = Math.Round(deadzone, DecimalsToDisplay).ToString(CultureInfo.CurrentCulture);
    }

    private void UpdateAxisValueLabel(Label valueLabel, float value)
    {
        valueLabel.Text = Math.Round(value, DecimalsToDisplay).ToString(CultureInfo.CurrentCulture);
    }
}
