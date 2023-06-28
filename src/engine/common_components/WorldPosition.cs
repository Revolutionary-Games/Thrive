namespace Components
{
    using Godot;

    /// <summary>
    ///   World-space coordinates of an entity. Note a constructor must be used to get <see cref="Rotation"/>
    ///   initialized correctly
    /// </summary>
    public struct WorldPosition
    {
        public Vector3 Position;
        public Quat Rotation;

        public WorldPosition(Vector3 position)
        {
            Position = position;
            Rotation = Quat.Identity;
        }

        public WorldPosition(Vector3 position, Quat rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
