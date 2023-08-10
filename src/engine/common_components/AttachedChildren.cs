namespace Components
{
    using System.Collections.Generic;
    using System.Linq;
    using DefaultEcs;

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
