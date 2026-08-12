namespace Systems;

using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;

/// <summary>
///   Handles creating the simple shapes of <see cref="SimpleShapeType"/>
/// </summary>
[RunsBefore(typeof(PhysicsBodyCreationSystem))]
[RuntimeCost(0.25f)]
public partial class SimpleShapeCreatorSystem(World world) : BaseSystem<World, float>(world)
{
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

        if (creator.Size <= 0)
            throw new InvalidOperationException("Size must be greater than 0");

        var cache = ProceduralDataCache.Instance;

        var cached = cache.ReadSimpleShape(creator.ShapeType, creator.Size, density);
        if (cached != null)
            return cached;

        PhysicsShape shape;
        switch (creator.ShapeType)
        {
            case SimpleShapeType.Box:
                shape = PhysicsShape.CreateBox(creator.Size, density);
                break;
            case SimpleShapeType.Sphere:
                shape = PhysicsShape.CreateSphere(creator.Size, density);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(creator.ShapeType),
                    "Unknown simple shape type to create");
        }

        cache.WriteSimpleShape(creator.ShapeType, creator.Size, density, shape);
        return shape;
    }
}
