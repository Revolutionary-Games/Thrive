using System;

/// <summary>
///   Admits main-thread resource loading work within a single frame.
/// </summary>
internal struct ResourceLoadFrameBudget
{
    private const double MINIMUM_AVAILABLE_TIME_FRACTION = 0.05;
    private const double MINIMUM_CARRY_FRAMES = -2.0;
    private const double MAXIMUM_CARRY_FRAMES = 0.5;

    private readonly double originalBudgetSeconds;
    private readonly double targetFrameTimeSeconds;

    private bool hasExecutedCompletionUnit;

    internal ResourceLoadFrameBudget(double elapsedFrameTimeSeconds, double savedProcessingTimeSeconds,
        double targetFrameTimeSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedFrameTimeSeconds);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(targetFrameTimeSeconds, 0);

        DoubleFiniteCheck(elapsedFrameTimeSeconds, nameof(elapsedFrameTimeSeconds));
        DoubleFiniteCheck(savedProcessingTimeSeconds, nameof(savedProcessingTimeSeconds));
        DoubleFiniteCheck(targetFrameTimeSeconds, nameof(targetFrameTimeSeconds));

        originalBudgetSeconds = Math.Max(targetFrameTimeSeconds - elapsedFrameTimeSeconds,
            targetFrameTimeSeconds * MINIMUM_AVAILABLE_TIME_FRACTION) + savedProcessingTimeSeconds;
        this.targetFrameTimeSeconds = targetFrameTimeSeconds;
        hasExecutedCompletionUnit = false;
    }

    /// <summary>
    ///   Gets the number of seconds in the frame budget that have not yet elapsed.
    /// </summary>
    internal double GetRemainingSeconds(double elapsedSeconds)
    {
        return originalBudgetSeconds - elapsedSeconds;
    }

    /// <summary>
    ///   Attempts to admit one indivisible main-thread completion unit.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Work that fits in the remaining budget is admitted normally. To ensure progress, the first main-thread
    ///     completion unit in a frame may exceed the remaining positive budget. Scheduling work performed before this
    ///     call does not consume that forced-admission opportunity, though its elapsed time still reduces the remaining
    ///     budget.
    ///   </para>
    /// </remarks>
    internal bool TryAdmit(double estimatedDurationSeconds, double elapsedSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(estimatedDurationSeconds);

        DoubleFiniteCheck(estimatedDurationSeconds, nameof(estimatedDurationSeconds));

        var remainingSeconds = GetRemainingSeconds(elapsedSeconds);

        if (remainingSeconds <= 0)
            return false;

        if (estimatedDurationSeconds > remainingSeconds && hasExecutedCompletionUnit)
            return false;

        hasExecutedCompletionUnit = true;
        return true;
    }

    /// <summary>
    ///   Calculates the number of processing seconds carried into the next frame from the actual elapsed time.
    /// </summary>
    internal double CalculateSecondsToCarry(double elapsedSeconds)
    {
        return Math.Clamp(GetRemainingSeconds(elapsedSeconds), targetFrameTimeSeconds * MINIMUM_CARRY_FRAMES,
            targetFrameTimeSeconds * MAXIMUM_CARRY_FRAMES);
    }

    private static void DoubleFiniteCheck(double value, string parameterName)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
