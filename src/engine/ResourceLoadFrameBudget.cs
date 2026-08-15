using System;
using System.Diagnostics;

/// <summary>
///   Admits and executes main-thread resource loading completion units within a single frame.
/// </summary>
internal struct ResourceLoadFrameBudget
{
    private const double MINIMUM_AVAILABLE_TIME_FRACTION = 0.05;
    private const double MINIMUM_CARRY_FRAMES = -2.0;
    private const double MAXIMUM_CARRY_FRAMES = 0.5;

    private readonly double originalBudgetSeconds;
    private readonly double targetFrameTimeSeconds;

    private bool hasExecutedCompletionUnit;

    public ResourceLoadFrameBudget(double elapsedFrameTimeSeconds, double savedProcessingTimeSeconds,
        double targetFrameTimeSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedFrameTimeSeconds);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(targetFrameTimeSeconds, 0);

        if (!double.IsFinite(elapsedFrameTimeSeconds))
            throw new ArgumentOutOfRangeException(nameof(elapsedFrameTimeSeconds));

        if (!double.IsFinite(savedProcessingTimeSeconds))
            throw new ArgumentOutOfRangeException(nameof(savedProcessingTimeSeconds));

        if (!double.IsFinite(targetFrameTimeSeconds))
            throw new ArgumentOutOfRangeException(nameof(targetFrameTimeSeconds));

        originalBudgetSeconds = Math.Max(targetFrameTimeSeconds - elapsedFrameTimeSeconds,
            targetFrameTimeSeconds * MINIMUM_AVAILABLE_TIME_FRACTION) + savedProcessingTimeSeconds;
        this.targetFrameTimeSeconds = targetFrameTimeSeconds;
        hasExecutedCompletionUnit = false;
    }

    /// <summary>
    ///   Gets the number of seconds in the frame budget that have not yet elapsed.
    /// </summary>
    public double GetRemainingSeconds<TTimeSource>(ref TTimeSource timeSource)
        where TTimeSource : struct, IResourceLoadFrameTimeSource
    {
        return originalBudgetSeconds - timeSource.Elapsed.TotalSeconds;
    }

    /// <summary>
    ///   Attempts to admit and execute one indivisible main-thread completion unit.
    /// </summary>
    /// <remarks>
    ///   Work that fits in the remaining budget is admitted normally. To ensure progress, the first main-thread
    ///   completion unit in a frame may exceed the remaining positive budget. Scheduling work performed before this
    ///   call does not consume that forced-admission opportunity, though its elapsed time still reduces the remaining
    ///   budget.
    /// </remarks>
    public bool TryRunCompletionUnit<TCompletionUnit, TTimeSource>(ref TCompletionUnit completionUnit,
        ref TTimeSource timeSource)
        where TCompletionUnit : struct, IResourceLoadCompletionUnit
        where TTimeSource : struct, IResourceLoadFrameTimeSource
    {
        double estimatedDurationSeconds = completionUnit.EstimatedDurationSeconds;

        ArgumentOutOfRangeException.ThrowIfNegative(estimatedDurationSeconds);

        if (!double.IsFinite(estimatedDurationSeconds))
            throw new ArgumentOutOfRangeException(nameof(completionUnit));

        double remainingSeconds = GetRemainingSeconds(ref timeSource);

        if (remainingSeconds <= 0)
            return false;

        if (estimatedDurationSeconds > remainingSeconds && hasExecutedCompletionUnit)
            return false;

        hasExecutedCompletionUnit = true;
        completionUnit.Execute();
        return true;
    }

    /// <summary>
    ///   Calculates the number of processing seconds carried into the next frame from the actual elapsed time.
    /// </summary>
    public float CalculateSecondsToCarry<TTimeSource>(ref TTimeSource timeSource)
        where TTimeSource : struct, IResourceLoadFrameTimeSource
    {
        return (float)Math.Clamp(GetRemainingSeconds(ref timeSource), targetFrameTimeSeconds * MINIMUM_CARRY_FRAMES,
            targetFrameTimeSeconds * MAXIMUM_CARRY_FRAMES);
    }
}

internal readonly struct StopwatchResourceLoadFrameTimeSource : IResourceLoadFrameTimeSource
{
    private readonly Stopwatch stopwatch;

    public StopwatchResourceLoadFrameTimeSource(Stopwatch stopwatch)
    {
        this.stopwatch = stopwatch;
    }

    public TimeSpan Elapsed => stopwatch.Elapsed;
}
