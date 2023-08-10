namespace Components
{
    using Godot;

    /// <summary>
    ///   Sends and receivers command signals (signaling agent)
    /// </summary>
    public struct CommandSignaler
    {
        // TODO: system to update
        public Vector3 EmittedPosition;

        public Vector3 ReceivedCommandSource;

        /// <summary>
        ///   Because AI is ran in parallel thread, if it wants to change the signaling, it needs to do it through this
        /// </summary>
        public MicrobeSignalCommand? QueuedSignalingCommand;

        public MicrobeSignalCommand Command;

        public MicrobeSignalCommand ReceivedCommand;
    }
}
