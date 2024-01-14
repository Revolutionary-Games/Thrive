namespace Components
{
    /// <summary>
    ///   Marks an entity as the one for the game's camera to follow. Also requires a <see cref="WorldPosition"/>
    ///   component.
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct CameraFollowTarget
    {
        /// <summary>
        ///   If set to true this target is ignored. Only one active target should exist as once, otherwise a random
        ///   one is selected to show with the camera.
        /// </summary>
        public bool Disabled;
    }
}
