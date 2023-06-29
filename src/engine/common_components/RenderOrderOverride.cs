namespace Components
{
    using Newtonsoft.Json;

    /// <summary>
    ///   Overrides rendering order for an entity with <see cref="EntityMaterial"/>. Used for some specific rendering
    ///   effects that can't be done otherwise.
    /// </summary>
    public struct RenderOrderOverride
    {
        /// <summary>
        ///   Overrides the render priority of this Spatial. Use
        ///   <see cref="RenderOrderOverrideHelpers.SetRenderPriority"/> to set this to ensure the applied flag is
        ///   reset to have the effect be applied.
        /// </summary>
        public int RenderPriority;

        /// <summary>
        ///   Must be set to false when changing <see cref="RenderPriority"/> to have a new value be applied
        /// </summary>
        [JsonIgnore]
        public bool RenderPriorityApplied;
    }

    public static class RenderOrderOverrideHelpers
    {
        public static void SetRenderPriority(this ref RenderOrderOverride spatialInstance, int priority)
        {
            spatialInstance.RenderPriorityApplied = false;
            spatialInstance.RenderPriority = priority;
        }
    }
}
