using System;
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

    /// <summary>
    ///   Forwards errors from elsewhere in the codebase to popup the GUI for unhandled errors
    /// </summary>
    /// <param name="error">The error that happened and the user should know</param>
    /// <param name="extraInfo">Extra info to append to the error popup</param>
    public static void ForwardCaughtError(Exception error, string? extraInfo = null)
    {
        if (instance == null)
        {
            GD.PrintErr("No GUI error receiver");
            return;
        }

        instance.PerformForward(error, extraInfo);
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

        DebugConsoleManager.Instance.Print(message, error);
    }

    public override void _LogError(string function, string file, int line, string code, string rationale,
        bool editorNotify, int errorType, Array<ScriptBacktrace> scriptBacktraces)
    {
        base._LogError(function, file, line, code, rationale, editorNotify, errorType, scriptBacktraces);

        DebugConsoleManager.Instance.Print(code + ": " + rationale, true);

        // Only trigger on errors
        if (errorType != (int)ErrorType.Error)
            return;

        // Ignore some errors we do not care about
        // This is technically a warning we could remove now with the type check
        if (rationale.Contains("with non-equal opposite anchors"))
            return;

        if (rationale.Contains("Parent node is busy setting up children") ||
            rationale.Contains("Parent node is busy adding"))
        {
            return;
        }

        // Disconnected screens after changing settings can cause these
        if (code.Contains("p_screen") && code.Contains("is out of bounds"))
        {
            return;
        }

        // Ignore unsupported antialiasing modes as it would be very complex to hide the options in the GUI:
        // https://github.com/Revolutionary-Games/Thrive/pull/6535#issuecomment-3611112455
        if ((rationale.Contains("only available when using the") && rationale.Contains("renderer.")) ||
            (rationale.Contains("currently unavailable on") && rationale.Contains("renderer.")))
        {
            return;
        }

        // Release-mode-only bug: https://github.com/Revolutionary-Games/Thrive/issues/5082
        if (rationale.Contains("tempt to disconnect a nonexistent connection from 'root:<Window"))
            return;

        // Potentially another related, bug one that Iman is reporting often so... we'll just ignore it
        if (rationale.Contains("disconnect a nonexistent connection") && rationale.Contains("ActiveModalContainer"))
            return;

        // Again, an engine bug that is caused by reopening a dropdown menu that always triggers this error
        if (rationale.Contains("is already connected to given callable"))
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
        // As we use only C# code, the only thing we need is the "code" which contains the C# backtrace
        // Though we let some engine errors through which do have quite critical info in rationale, so append it
        ForwardToGUI(code, rationale);
    }

    private void ForwardToGUI(string finalError, string? rationale)
    {
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

            if (!string.IsNullOrEmpty(rationale))
            {
                finalError += "\n" + rationale;
            }

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

    private void PerformForward(Exception error, string? extraInfo = null)
    {
#if DEBUG
        if (Debugger.IsAttached)
            Debugger.Break();
#endif

        ForwardToGUI(error.ToString(), extraInfo);
    }
}
