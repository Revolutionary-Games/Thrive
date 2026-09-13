using System;
using System.Collections.Generic;
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
