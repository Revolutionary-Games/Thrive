using System.Diagnostics;
using System.Text;
using Godot;
using Godot.Collections;

/// <summary>
///   Receives Godot Engine logs and allows us to process them in realtime
/// </summary>
public partial class LogInterceptor : Logger
{
    private static LogInterceptor? instance;

    private readonly StringBuilder errorBuilder = new();

    private readonly string?[] previousMessageBuffer = new string[10];
    private int previousMessageBufferIndex;

    public LogInterceptor()
    {
        previousMessageBufferIndex = previousMessageBuffer.Length;
    }

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

    // The log callbacks may be called from other threads

    public override void _LogMessage(string message, bool error)
    {
        base._LogMessage(message, error);

        lock (previousMessageBuffer)
        {
            // Save messages in a circular buffer for later retrieval
            ++previousMessageBufferIndex;

            if (previousMessageBufferIndex >= previousMessageBuffer.Length)
                previousMessageBufferIndex = 0;

            previousMessageBuffer[previousMessageBufferIndex] = message;

            // TODO: do some errors just show up as a single error message?
            // Some probably do but do we care about any of them?
            // Should we do some handling for them?
            // These errors as they aren't very visible, would make sense to show despite a debugger being attached
        }
    }

    public override void _LogError(string function, string file, int line, string code, string rationale,
        bool editorNotify, int errorType, Array<ScriptBacktrace> scriptBacktraces)
    {
        base._LogError(function, file, line, code, rationale, editorNotify, errorType, scriptBacktraces);

        // Only trigger on errors
        if (errorType != (int)ErrorType.Error)
            return;

        // Ignore some errors we do not care about
        // This is technically a warning we could remove now with the type check
        if (code.Contains("with non-equal opposite anchors"))
            return;

        // We might want to ignore this somewhat intermittent error: Parent node is busy adding
        // that sometimes happens on scene switch but doesn't seem to cause any problems

        // Avoid recursion
        if (code.Contains("Unhandled Exception Log"))
            return;

        if (Engine.IsEditorHint())
            return;

        // Don't want to show extra errors when a debugger is attached
        if (Debugger.IsAttached)
        {
            Debugger.Break();
            return;
        }

        // Forward other errors to notify the player about the error
        lock (previousMessageBuffer)
        {
            // Read in reverse order to get a natural order
            for (int i = previousMessageBuffer.Length - 1; i >= 0; --i)
            {
                var potentialLine =
                    previousMessageBuffer[
                        (previousMessageBufferIndex - i).PositiveModulo(previousMessageBuffer.Length)];

                // The lines already have line endings in them
                if (potentialLine != null)
                    errorBuilder.Append(potentialLine);
            }

            var contextLines = errorBuilder.ToString();
            errorBuilder.Clear();

            // As we use only C# code, the only thing we need is the "code" which contains the C# backtrace

            var finalError = code;

            Invoke.Instance.Perform(() =>
            {
                var gui = UnHandledErrorsGUI.Instance;

                if (gui == null)
                {
                    // We can print again as this is no longer in the error callback context
                    GD.PrintErr("No unhandled error receiver");
                    return;
                }

                gui.ReportError(finalError, contextLines);
            });
        }
    }
}
