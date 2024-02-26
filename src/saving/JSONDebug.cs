namespace Saving
{
    using System.Collections.Generic;
    using Godot;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    ///   JSON saving failure debugging functionality
    /// </summary>
    public static class JSONDebug
    {
        private static readonly List<string> QueuedTraces = new();

        public enum DebugMode
        {
            /// <summary>
            ///   Never use JSON debug mode
            /// </summary>
            AlwaysDisabled,

            /// <summary>
            ///   Debug mode is automatically enable if saving fails
            /// </summary>
            Automatic,

            /// <summary>
            ///   Always use JSON debug mode, even when no error
            /// </summary>
            AlwaysEnabled,
        }

        /// <summary>
        ///   Set to true when an error has occurred. Affects the used debug print settings.
        ///   Reset to false on successful JSON operation.
        /// </summary>
        public static bool ErrorHasOccurred { get; set; }

        /// <summary>
        ///   Called from JSON serializer when it finishes an operation (and logging was enabled)
        /// </summary>
        /// <param name="traceWriter">The used trace writer that contains the output</param>
        public static void OnTraceFinished(ITraceWriter traceWriter)
        {
            var trace = traceWriter.ToString();

            if (string.IsNullOrEmpty(trace))
                return;

            QueuedTraces.Add(trace);
        }

        /// <summary>
        ///   Dump the JSON traces to a file and to console output as well (if short enough) if there are any
        /// </summary>
        public static void FlushJSONTracesOut()
        {
            if (QueuedTraces.Count < 1)
                return;

            using var file = FileAccess.Open(Constants.JSON_DEBUG_OUTPUT_FILE, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr("Failed to open JSON debug file for writing at: ", Constants.JSON_DEBUG_OUTPUT_FILE);
            }
            else
            {
                bool first = true;

                foreach (var trace in QueuedTraces)
                {
                    if (!first)
                        file.StoreLine("---- Start of next trace ----");

                    file.StoreLine(trace);

                    first = false;
                }

                GD.Print("JSON trace written to: ", Constants.JSON_DEBUG_OUTPUT_FILE);
            }

            foreach (var trace in QueuedTraces)
            {
                if (trace.Length > Constants.MAX_JSON_ERROR_LENGTH_FOR_CONSOLE)
                {
                    GD.Print("There's a very long JSON trace, only written to: ", Constants.JSON_DEBUG_OUTPUT_FILE);
                }
                else
                {
                    GD.Print("JSON serialization trace: ", trace);
                }
            }

            QueuedTraces.Clear();

            // Also clear the error status to work with automatic mode better
            ErrorHasOccurred = false;
        }
    }
}
