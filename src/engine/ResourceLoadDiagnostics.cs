using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Godot;

/// <summary>
///   Collects active resource-loading work separately from time spent waiting between frames.
/// </summary>
internal static class ResourceLoadDiagnostics
{
    private static readonly object StateLock = new();
    private static readonly List<double> FrameDurations = new();

    private static long sessionStartTimestamp;
    private static long queueDrainedTimestamp;
    private static long backgroundLoadTicks;
    private static long mainThreadLoadAndPostProcessTicks;
    private static long callbackTicks;

    private static string? firstResourceIdentifier;
    private static int queuedResourceCount;
    private static bool sessionActive;
    private static bool waitingForFinalFrameBoundary;

    private static bool Enabled =>
        Constants.TRACK_ACTUAL_RESOURCE_LOAD_TIMES || Constants.REPORT_ALL_LOAD_TIMES;

    private static bool ReportAll => Constants.REPORT_ALL_LOAD_TIMES;
    private static bool TrackEstimates => Constants.TRACK_ACTUAL_RESOURCE_LOAD_TIMES || ReportAll;

    public static void OnResourceQueued(IResource resource)
    {
        if (!Enabled)
            return;

        lock (StateLock)
        {
            if (!sessionActive)
            {
                sessionActive = true;
                sessionStartTimestamp = Stopwatch.GetTimestamp();
                firstResourceIdentifier = resource.Identifier;
                queuedResourceCount = 0;
                FrameDurations.Clear();
                backgroundLoadTicks = 0;
                mainThreadLoadAndPostProcessTicks = 0;
                callbackTicks = 0;
            }

            waitingForFinalFrameBoundary = false;
            ++queuedResourceCount;
        }
    }

    public static void OnFrameStarted(double delta)
    {
        if (!Enabled)
            return;

        lock (StateLock)
        {
            if (!sessionActive)
                return;

            FrameDurations.Add(delta);

            if (waitingForFinalFrameBoundary)
                ReportAndResetSession();
        }
    }

    public static void OnFrameFinished(bool hasPendingResourceWork)
    {
        if (!Enabled || !sessionActive || hasPendingResourceWork)
            return;

        lock (StateLock)
        {
            if (!sessionActive || waitingForFinalFrameBoundary)
                return;

            queueDrainedTimestamp = Stopwatch.GetTimestamp();
            waitingForFinalFrameBoundary = true;
        }
    }

    public static long BeginOperation()
    {
        return Enabled ? Stopwatch.GetTimestamp() : 0;
    }

    public static void RecordBackgroundLoad(IResource resource, long startedAt, double? estimatedSeconds = null)
    {
        RecordOperation(resource, "background-load", startedAt, estimatedSeconds, ref backgroundLoadTicks);
    }

    public static void RecordMainThreadLoadOrPostProcess(IResource resource, long startedAt,
        double? estimatedSeconds = null)
    {
        RecordOperation(resource, "main-thread-load-or-post-process", startedAt, estimatedSeconds,
            ref mainThreadLoadAndPostProcessTicks);
    }

    public static void RecordCallback(IResource resource, long startedAt)
    {
        RecordOperation(resource, "callback", startedAt, null, ref callbackTicks);
    }

    private static void RecordOperation(IResource resource, string phase, long startedAt, double? estimatedSeconds,
        ref long totalTicks)
    {
        if (!Enabled || startedAt == 0)
            return;

        long elapsedTicks = Stopwatch.GetTimestamp() - startedAt;

        lock (StateLock)
        {
            totalTicks += elapsedTicks;
        }

        double elapsedSeconds = TicksToSeconds(elapsedTicks);

        if (TrackEstimates && estimatedSeconds.HasValue)
        {
            double difference = elapsedSeconds - estimatedSeconds.Value;

            if (Math.Abs(difference) > Constants.REPORT_LOAD_TIMES_OF_BY)
            {
                GD.Print(string.Format(CultureInfo.InvariantCulture,
                    "Resource load estimate off: phase={0}, difference_s={1:F6}, identifier={2}", phase, difference,
                    resource.Identifier));
            }
        }

        if (ReportAll)
        {
            string estimate = estimatedSeconds?.ToString("F6", CultureInfo.InvariantCulture) ?? "none";

            GD.Print(string.Format(CultureInfo.InvariantCulture,
                "Resource active time: phase={0}, active_s={1:F6}, estimate_s={2}, identifier={3}", phase,
                elapsedSeconds, estimate, resource.Identifier));
        }
    }

    private static void ReportAndResetSession()
    {
        if (ReportAll)
        {
            FrameDurations.Sort();

            double p50 = GetPercentile(FrameDurations, 0.50);
            double p95 = GetPercentile(FrameDurations, 0.95);
            double p99 = GetPercentile(FrameDurations, 0.99);
            double maximum = FrameDurations.Count > 0 ? FrameDurations[^1] : 0;
            double queueClearSeconds = TicksToSeconds(queueDrainedTimestamp - sessionStartTimestamp);

            GD.Print(string.Format(CultureInfo.InvariantCulture,
                "Resource load session: first={0}, queued={1}, frames={2}, frame_ms_p50={3:F3}, " +
                "frame_ms_p95={4:F3}, frame_ms_p99={5:F3}, frame_ms_max={6:F3}, " +
                "background_load_active_ms={7:F3}, main_thread_load_post_active_ms={8:F3}, " +
                "callback_active_ms={9:F3}, queue_clear_ms={10:F3}", firstResourceIdentifier, queuedResourceCount,
                FrameDurations.Count, p50 * 1000, p95 * 1000, p99 * 1000, maximum * 1000,
                TicksToSeconds(backgroundLoadTicks) * 1000,
                TicksToSeconds(mainThreadLoadAndPostProcessTicks) * 1000, TicksToSeconds(callbackTicks) * 1000,
                queueClearSeconds * 1000));
        }

        sessionActive = false;
        waitingForFinalFrameBoundary = false;
        firstResourceIdentifier = null;
        queuedResourceCount = 0;
        FrameDurations.Clear();
    }

    private static double GetPercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count < 1)
            return 0;

        int index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        return sortedValues[Math.Clamp(index, 0, sortedValues.Count - 1)];
    }

    private static double TicksToSeconds(long ticks)
    {
        return ticks / (double)Stopwatch.Frequency;
    }
}
