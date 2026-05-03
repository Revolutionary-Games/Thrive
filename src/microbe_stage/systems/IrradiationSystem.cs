namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;

/// <summary>
///   Handles <see cref="RadiationSource"/> sending out radiation to anything that can receive it that is nearby
/// </summary>
/// <remarks>
///   <para>
///     Compound bag writing uses no safety checks from multiple threads, so this is marked as running before the most
///     likely systems to access it.
///   </para>
/// </remarks>
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(CompoundStorage))]
[ReadsComponent(typeof(MicrobeColony))]
[RunsBefore(typeof(ProcessSystem))]
[RunsBefore(typeof(CompoundAbsorptionSystem))]
[RuntimeCost(0.5f)]
public partial class IrradiationSystem(World world) : BaseSystem<World, float>(world)
{
    private static PhysicsShape CreateDetectorShape(float sourceRadius)
    {
        return PhysicsShape.CreateSphere(sourceRadius);
    }

    private static void HandleRadiationForEntity(in Entity entity, float radiationAmount, float distanceSquared)
    {
        ref var compoundStorage = ref entity.Get<CompoundStorage>();
        var compounds = compoundStorage.Compounds;

        // If the storage has no capacity set, don't add anything. This should filter out drain-only storages
        // like chunks.
        if (compounds.NominalCapacity <= 0)
            return;

        compounds.AddCompound(Compound.Radiation, radiationAmount / distanceSquared);
    }

    private static float GetDistanceSquared(Entity entity, Vector3 chunkPosition)
    {
        ref readonly var worldPosition = ref entity.Get<WorldPosition>();
        return chunkPosition.DistanceSquaredTo(worldPosition.Position);
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref RadiationSource source, ref PhysicsSensor sensor,
        ref WorldPosition position)
    {
        if (sensor.SensorBody == null)
        {
            // Ignore invalid configurations
            if (source.Radius <= 0)
                return;

            // Setup detection when missing
            sensor.ActiveArea = CreateDetectorShape(source.Radius);
            sensor.ApplyNewShape = true;
        }
        else
        {
            source.RadiatedEntities ??= [];

            source.RadiatedEntities.Clear();
            sensor.GetDetectedBodies(source.RadiatedEntities);

            if (source.RadiatedEntities.Count == 0)
                return;

            var radiationAmount = source.RadiationStrength * delta;

            var chunkPosition = position.Position;
            var sourceRadiusSquared = source.Radius * source.Radius;

            foreach (var radiatedEntity in source.RadiatedEntities)
            {
                if (radiatedEntity == default(Entity))
                    continue;

                if (!radiatedEntity.IsAliveAndHas<CompoundStorage>())
                    continue;

                if (!radiatedEntity.Has<MicrobeColony>())
                {
                    // Not a cell colony, handle just the entity itself that was detected as hit
                    var distanceSquared = GetDistanceSquared(radiatedEntity, chunkPosition);
                    HandleRadiationForEntity(radiatedEntity, radiationAmount, distanceSquared);
                    continue;
                }

                // Colony physics detections can come from any member sub-shape, so check every member by position.
                ref readonly var colony = ref radiatedEntity.Get<MicrobeColony>();
                var colonyMembers = colony.ColonyMembers;

                foreach (var entity in colonyMembers)
                {
                    if (!entity.IsAliveAndHas<CompoundStorage>() || !entity.Has<WorldPosition>())
                        continue;

                    var distanceSquared = GetDistanceSquared(entity, chunkPosition);

                    if (distanceSquared > sourceRadiusSquared)
                        continue;

                    HandleRadiationForEntity(entity, radiationAmount, distanceSquared);
                }
            }
        }
    }
}
