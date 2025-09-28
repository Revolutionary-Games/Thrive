namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;

/// <summary>
///   Loads predefined collision shapes from resources
/// </summary>
/// <remarks>
///   <para>
///     Runs on the main thread for now due to needing to load Godot resources for physics.
///     Maybe in the future we'll have a pre-converted format for the game that doesn't need this.
///     Also, multithreading is disabled for now.
///   </para>
/// </remarks>
[RuntimeCost(0.5f)]
[RunsOnMainThread]
public partial class CollisionShapeLoaderSystem : BaseSystem<World, float>
{
    public CollisionShapeLoaderSystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref CollisionShapeLoader loader, ref PhysicsShapeHolder shapeHolder)
    {
        if (loader.ShapeLoaded)
            return;

        float density;
        if (loader.ApplyDensity)
        {
            density = loader.Density;
        }
        else
        {
            // TODO: per resource object defaults (if those are possible to add)
            density = 1000;
        }

        // TODO: switch to pre-processing collision shapes before the game is exported for faster runtime loading
        shapeHolder.Shape = PhysicsShape.CreateShapeFromGodotResource(loader.CollisionResourcePath, density);

        if (!loader.SkipForceRecreateBodyIfCreated)
            shapeHolder.UpdateBodyShapeIfCreated = true;

        loader.ShapeLoaded = true;
    }
}
