using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Combines multiple <see cref="RunOnAxisAttribute"/>s to be able to distinguish between axes
///   Can only be applied once.
/// </summary>
/// <example>
///   <code>
///     [RunOnAxis(new[] { "g_move_forward", "g_move_backwards" }, new[] { -1.0f, 1.0f })]
///     [RunOnAxis(new[] { "g_move_left", "g_move_right" }, new[] { -1.0f, 1.0f })]
///     [RunOnAxisGroup]
///     public void OnMovement(float delta, float forwardBackwardMovement, float leftRightMovement)
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class RunOnAxisGroupAttribute : InputAttribute
{
    /// <summary>
    ///   All the axes that are managed by this Attribute
    /// </summary>
    private readonly List<RunOnAxisAttribute> axes = new();

    // These two variables exist to make OnProcess more efficient
    private readonly List<float> currentAxisValues = new();

    /// <summary>
    ///   Cached call parameters object to not recreate this all the time.
    ///   This needs to be <code>object[]</code> to match CallMethod's parameter declaration.
    /// </summary>
    private object[]? callParameters;

    /// <summary>
    ///   Should the method be invoked when all of the inputs are in their idle states
    /// </summary>
    public bool InvokeAlsoWithNoInput { get; set; }

    /// <summary>
    ///   If false then the delta is not passed to the called method
    /// </summary>
    public bool InvokeWithDelta { get; set; } = true;

    public override bool OnInput(InputEvent @event)
    {
        var wasUsed = false;

        foreach (var axis in axes)
        {
            if (axis.OnInput(@event))
                wasUsed = true;
        }

        if (wasUsed && TrackInputMethod)
            LastUsedInputMethod = InputManager.InputMethodFromInput(@event);

        return wasUsed;
    }

    public override void OnProcess(double delta)
    {
        // Read new axis values
        // TODO: could this run only if OnInput used something? (currently wouldn't work for mouse look)
        int axisCount = axes.Count;

        int parameterOffset = 0;

        int wantedLength = axisCount;

        if (InvokeWithDelta)
        {
            ++parameterOffset;
            ++wantedLength;
        }

        if (TrackInputMethod)
            ++wantedLength;

        if (callParameters?.Length != wantedLength)
            callParameters = new object[wantedLength];

        var convertedDelta = (float)delta;

        for (int i = 0; i < axisCount; ++i)
        {
            var value = axes[i].GetCurrentResult(convertedDelta);
            currentAxisValues[i] = value;

            // This is applied here for more performance as InvokeAlsoWithNoInput seems very common, and assigning
            // two variables in a single loop is hopefully really optimized in the runtime
            // TODO: try to avoid the boxing here
            callParameters[i + parameterOffset] = value;
        }

        // Skip process if all axes have default values, and invoke also with no input is not set
        if (!InvokeAlsoWithNoInput)
        {
            bool hasDifference = false;

            // TODO: check if combining this into the first loop in this method would be faster
            for (int i = 0; i < axisCount; ++i)
            {
                var difference = currentAxisValues[i] - axes[i].DefaultState;
                if (difference is > 0.001f or < -0.001f)
                {
                    hasDifference = true;
                    break;
                }
            }

            if (!hasDifference)
                return;
        }

        if (InvokeWithDelta)
            callParameters[0] = delta;

        if (TrackInputMethod)
            callParameters[wantedLength - 1] = LastUsedInputMethod;

        CallMethod(callParameters);
    }

    public override void FocusLost()
    {
        axes.ForEach(p => p.FocusLost());
    }

    internal override void OnPostLoad()
    {
        base.OnPostLoad();

        foreach (var axisAttribute in axes)
        {
            axisAttribute.OnPostLoad();
        }
    }

    internal override void OnWindowSizeChanged()
    {
        base.OnWindowSizeChanged();

        foreach (var axisAttribute in axes)
        {
            axisAttribute.OnWindowSizeChanged();
        }
    }

    internal void AddAxis(RunOnAxisAttribute axis)
    {
        axes.Add(axis);
        currentAxisValues.Add(axis.DefaultState);
    }
}
