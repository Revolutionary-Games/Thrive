public interface ICommandInvoker
{
    public CommandHistory CommandHistory { get; }

    public void Clear();
    public void Print(DebugConsoleManager.RawDebugEntry entry);
    public void SubmitCommand(string command);
}
