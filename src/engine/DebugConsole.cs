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

    public override void _Process(double delta)
    {
        if (!IsConsoleOpen)
            return;

        var lineCount = consoleArea.GetLineCount();
        var lines = DebugConsoleManager.Lines;

        if (lineCount + lines.Count > DebugConsoleManager.MaxConsoleSize)
        {
            for (var i = 0; i < lines.Count; i += 1)
            {
                consoleArea.RemoveParagraph(0, true);
            }

            consoleArea.InvalidateParagraph(0);
        }

        while (lines.TryDequeue(out var ln))
        {
            consoleArea.PushColor(ln.Color);
            consoleArea.AppendText(ln.Line.StripEdges(false));
            consoleArea.Pop();
            consoleArea.Newline();
        }

        base._Process(delta);
    }

    public void AddLog(DebugConsoleManager.ConsoleLine line)
    {
        DebugConsoleManager.Lines.Enqueue(line);
    }
}
