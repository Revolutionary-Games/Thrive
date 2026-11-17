using System;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
///   Shared state of one <see cref="TaskExecutor.ParallelFor{TBody}"/> call.
/// </summary>
public abstract class ParallelForWork
{
    public abstract Exception? FirstError { get; }

    /// <summary>
    ///   Claims and runs batches until there are none left. Never throws because of the loop body.
    /// </summary>
    public abstract void RunBatches();
}

public sealed class ParallelForWork<TBody> : ParallelForWork
    where TBody : struct, IParallelForBody
{
    private readonly int fromInclusive;
    private readonly int toExclusive;
    private readonly int batchSize;
    private readonly int batchCount;

    private TBody body;

    private BatchCounters counters;

    private volatile bool failed;

    private Exception? firstError;

    public ParallelForWork(int fromInclusive, int toExclusive, int batchSize, int batchCount, TBody body)
    {
        this.fromInclusive = fromInclusive;
        this.toExclusive = toExclusive;
        this.batchSize = batchSize;
        this.batchCount = batchCount;
        this.body = body;
    }

    public override Exception? FirstError => Volatile.Read(ref firstError);

    public override void RunBatches()
    {
        while (true)
        {
            int batch = Interlocked.Increment(ref counters.NextBatch) - 1;

            if (batch >= batchCount)
                return;

            // After a failure, the remaining batches are only counted as done so the caller can stop waiting
            if (!failed)
            {
                long start = fromInclusive + (long)batch * batchSize;
                int end = (int)Math.Min(start + batchSize, toExclusive);

                try
                {
                    body.Execute((int)start, end);
                }
                catch (Exception e)
                {
                    Interlocked.CompareExchange(ref firstError, e, null);
                    failed = true;
                }
            }

            // Full fence: makes the batch's results visible to the thread waiting for completion
            Interlocked.Increment(ref counters.CompletedBatches);
        }
    }

    /// <summary>
    ///   Waits until batches claimed by other threads have finished. Must be called only by the thread that
    ///   started the loop and only after it has run out of batches to claim in <see cref="RunBatches"/>.
    /// </summary>
    public void WaitForAllBatches()
    {
        // Batches are expected to be short, so this spins and yields instead of blocking. Sleeping is disabled
        // as a minimum sleep of a millisecond is a long time for loops that run each frame.
        var spinner = default(SpinWait);

        while (Volatile.Read(ref counters.CompletedBatches) < batchCount)
        {
            spinner.SpinOnce(-1);
        }

        body = default;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 192)]
#pragma warning disable SA1201
public struct BatchCounters
#pragma warning restore SA1201
{
    [FieldOffset(64)]
    public int NextBatch;

    [FieldOffset(128)]
    public int CompletedBatches;
}
