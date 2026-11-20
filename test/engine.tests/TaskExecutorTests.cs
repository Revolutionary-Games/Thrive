using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Verifies task batches hand ownership back to their caller without retaining earlier failures.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class TaskExecutorTests
{
    [TestCase]
    public void RunTasks_FailureDoesNotContaminateNextBatch()
    {
        var failure = new InvalidOperationException("Expected task failure");
        var failedTask = new Task(() => throw failure);

        Exception? observed = null;
        try
        {
            TaskExecutor.Instance.RunTasks([failedTask]);
        }
        catch (AggregateException e)
        {
            observed = e.InnerException;
        }

        AssertThat(observed).IsSame(failure);

        var healthyTask = new Task(() => { });
        TaskExecutor.Instance.RunTasks([healthyTask]);

        AssertThat(healthyTask.IsCompletedSuccessfully).IsTrue();
    }

    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, true)]
    public void RunTasks_WaitsForAcceptedWork(bool fail, bool help, bool cancelledBeforeStart)
    {
        using var cancellation = new CancellationTokenSource();
        var siblingStarted = new ManualResetEventSlim();
        var releaseSibling = new ManualResetEventSlim();
        var returned = new ManualResetEventSlim();

        // The finally block joins the caller and waits for the sibling before disposing these captured events.
        // ReSharper cannot infer that lifetime guarantee from the completion assertions.
        var sibling = new Task(() =>
        {
            // ReSharper disable once AccessToDisposedClosure
            siblingStarted.Set();

            // ReSharper disable once AccessToDisposedClosure
            if (!releaseSibling.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Sibling was not released");
        });
        var first = new Task(() =>
        {
            // ReSharper disable once AccessToDisposedClosure
            if (!siblingStarted.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Sibling did not start");
            if (fail)
                throw new InvalidOperationException("Expected failure before sibling completion");
        }, cancellation.Token);
        Exception? failure = null;
        bool siblingCompletedAtReturn = false;
        var caller = new Thread(() =>
        {
            try
            {
                TaskExecutor.Instance.RunTasks(new List<Task> { first, sibling }, help);
            }
            catch (Exception e)
            {
                failure = e;
            }
            finally
            {
                siblingCompletedAtReturn = sibling.IsCompleted;

                // ReSharper disable once AccessToDisposedClosure
                returned.Set();
            }
        }) { IsBackground = true };

        if (cancelledBeforeStart)
            cancellation.Cancel();

        caller.Start();
        try
        {
            // First has ended and the sibling is held. Observe the caller actually waiting or returning;
            // a delay is not used to guess when the executor has reached its completion boundary.
            AssertThat(SpinWait.SpinUntil(() => first.IsCompleted, TimeSpan.FromSeconds(10))).IsTrue();

            // SpinUntil invokes this predicate synchronously, before the finally block disposes returned.
            // ReSharper disable once AccessToDisposedClosure
            AssertThat(SpinWait.SpinUntil(() => returned.IsSet ||
                (caller.ThreadState & ThreadState.WaitSleepJoin) != 0, TimeSpan.FromSeconds(10))).IsTrue();
            AssertThat(returned.IsSet).IsFalse();
        }
        finally
        {
            releaseSibling.Set();
            AssertThat(caller.Join(TimeSpan.FromSeconds(10))).IsTrue();
            AssertThat(sibling.Wait(TimeSpan.FromSeconds(10))).IsTrue();

            // Dispose only after both users stop. A timeout must not dispose events still used by background work.
            siblingStarted.Dispose();
            releaseSibling.Dispose();
            returned.Dispose();
        }

        AssertThat(siblingCompletedAtReturn).IsTrue();
        AssertThat(failure != null).IsEqual(fail || cancelledBeforeStart);
        TaskExecutor.Instance.RunTasks([new Task(() => { })]);
    }

    [TestCase]
    public void RunTasks_NestedBatchDoesNotWaitForItsCaller()
    {
        var inner = new Task(() => { });
        var outer = new Task(() => TaskExecutor.Instance.RunTasks([inner]));
        TaskExecutor.Instance.RunTasks([outer]);
        AssertThat(inner.IsCompletedSuccessfully && outer.IsCompletedSuccessfully).IsTrue();
        TaskExecutor.Instance.RunTasks(new List<Task>());
    }

    [TestCase(0, 0, 1)]
    [TestCase(5, 6, 1)]
    [TestCase(-50, 50, 1)]
    [TestCase(0, 10007, 1)]
    [TestCase(0, 10007, 64)]
    [TestCase(0, 10007, 100000)]
    [TestCase(3, 2, 1)]
    public void ParallelFor_RunsEveryIndexExactlyOnce(int from, int to, int minBatchSize)
    {
        const int margin = 3;
        int lowest = Math.Min(from, to) - margin;
        var counts = new int[Math.Max(to - from, 0) + 2 * margin + Math.Max(from - to, 0)];

        for (int repeat = 0; repeat < 20; ++repeat)
        {
            Array.Clear(counts);
            TaskExecutor.Instance.ParallelFor(from, to, new CountingBody(counts, lowest), minBatchSize);

            for (int i = 0; i < counts.Length; ++i)
            {
                int index = lowest + i;
                int expected = index >= from && index < to ? 1 : 0;
                AssertThat(counts[i]).OverrideFailureMessage($"Index {index} ran {counts[i]} times")
                    .IsEqual(expected);
            }
        }
    }

    [TestCase]
    public void ParallelFor_InvalidBatchSizeThrows()
    {
        AssertThrown(() => TaskExecutor.Instance.ParallelFor(0, 10, new CountingBody(new int[10], 0), 0))
            .IsInstanceOf<ArgumentOutOfRangeException>();
    }

    [TestCase]
    public void ParallelFor_FailureIsRethrownAndWorkersKeepRunning()
    {
        var failure = new InvalidOperationException("Expected loop failure");

        Exception? observed = null;
        try
        {
            TaskExecutor.Instance.ParallelFor(0, 10000, new ThrowingBody(failure));
        }
        catch (AggregateException e)
        {
            observed = e.InnerException;
        }

        AssertThat(observed).IsSame(failure);

        observed = null;
        try
        {
            TaskExecutor.Instance.ParallelFor(0, 1, new ThrowingBody(failure));
        }
        catch (AggregateException e)
        {
            observed = e.InnerException;
        }

        AssertThat(observed).IsSame(failure);

        AssertAllWorkerThreadsParticipate();
    }

    [TestCase]
    public void ParallelFor_UsesAllWorkerThreads()
    {
        AssertAllWorkerThreadsParticipate();
    }

    [TestCase]
    public void ParallelFor_NestedLoopsComplete()
    {
        const int outer = 16;
        const int inner = 1000;
        var sums = new long[outer];

        var finished = new Thread(() => TaskExecutor.Instance.ParallelFor(0, outer, new NestedBody(sums, inner)))
        {
            IsBackground = true,
        };
        finished.Start();

        // A deadlock would otherwise hang the test run
        AssertThat(finished.Join(TimeSpan.FromSeconds(10))).IsTrue();

        long expected = (long)inner * (inner - 1) / 2;
        foreach (var sum in sums)
            AssertThat(sum).IsEqual(expected);
    }

    [TestCase]
    public void ParallelFor_RunsFromInsideExecutorTasks()
    {
        var counts = new int[5000];
        var task = new Task(() => TaskExecutor.Instance.ParallelFor(0, counts.Length, new CountingBody(counts, 0)));
        var sibling = new Task(() => TaskExecutor.Instance.ParallelFor(0, 100, new CountingBody(new int[100], 0)));

        TaskExecutor.Instance.RunTasks(new List<Task> { task, sibling }, true);

        foreach (var count in counts)
            AssertThat(count).IsEqual(1);
    }

    private static void AssertAllWorkerThreadsParticipate()
    {
        int threads = TaskExecutor.Instance.ParallelTasks + 1;
        using var allStarted = new CountdownEvent(threads);
        var timedOut = new int[1];

        TaskExecutor.Instance.ParallelFor(0, threads, new RendezvousBody(allStarted, timedOut));

        AssertThat(timedOut[0]).OverrideFailureMessage("Not all executor threads joined the parallel loop")
            .IsEqual(0);
    }

    private readonly struct CountingBody(int[] counts, int lowestIndex) : IParallelForBody
    {
        public void Execute(int fromInclusive, int toExclusive)
        {
            for (int i = fromInclusive; i < toExclusive; ++i)
                Interlocked.Increment(ref counts[i - lowestIndex]);
        }
    }

    private readonly struct ThrowingBody(Exception failure) : IParallelForBody
    {
        public void Execute(int fromInclusive, int toExclusive)
        {
            throw failure;
        }
    }

    private readonly struct NestedBody(long[] sums, int innerCount) : IParallelForBody
    {
        public void Execute(int fromInclusive, int toExclusive)
        {
            for (int i = fromInclusive; i < toExclusive; ++i)
            {
                var values = new int[innerCount];
                TaskExecutor.Instance.ParallelFor(0, innerCount, new IndexWritingBody(values));

                long sum = 0;
                foreach (var value in values)
                    sum += value;

                sums[i] = sum;
            }
        }
    }

    private readonly struct IndexWritingBody(int[] values) : IParallelForBody
    {
        public void Execute(int fromInclusive, int toExclusive)
        {
            for (int i = fromInclusive; i < toExclusive; ++i)
                values[i] = i;
        }
    }

    private readonly struct RendezvousBody(CountdownEvent allStarted, int[] timedOut) : IParallelForBody
    {
        public void Execute(int fromInclusive, int toExclusive)
        {
            for (int i = fromInclusive; i < toExclusive; ++i)
            {
                allStarted.Signal();

                if (!allStarted.Wait(TimeSpan.FromSeconds(10)))
                    Interlocked.Increment(ref timedOut[0]);
            }
        }
    }
}
