namespace Components
{
    using System.Collections.Generic;
    using System.Linq;
    using DefaultEcs;

    // TODO: delete this if not necessary (this is currently not updated at all for the microbe colony or engulf logic)
    /// <summary>
    ///   Added to the parent entity when <see cref="AttachedToEntity"/> is added to the child entity. This tracks all
    ///   of the entities that are attached to this entity to allow easily finding them for required operations.
    /// </summary>
    public struct AttachedChildren
    {
        public List<Entity> Children;

        public AttachedChildren(IEnumerable<Entity> children)
        {
            Children = children.ToList();
        }
    }
}
