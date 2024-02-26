namespace Components
{
    using Godot;

    /// <summary>
    ///   World-space coordinates of an entity. Note a constructor must be used to get <see cref="Rotation"/>
    ///   initialized correctly
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct WorldPosition
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public WorldPosition(Vector3 position)
        {
            Position = position;
            Rotation = Quaternion.Identity;
        }

        public WorldPosition(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    public static class WorldPositionHelpers
    {
        public static Transform3D ToTransform(this ref WorldPosition position)
        {
            return new Transform3D(new Basis(position.Rotation), position.Position);
        }
    }
}
