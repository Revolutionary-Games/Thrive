using System.Diagnostics;
using Godot;
using Godot.Collections;

/// <summary>
///   Receives Godot Engine logs and allows us to process them in realtime
/// </summary>
public partial class LogInterceptor : Logger
{
    private static LogInterceptor? instance;

    private readonly string[] previousMessageBuffer = new string[10];
    private int nextMessageBufferIndex = 0;

    public static void Register()
    {
        if (instance != null)
        {
            GD.PrintErr("LogInterceptor already registered");
            return;
        }

        instance = new LogInterceptor();
        OS.AddLogger(instance);
    }

    public static void Remove()
    {
        if (instance == null)
        {
            GD.PrintErr("LogInterceptor not registered");
            return;
        }

        OS.RemoveLogger(instance);
        instance = null;
    }

    public override void _LogMessage(string message, bool error)
    {
        base._LogMessage(message, error);

        // Save messages in a circular buffer for later retrieval
        previousMessageBuffer[nextMessageBufferIndex] = message;
        ++nextMessageBufferIndex;

        if (nextMessageBufferIndex >= previousMessageBuffer.Length)
            nextMessageBufferIndex = 0;

        // TODO: do some errors just show up as a single error message?
        // Some probably do but do we care about any of them? Should we do some handling for them?
    }

    public override void _LogError(string function, string file, int line, string code, string rationale,
        bool editorNotify, int errorType, Array<ScriptBacktrace> scriptBacktraces)
    {
        base._LogError(function, file, line, code, rationale, editorNotify, errorType, scriptBacktraces);

        // TODO: ignore some errors we do not care about?

        // But forward others to notify about the error

        if (nextMessageBufferIndex > 0)
            Debugger.Break();
    }
}
