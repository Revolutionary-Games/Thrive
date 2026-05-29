using System;
using System.Threading.Tasks;

/// <summary>
///   A dummy exception thrown only to simulate an unhandled exception.
/// </summary>
[Serializable]
public class DummyException : Exception
{
    private const string DummyExceptionDefaultMessage = "This is not a real error, but just an exception used for " +
        "testing.";

    public DummyException()
    {
    }

    public DummyException(string message)
        : base(message)
    {
    }

    [Command("throw", false, "Throws a dummy exception for testing how unhandled" +
        "exceptions behave in-game.")]
    private static void CommandThrow(string dummyMessage = DummyExceptionDefaultMessage)
    {
        // Exceptions thrown by the command's body are caught by the CommandRegistry after the execution, so we need
        // to throw the exception in a task's body, and then execute the task.
        TaskExecutor.Instance.AddTask(new Task(() => throw new DummyException(dummyMessage)), true);
    }
}
