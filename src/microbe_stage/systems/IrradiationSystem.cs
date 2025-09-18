namespace Systems;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;

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
[RunsBefore(typeof(ProcessSystem))]
[RunsBefore(typeof(CompoundAbsorptionSystem))]
public partial class IrradiationSystem : BaseSystem<World, float>
{
    public IrradiationSystem(World world) : base(world)
    {
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
            source.RadiatedEntities ??= new HashSet<Entity>();

            source.RadiatedEntities.Clear();
            sensor.GetDetectedBodies(source.RadiatedEntities);

            if (source.RadiatedEntities.Count == 0)
                return;

            float radiationAmount = source.RadiationStrength * delta;

            var chunkPosition = position.Position;

            foreach (var radiatedEntity in source.RadiatedEntities)
            {
                if (radiatedEntity == Entity.Null)
                    continue;

                // Anything with a compound storage can receive radiation
                if (!radiatedEntity.Has<CompoundStorage>())
                    continue;

                var compounds = radiatedEntity.Get<CompoundStorage>().Compounds;

                // Though if the storage has no capacity set, then don't add anything. This should filter out
                // drain-only storages like chunks
                if (compounds.NominalCapacity <= 0)
                    continue;

                var distanceSquared = chunkPosition.DistanceSquaredTo(radiatedEntity.Get<WorldPosition>().Position);

                HandleRadiation(compounds, radiationAmount, distanceSquared);
            }
        }
    }

    private PhysicsShape CreateDetectorShape(float sourceRadius)
    {
        return PhysicsShape.CreateSphere(sourceRadius);
    }

    private void HandleRadiation(CompoundBag compounds, float amount, float distanceSquared)
    {
        // Apply inverse square law
        amount *= 1 / distanceSquared;

        compounds.AddCompound(Compound.Radiation, amount);
    }
}
