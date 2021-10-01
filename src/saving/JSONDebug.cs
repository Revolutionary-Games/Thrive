namespace Saving
{
    /// <summary>
    ///   JSON saving failure debugging functionality
    /// </summary>
    public static class JSONDebug
    {
        private static bool errorHasOccurred;

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
        ///   Resets the flag that is set when a save error occurs
        /// </summary>
        public static void ResetErrorStatus()
        {
            errorHasOccurred = false;
        }
    }
}
