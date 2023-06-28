namespace Components
{
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Physics body for an entity
    /// </summary>
    public struct Physics
    {
        [JsonIgnore]
        public NativePhysicsBody? Body;

        [JsonIgnore]
        public PhysicalWorld? BodyCreatedInWorld;

        // TODO: maybe the following could be assumed true when AttachedToEntity component exists?
        /// <summary>
        ///   When the body is disabled the body state is no longer read into the position variables allowing custom
        ///   control
        /// </summary>
        [JsonProperty]
        public bool BodyDisabled;
    }

    public static class PhysicsExtensions
    {
        public static void SetVelocityToZero(this Physics entity)
        {
            if (entity.Body == null || !entity.CheckHasWorldReference())
                return;

            entity.BodyCreatedInWorld!.SetBodyVelocity(entity.Body, Vector3.Zero, Vector3.Zero);
        }

        public static bool CheckHasWorldReference(this Physics entity)
        {
            if (entity.BodyCreatedInWorld == null)
            {
                GD.PrintErr("Physics entity doesn't have a known physics world, can't apply an operation");
                return false;
            }

            return true;
        }
    }
}
