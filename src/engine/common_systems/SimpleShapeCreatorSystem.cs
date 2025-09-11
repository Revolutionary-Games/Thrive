namespace Systems;

using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;

/// <summary>
///   Handles creating the simple shapes of <see cref="SimpleShapeType"/>
/// </summary>
[RunsBefore(typeof(PhysicsBodyCreationSystem))]
[RuntimeCost(0.25f)]
public partial class SimpleShapeCreatorSystem : BaseSystem<World, float>
{
    public SimpleShapeCreatorSystem(World world, IParallelRunner runner) :
        base(world, runner, Constants.SYSTEM_HIGH_ENTITIES_PER_THREAD)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref SimpleShapeCreator shapeCreator, ref PhysicsShapeHolder shapeHolder)
    {
        if (shapeCreator.ShapeCreated)
            return;

        shapeHolder.Shape = CreateShape(ref shapeCreator);

        if (!shapeCreator.SkipForceRecreateBodyIfCreated)
            shapeHolder.UpdateBodyShapeIfCreated = true;

        shapeCreator.ShapeCreated = true;
    }

    private PhysicsShape CreateShape(ref SimpleShapeCreator creator)
    {
        var density = creator.Density;

        if (density <= 0)
            density = 1000;

        // TODO: add caching here for small shapes that get recreated a lot
        switch (creator.ShapeType)
        {
            case SimpleShapeType.Box:
                return PhysicsShape.CreateBox(creator.Size, density);
            case SimpleShapeType.Sphere:
                return PhysicsShape.CreateSphere(creator.Size, density);
            default:
                throw new ArgumentOutOfRangeException(nameof(creator.ShapeType),
                    "Unknown simple shape type to create");
        }
    }
}
