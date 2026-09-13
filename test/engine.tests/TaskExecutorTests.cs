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
            TaskExecutor.Instance.RunTasks(new List<Task> { failedTask });
        }
        catch (AggregateException e)
        {
            observed = e.InnerException;
        }

        AssertThat(observed).IsSame(failure);

        var healthyTask = new Task(() => { });
        TaskExecutor.Instance.RunTasks(new List<Task> { healthyTask });

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
        var sibling = new Task(() =>
        {
            siblingStarted.Set();
            if (!releaseSibling.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Sibling was not released");
        });
        var first = new Task(() =>
        {
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
        TaskExecutor.Instance.RunTasks(new List<Task> { new(() => { }) });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RunTasks_ObservesEveryFailureBeforeReturning(bool catchErrors)
    {
        var firstFailure = new InvalidOperationException("First expected failure");
        var secondFailure = new InvalidOperationException("Second expected failure");
        var first = new Task(() => throw firstFailure);
        var second = new Task(() => throw secondFailure);
        AggregateException? observed = null;
        try
        {
            TaskExecutor.Instance.RunTasks(new List<Task> { first, second }, catchErrors: catchErrors);
        }
        catch (AggregateException e)
        {
            observed = e;
        }

        AssertThat(first.IsFaulted && second.IsFaulted).IsTrue();
        if (catchErrors)
        {
            AssertThat(observed).IsNull();
        }
        else
        {
            AssertThat(observed).IsNotNull();
            AssertThat(observed!.InnerExceptions.Contains(firstFailure)).IsTrue();
            AssertThat(observed.InnerExceptions.Contains(secondFailure)).IsTrue();
        }

        TaskExecutor.Instance.RunTasks(new List<Task> { new(() => { }) });
    }

    [TestCase]
    public void RunTasks_NestedBatchDoesNotWaitForItsCaller()
    {
        var inner = new Task(() => { });
        var outer = new Task(() => TaskExecutor.Instance.RunTasks(new List<Task> { inner }));
        TaskExecutor.Instance.RunTasks(new List<Task> { outer });
        AssertThat(inner.IsCompletedSuccessfully && outer.IsCompletedSuccessfully).IsTrue();
        TaskExecutor.Instance.RunTasks(new List<Task>());
    }
}
