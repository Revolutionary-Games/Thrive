using Godot;

/// <summary>
///   Handles the debug console
/// </summary>
public partial class DebugConsole : CustomWindow
{
#pragma warning disable CA2213
    [Export]
    private RichTextLabel consoleArea = null!;
#pragma warning restore CA2213

    public bool IsConsoleOpen
    {
        get => Visible;
        set
        {
            if (value)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
    }

    public override void _Ready()
    {
        consoleArea.Clear();
        foreach (var line in DebugConsoleManager.GetInstance()!.History)
        {
            AddLog(line);
        }

        DebugConsoleManager.GetInstance()!.OnLogReceived += AddLog;
    }

    public void AddLog(DebugConsoleManager.ConsoleLine line)
    {
        consoleArea.PushColor(line.Color);
        consoleArea.AddText(line.Line);
        consoleArea.Pop();
    }

    private void AddLog(object? o, DebugConsoleManager.ConsoleLineArgs line)
    {
        AddLog(line.Line);
    }
}
