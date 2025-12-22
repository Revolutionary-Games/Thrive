using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles the debug console
/// </summary>
public partial class DebugConsole : CustomWindow
{
    private readonly List<DebugConsoleManager.ConsoleLine> lineBuffer = [];

#pragma warning disable CA2213
    [Export]
    private TextEdit consoleArea = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Whether the console may be opened.
    /// </summary>
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
        consoleArea.SyntaxHighlighter = DebugConsoleManager.Highlighter;
        consoleArea.SetEditable(false);

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
        lock (lineBuffer)
        {
            foreach (var ln in lineBuffer)
            {
                consoleArea.SetCaretLine(-1);
                consoleArea.SetCaretColumn(-1);

                consoleArea.InsertTextAtCaret(ln.Line);
            }

            lineBuffer.Clear();
        }

        base._Process(delta);
    }

    public void ReloadGUI()
    {
        consoleArea.Text = string.Empty;

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
            lineBuffer.Add(line);
        }
    }
}
