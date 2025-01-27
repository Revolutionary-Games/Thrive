namespace Systems;

using System.Collections.Generic;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;

/// <summary>
///   Handles <see cref="RadiationSource"/> sending out radiation to anything that can receive it that is nearby
/// </summary>
/// <remarks>
///   <para>
///     Compound bag writing uses no safety checks from multiple threads, so this is marked as running before the most
///     likely systems to access it.
///   </para>
/// </remarks>
[With(typeof(RadiationSource))]
[With(typeof(PhysicsSensor))]
[With(typeof(WorldPosition))]
[ReadsComponent(typeof(WorldPosition))]
[RunsBefore(typeof(ProcessSystem))]
[RunsBefore(typeof(CompoundAbsorptionSystem))]
public sealed class IrradiationSystem : AEntitySetSystem<float>
{
    public IrradiationSystem(World world, IParallelRunner runner) : base(world, runner,
        Constants.SYSTEM_HIGHER_ENTITIES_PER_THREAD)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var source = ref entity.Get<RadiationSource>();
        ref var sensor = ref entity.Get<PhysicsSensor>();

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

            sensor.GetDetectedBodies(source.RadiatedEntities);

            if (source.RadiatedEntities.Count == 0)
                return;

            var chunkPosition = entity.Get<WorldPosition>().Position;

            float radiationAmount = source.RadiationStrength * delta;

            foreach (var radiatedEntity in source.RadiatedEntities)
            {
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
