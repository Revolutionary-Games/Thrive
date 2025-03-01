﻿using System;
using Godot;

/// <summary>
///   Displays additional tolerance slider info, such as the optimal value and value labels.
/// </summary>
public partial class ToleranceInfo : Control
{
#pragma warning disable CA2213
    [Export]
    private Label startValue = null!;

    [Export]
    private Label endValue = null!;

    [Export]
    private Control optimalValueMarker = null!;
#pragma warning restore CA2213

    public void UpdateInfo(float start, float end, float optimalValue)
    {
        optimalValueMarker.AnchorLeft = optimalValue;
        optimalValueMarker.AnchorRight = optimalValue;

        startValue.Text = start.ToString();
        endValue.Text = end.ToString();
    }
}
