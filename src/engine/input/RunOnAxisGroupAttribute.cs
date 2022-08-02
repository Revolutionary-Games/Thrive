using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Combines multiple RunOnAxisAttributes to be able to distinguish between axes
///   Can only be applied once.
/// </summary>
/// <example>
///   [RunOnAxis(new[] { "g_move_forward", "g_move_backwards" }, new[] { -1.0f, 1.0f })]
///   [RunOnAxis(new[] { "g_move_left", "g_move_right" }, new[] { -1.0f, 1.0f })]
///   [RunOnAxisGroup]
///   public void OnMovement(float delta, float forwardBackwardMovement, float leftRightMovement)
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

    public override bool OnInput(InputEvent @event)
    {
        var wasUsed = false;

        foreach (var axis in axes)
        {
            if (axis.OnInput(@event))
                wasUsed = true;
        }

        return wasUsed;
    }

    public override void OnProcess(float delta)
    {
        // Read new axis values
        // TODO: could this run only if OnInput used something?
        int axisCount = axes.Count;

        int wantedLength = axisCount + 1;
        if (callParameters?.Length != wantedLength)
            callParameters = new object[wantedLength];

        for (int i = 0; i < axisCount; ++i)
        {
            var value = axes[i].CurrentResult;
            currentAxisValues[i] = value;

            // This is applied here for more performance as InvokeAlsoWithNoInput seems very common, and assigning
            // two variables in a single loop is hopefully really optimized in the runtime
            callParameters[i + 1] = value;
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

        callParameters[0] = delta;

        CallMethod(callParameters);
    }

    public override void FocusLost()
    {
        axes.ForEach(p => p.FocusLost());
    }

    internal void AddAxis(RunOnAxisAttribute axis)
    {
        axes.Add(axis);
        currentAxisValues.Add(axis.DefaultState);
    }
}
