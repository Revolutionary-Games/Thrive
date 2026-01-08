using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles the debug console
/// </summary>
public partial class DebugConsole : CustomWindow
{
    private readonly Queue<DebugConsoleManager.ConsoleLine> lineBuffer = [];

#pragma warning disable CA2213
    [Export]
    private RichTextLabel consoleArea = null!;
#pragma warning restore CA2213

    public static bool CanOpenConsole => Settings.Instance.DebugConsoleEnabled;

    public bool IsConsoleOpen
    {
        get => Visible;
        set
        {
            if (!CanOpenConsole && value)
                throw new InvalidOperationException("Debug Console is disabled");

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
        ReloadGUI();

        base._Ready();
    }

    public override void _EnterTree()
    {
        DebugConsoleManager.OnMessageReceived += AddLog;

        base._EnterTree();
    }

    public override void _ExitTree()
    {
        DebugConsoleManager.OnMessageReceived -= AddLog;

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        if (!IsConsoleOpen)
            return;

        lock (lineBuffer)
        {
            var lineCount = consoleArea.GetLineCount();

            if (lineCount + lineBuffer.Count > DebugConsoleManager.MaxConsoleSize)
            {
                for (var i = 0; i < lineBuffer.Count; i += 1)
                {
                    consoleArea.RemoveParagraph(0, true);
                }

                consoleArea.InvalidateParagraph(0);
            }

            while (lineBuffer.Count > 0)
            {
                var ln = lineBuffer.Dequeue();

                consoleArea.PushColor(ln.Color);
                consoleArea.AppendText(ln.Line.StripEdges(false));
                consoleArea.Pop();
                consoleArea.Newline();
            }

            lineBuffer.Clear();
        }

        base._Process(delta);
    }

    public void ReloadGUI()
    {
        lineBuffer.Clear();
        consoleArea.Clear();

        foreach (var ln in DebugConsoleManager.GetLines())
        {
            AddLog(ln);
        }
    }

    public void AddLog(object? s, DebugConsoleManager.ConsoleLineArgs args)
    {
        AddLog(args.Line);
    }

    public void AddLog(DebugConsoleManager.ConsoleLine line)
    {
        lock (lineBuffer)
        {
            lineBuffer.Enqueue(line);
        }
    }
}
